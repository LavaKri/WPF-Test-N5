// =====================================================================================================
// CorrectifLectureRiche.cs — correctif réutilisable : améliorer ce qu'un lecteur d'écran lit dans un RichTextBox.
//
// DÉPENDANCE OBLIGATOIRE : Harmony (bibliothèque de patch de méthodes .NET à l'exécution)
//   - paquet NuGet : Lib.Harmony, version 2.4.3 (testée)
//   - licence : MIT — https://github.com/pardeike/Harmony
//   Voir l'en-tête de CorrectifLigneVide.cs pour les détails.
//
// CIBLES : .NET 10 (net10.0-windows) et .NET Framework 4.8 (net48).
//   Fichier autonome : pas de using implicites, pas de syntaxe C# récente (compile en C# 7.3).
//
// CORRECTIF GLOBAL, complété par étapes. Constat (NVDA, .NET 10, novembre 2026) : dans un document
// (FlowDocument) d'un RichTextBox, NVDA ne signale que le tableau ; ni titre, ni liste, ni lien.
//
// ÉTAPE 1 — TITRES ET LIENS. Code de WPF (TextRangeAdaptor.GetAttributeValue) : les attributs de texte UIA
//   StyleId (40034) et Link (40035), que NVDA utilise pour annoncer les titres et les liens, sont inconnus de
//   la table d'attributs de WPF, qui répond « non pris en charge ». Le correctif répond à leur place (préfixe
//   Harmony, seulement pour ces deux identifiants ; les autres restent à WPF) :
//   - StyleId : StyleId_Heading1 à StyleId_Heading9 (70001 à 70009) pour un paragraphe marqué par la propriété
//     attachée NiveauTitre (1 à 9) — WPF n'a pas de notion de titre —, StyleId_Normal (70012) sinon ;
//   - Link : une plage de texte (type attendu par UIA), enveloppée comme WPF le fait pour ses autres plages
//     (TextRangeProviderWrapper.WrapArgument), si la plage est dans un Hyperlink ; « non pris en charge » sinon ;
//   - valeur « mélangée » (MixedAttributeValue) si la plage couvre des valeurs différentes : NVDA la découpe.
//
// ÉTAPE 2 — CELLULES DE TABLEAU. WPF expose une cellule (TableCellAutomationPeer) avec le type UIA Custom et le
//   seul motif GridItem ; NVDA ne lit la position et l'en-tête d'une cellule que pour les rôles cellule, élément
//   de données ou en-tête, et l'en-tête de colonne par le motif TableItem, en lisant le NOM de la cellule
//   d'en-tête (vide dans WPF). Trois postfix Harmony :
//   - TableCellAutomationPeer.GetAutomationControlTypeCore : DataItem au lieu de Custom ;
//   - TableCellAutomationPeer.GetPattern : motif TableItem en plus (ligne, colonne et tableau repris de GridItem ;
//     en-tête de colonne = cellule de même colonne dans la première ligne, si le tableau est marqué par la
//     propriété attachée LigneEntete) ;
//   - ContentElementAutomationPeer.GetNameCore : une cellule de la ligne d'en-tête d'un tableau marqué, sans
//     nom posé par l'application, prend pour nom son texte ;
//   - ContentElementAutomationPeer.IsEnabledCore : un Table ou une TableCell sont déclarés activés. Dans un
//     RichTextBox, tant que IsDocumentEnabled est faux, tout le contenu du document est désactivé
//     (FlowDocument.IsEnabledCore) et NVDA disait « Tableau non disponible », « Ligne 2 non disponible »
//     (constaté, novembre 2026). Juste pour un lien (inerte), faux pour un tableau : les liens gardent leur état.
//   La colonne est calculée par la place de la cellule dans sa ligne (ColumnSpan compris) : l'index de colonne
//   de WPF n'est connu qu'après la mise en page.
//
// ÉTAPE 3 — LISTES. WPF ne crée aucun peer pour List ni ListItem (ContentElement.OnCreateAutomationPeer rend
//   null) ; NVDA reconnaît une liste dans un texte par l'élément UIA qui englobe la plage (GetEnclosingElement,
//   puis ses parents), de type List, et seulement s'il est en lecture seule (sinon c'est pour lui une zone de
//   liste). Postfix Harmony de ContentElement.OnCreateAutomationPeer (implémentation de base) : un peer du
//   correctif pour List (type List, motif Value en lecture seule, seul moyen d'avoir cet état) et pour ListItem
//   (type ListItem), tous deux activés ; le motif Text de WPF les trouve ensuite seul (CreatePeerForElement).
//   WPF garde le peer en cache sur l'élément : Desactiver efface ce cache pour rendre le comportement natif.
//   Ni la puce ni le numéro ne sont lus : WPF les dessine sans les mettre dans le texte.
//
// UTILISATION :
//   CorrectifsWpf.CorrectifLectureRiche.SetNiveauTitre(paragraphe, 1);   // marquer un titre (niveau 1 à 9)
//   CorrectifsWpf.CorrectifLectureRiche.SetLigneEntete(tableau, true);   // première ligne = en-têtes de colonne
//   CorrectifsWpf.CorrectifLectureRiche.Activer();     // lève une exception si le code de WPF a changé
//   CorrectifsWpf.CorrectifLectureRiche.Desactiver();  // rétablit le comportement d'origine (listes comprises)
//   Le patch agit sur toutes les zones de texte du processus, en mémoire seulement. À retester à chaque
//   version de .NET.
// =====================================================================================================

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Documents;
using HarmonyLib;

namespace CorrectifsWpf
{
    /// <summary>Titres, liens, cellules de tableau et listes d'un FlowDocument annoncés par les lecteurs d'écran.</summary>
    public static class CorrectifLectureRiche
    {
        private const string IdHarmony = "CorrectifsWpf.CorrectifLectureRiche";
        private static readonly Harmony harmony = new Harmony(IdHarmony);

        // Identifiants UIA (UIAutomationClient.h), absents des types de WPF.
        private const int AttributStyleId = 40034;
        private const int AttributLien = 40035;
        private const int StyleTitre1 = 70001;
        private const int StyleNormal = 70012;

        private static FieldInfo champDebut;
        private static FieldInfo champFin;
        private static FieldInfo champPeer;
        private static MethodInfo envelopper;
        private static MethodInfo fournisseurDePeer;
        private static object cachePeer;
        private static MethodInfo effacerCache;
        private static PropertyInfo marquePeer;

        // Listes et éléments de liste qui ont reçu un peer du correctif (gardé en cache par WPF).
        private static readonly List<WeakReference<ContentElement>> elementsListe = new List<WeakReference<ContentElement>>();

        public static bool Actif { get; private set; }

        /// <summary>Niveau de titre d'un paragraphe (1 à 9) ; 0 : pas un titre.</summary>
        public static readonly DependencyProperty NiveauTitreProperty = DependencyProperty.RegisterAttached(
            "NiveauTitre", typeof(int), typeof(CorrectifLectureRiche), new PropertyMetadata(0));

        public static int GetNiveauTitre(DependencyObject element)
        {
            return (int)element.GetValue(NiveauTitreProperty);
        }

        public static void SetNiveauTitre(DependencyObject element, int niveau)
        {
            element.SetValue(NiveauTitreProperty, niveau);
        }

        /// <summary>Vrai sur un Table : sa première ligne donne les en-têtes de colonne.</summary>
        public static readonly DependencyProperty LigneEnteteProperty = DependencyProperty.RegisterAttached(
            "LigneEntete", typeof(bool), typeof(CorrectifLectureRiche), new PropertyMetadata(false));

        public static bool GetLigneEntete(DependencyObject element)
        {
            return (bool)element.GetValue(LigneEnteteProperty);
        }

        public static void SetLigneEntete(DependencyObject element, bool valeur)
        {
            element.SetValue(LigneEnteteProperty, valeur);
        }

        public static void Activer()
        {
            if (Actif)
            {
                return;
            }

            Type adaptateur = typeof(FrameworkElement).Assembly.GetType("MS.Internal.Automation.TextRangeAdaptor");
            Type enveloppe = typeof(UIElement).Assembly.GetType("MS.Internal.Automation.TextRangeProviderWrapper");
            MethodInfo attribut = adaptateur == null ? null : adaptateur.GetMethod(
                "System.Windows.Automation.Provider.ITextRangeProvider.GetAttributeValue",
                BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(int) }, null);
            champDebut = adaptateur == null ? null : AccessTools.Field(adaptateur, "_start");
            champFin = adaptateur == null ? null : AccessTools.Field(adaptateur, "_end");
            champPeer = adaptateur == null ? null : AccessTools.Field(adaptateur, "_textPeer");
            envelopper = enveloppe == null ? null : enveloppe.GetMethod("WrapArgument",
                BindingFlags.Static | BindingFlags.NonPublic, null,
                new[] { typeof(ITextRangeProvider), typeof(AutomationPeer) }, null);
            MethodInfo typeCellule = AccessTools.DeclaredMethod(typeof(TableCellAutomationPeer), "GetAutomationControlTypeCore");
            MethodInfo motifCellule = AccessTools.DeclaredMethod(typeof(TableCellAutomationPeer), "GetPattern", new[] { typeof(PatternInterface) });
            MethodInfo nomElement = AccessTools.DeclaredMethod(typeof(ContentElementAutomationPeer), "GetNameCore");
            MethodInfo activeElement = AccessTools.DeclaredMethod(typeof(ContentElementAutomationPeer), "IsEnabledCore");
            fournisseurDePeer = AccessTools.Method(typeof(AutomationPeer), "ProviderFromPeer", new[] { typeof(AutomationPeer) });
            MethodInfo creationPeer = AccessTools.DeclaredMethod(typeof(ContentElement), "OnCreateAutomationPeer");
            FieldInfo champCache = AccessTools.Field(typeof(ContentElement), "AutomationPeerField");
            cachePeer = champCache == null ? null : champCache.GetValue(null);
            effacerCache = cachePeer == null ? null : cachePeer.GetType().GetMethod("ClearValue", new[] { typeof(DependencyObject) });
            marquePeer = AccessTools.Property(typeof(ContentElement), "HasAutomationPeer");
            if (attribut == null || champDebut == null || champFin == null || champPeer == null || envelopper == null
                || typeCellule == null || motifCellule == null || nomElement == null || activeElement == null
                || fournisseurDePeer == null || creationPeer == null || effacerCache == null || marquePeer == null)
            {
                throw new InvalidOperationException("TextRangeAdaptor, TextRangeProviderWrapper, peers de tableau ou cache de peer introuvables (code interne de WPF différent).");
            }

            try
            {
                harmony.Patch(attribut, prefix: new HarmonyMethod(typeof(CorrectifLectureRiche), nameof(AvantAttribut)));
                harmony.Patch(typeCellule, postfix: new HarmonyMethod(typeof(CorrectifLectureRiche), nameof(ApresTypeCellule)));
                harmony.Patch(motifCellule, postfix: new HarmonyMethod(typeof(CorrectifLectureRiche), nameof(ApresMotifCellule)));
                harmony.Patch(nomElement, postfix: new HarmonyMethod(typeof(CorrectifLectureRiche), nameof(ApresNomElement)));
                harmony.Patch(activeElement, postfix: new HarmonyMethod(typeof(CorrectifLectureRiche), nameof(ApresActifElement)));
                harmony.Patch(creationPeer, postfix: new HarmonyMethod(typeof(CorrectifLectureRiche), nameof(ApresCreationPeer)));
            }
            catch
            {
                harmony.UnpatchAll(IdHarmony);
                throw;
            }
            Actif = true;
        }

        public static void Desactiver()
        {
            if (!Actif)
            {
                return;
            }
            harmony.UnpatchAll(IdHarmony);
            EffacerPeersListe();
            Actif = false;
        }

        // __0 : identifiant de l'attribut demandé. Faux : la réponse est donnée ici, WPF n'est pas appelé.
        private static bool AvantAttribut(object __instance, int __0, ref object __result)
        {
            if (__0 != AttributStyleId && __0 != AttributLien)
            {
                return true;
            }
            TextPointer debut = champDebut.GetValue(__instance) as TextPointer;
            TextPointer fin = champFin.GetValue(__instance) as TextPointer;
            if (debut == null || fin == null)
            {
                return true;   // autre type de document : réponse de WPF
            }
            if (debut.CompareTo(fin) > 0)
            {
                TextPointer echange = debut;
                debut = fin;
                fin = echange;
            }
            __result = __0 == AttributStyleId ? ValeurStyle(debut, fin) : ValeurLien(__instance, debut, fin);
            return false;
        }

        // Style de chaque paragraphe touché par la plage ; « mélangé » s'ils diffèrent.
        private static object ValeurStyle(TextPointer debut, TextPointer fin)
        {
            int? valeur = null;
            TextPointer position = debut;
            while (position != null)
            {
                Paragraph paragraphe = position.Paragraph;
                int niveau = paragraphe == null ? 0 : GetNiveauTitre(paragraphe);
                int style = niveau >= 1 && niveau <= 9 ? StyleTitre1 + niveau - 1 : StyleNormal;
                if (valeur == null)
                {
                    valeur = style;
                }
                else if (valeur != style)
                {
                    return TextPatternIdentifiers.MixedAttributeValue;
                }
                if (paragraphe == null)
                {
                    break;
                }
                position = paragraphe.ContentEnd.GetNextInsertionPosition(LogicalDirection.Forward);
                if (position == null || position.CompareTo(fin) >= 0)
                {
                    break;
                }
            }
            return valeur ?? StyleNormal;
        }

        // Lien qui contient le texte de la plage ; « mélangé » si une partie du texte est dans un autre lien (ou hors
        // lien). Seules comptent les positions suivies de texte : les bords d'éléments sans caractère (fin d'un Run,
        // fin du lien) ne changent rien à ce qui est lu. Plage vide (curseur) : le lien à cette position.
        private static object ValeurLien(object adaptateur, TextPointer debut, TextPointer fin)
        {
            Hyperlink lien = null;
            bool premier = true;
            for (TextPointer position = debut; position != null && position.CompareTo(fin) < 0;
                 position = position.GetNextContextPosition(LogicalDirection.Forward))
            {
                TextPointerContext suite = position.GetPointerContext(LogicalDirection.Forward);
                if (suite != TextPointerContext.Text && suite != TextPointerContext.EmbeddedElement)
                {
                    continue;
                }
                Hyperlink ici = LienA(position);
                if (premier)
                {
                    lien = ici;
                    premier = false;
                }
                else if (ici != lien)
                {
                    return TextPatternIdentifiers.MixedAttributeValue;
                }
            }
            if (premier)
            {
                lien = LienA(debut);
            }
            if (lien == null)
            {
                return AutomationElementIdentifiers.NotSupported;
            }
            AutomationPeer peer = (AutomationPeer)champPeer.GetValue(adaptateur);
            return envelopper.Invoke(null, new object[] { (ITextRangeProvider)adaptateur, peer });
        }

        // ===== Étape 2 : cellules de tableau =====

        private static void ApresTypeCellule(ref AutomationControlType __result)
        {
            __result = AutomationControlType.DataItem;
        }

        private static void ApresMotifCellule(TableCellAutomationPeer __instance, PatternInterface __0, ref object __result)
        {
            if (__0 == PatternInterface.TableItem && __result == null)
            {
                __result = new ElementDeTableau(__instance);
            }
        }

        // Nom d'une cellule d'en-tête : son texte, pour que le lecteur d'écran puisse lire l'en-tête de colonne.
        private static void ApresNomElement(ContentElementAutomationPeer __instance, ref string __result)
        {
            TableCell cellule = __instance.Owner as TableCell;
            if (string.IsNullOrEmpty(__result) && cellule != null && LigneEnteteDe(cellule) == cellule.Parent)
            {
                __result = new TextRange(cellule.ContentStart, cellule.ContentEnd).Text.Trim();
            }
        }

        // Un tableau n'a rien à activer : il ne doit pas être annoncé « non disponible » quand le document est inerte.
        private static void ApresActifElement(ContentElementAutomationPeer __instance, ref bool __result)
        {
            if (!__result && (__instance.Owner is Table || __instance.Owner is TableCell))
            {
                __result = true;
            }
        }

        // Première ligne d'un tableau marqué LigneEntete, ou null.
        private static TableRow LigneEnteteDe(TableCell cellule)
        {
            TableRow ligne = cellule.Parent as TableRow;
            TableRowGroup groupe = ligne == null ? null : ligne.Parent as TableRowGroup;
            Table tableau = groupe == null ? null : groupe.Parent as Table;
            if (tableau == null || !GetLigneEntete(tableau) || tableau.RowGroups.Count == 0 || tableau.RowGroups[0].Rows.Count == 0)
            {
                return null;
            }
            return tableau.RowGroups[0].Rows[0];
        }

        // Colonne d'une cellule d'après sa place dans sa ligne (étendues comprises).
        private static int ColonneDe(TableCell cellule)
        {
            TableRow ligne = (TableRow)cellule.Parent;
            int colonne = 0;
            foreach (TableCell autre in ligne.Cells)
            {
                if (autre == cellule)
                {
                    break;
                }
                colonne += Math.Max(1, autre.ColumnSpan);
            }
            return colonne;
        }

        // Cellule de la ligne d'en-tête qui couvre la colonne de la cellule ; null si pas d'en-tête.
        private static TableCell EnteteDe(TableCell cellule)
        {
            TableRow entete = LigneEnteteDe(cellule);
            if (entete == null || entete == cellule.Parent)
            {
                return null;
            }
            int colonne = ColonneDe(cellule);
            int debut = 0;
            foreach (TableCell candidate in entete.Cells)
            {
                int fin = debut + Math.Max(1, candidate.ColumnSpan);
                if (colonne >= debut && colonne < fin)
                {
                    return candidate;
                }
                debut = fin;
            }
            return null;
        }

        // Motif TableItem d'une cellule : position reprise du motif GridItem de WPF, en-tête de colonne calculé.
        private sealed class ElementDeTableau : ITableItemProvider
        {
            private readonly TableCellAutomationPeer peer;
            private readonly IGridItemProvider grille;

            public ElementDeTableau(TableCellAutomationPeer peer)
            {
                this.peer = peer;
                grille = (IGridItemProvider)peer;
            }

            public int Row { get { return grille.Row; } }
            public int Column { get { return grille.Column; } }
            public int RowSpan { get { return grille.RowSpan; } }
            public int ColumnSpan { get { return grille.ColumnSpan; } }
            public IRawElementProviderSimple ContainingGrid { get { return grille.ContainingGrid; } }

            public IRawElementProviderSimple[] GetRowHeaderItems()
            {
                return Array.Empty<IRawElementProviderSimple>();
            }

            public IRawElementProviderSimple[] GetColumnHeaderItems()
            {
                TableCell entete = EnteteDe((TableCell)peer.Owner);
                AutomationPeer peerEntete = entete == null ? null : ContentElementAutomationPeer.CreatePeerForElement(entete);
                if (peerEntete == null)
                {
                    return Array.Empty<IRawElementProviderSimple>();
                }
                return new[] { (IRawElementProviderSimple)fournisseurDePeer.Invoke(peer, new object[] { peerEntete }) };
            }
        }

        // ===== Étape 3 : listes =====

        private static void ApresCreationPeer(ContentElement __instance, ref AutomationPeer __result)
        {
            if (__result != null)
            {
                return;
            }
            List liste = __instance as List;
            ListItem element = __instance as ListItem;
            if (liste != null)
            {
                __result = new PeerListe(liste);
            }
            else if (element != null)
            {
                __result = new PeerElementListe(element);
            }
            else
            {
                return;
            }
            elementsListe.Add(new WeakReference<ContentElement>(__instance));
        }

        // Retire des listes le peer du correctif gardé en cache par WPF (ContentElement.CreateAutomationPeer) :
        // l'appel suivant redemande un peer à l'élément, qui n'en a plus.
        private static void EffacerPeersListe()
        {
            foreach (WeakReference<ContentElement> reference in elementsListe)
            {
                ContentElement element;
                if (reference.TryGetTarget(out element) && element.CheckAccess())
                {
                    effacerCache.Invoke(cachePeer, new object[] { element });
                    marquePeer.SetValue(element, false);
                }
            }
            elementsListe.Clear();
        }

        // Liste : en lecture seule (motif Value), seul état que NVDA accepte pour une liste de document.
        private sealed class PeerListe : TextElementAutomationPeer, IValueProvider
        {
            public PeerListe(List owner) : base(owner)
            {
            }

            public bool IsReadOnly { get { return true; } }
            public string Value { get { return string.Empty; } }

            public void SetValue(string value)
            {
                throw new InvalidOperationException("Liste en lecture seule.");
            }

            public override object GetPattern(PatternInterface patternInterface)
            {
                return patternInterface == PatternInterface.Value ? this : base.GetPattern(patternInterface);
            }

            protected override AutomationControlType GetAutomationControlTypeCore()
            {
                return AutomationControlType.List;
            }

            protected override string GetClassNameCore()
            {
                return "List";
            }

            protected override bool IsControlElementCore()
            {
                return true;
            }

            protected override bool IsEnabledCore()
            {
                return true;
            }
        }

        private sealed class PeerElementListe : TextElementAutomationPeer
        {
            public PeerElementListe(ListItem owner) : base(owner)
            {
            }

            protected override AutomationControlType GetAutomationControlTypeCore()
            {
                return AutomationControlType.ListItem;
            }

            protected override string GetClassNameCore()
            {
                return "ListItem";
            }

            protected override bool IsControlElementCore()
            {
                return true;
            }

            protected override bool IsEnabledCore()
            {
                return true;
            }
        }

        private static Hyperlink LienA(TextPointer position)
        {
            TextElement element = position.Parent as TextElement;
            while (element != null)
            {
                Hyperlink lien = element as Hyperlink;
                if (lien != null)
                {
                    return lien;
                }
                element = element.Parent as TextElement;
            }
            return null;
        }
    }
}
