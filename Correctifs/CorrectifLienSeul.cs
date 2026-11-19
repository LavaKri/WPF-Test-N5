// =====================================================================================================
// CorrectifLienSeul.cs — correctif réutilisable : un Hyperlink seul dans un TextBlock est lu deux fois par
// les lecteurs d'écran (« Mot de passe oublié, mot de passe oublié, lien »).
//
// DÉPENDANCE OBLIGATOIRE : Harmony (bibliothèque de patch de méthodes .NET à l'exécution)
//   - paquet NuGet : Lib.Harmony, version 2.4.3 (testée)
//   - licence : MIT — https://github.com/pardeike/Harmony
//   Voir l'en-tête de CorrectifLigneVide.cs pour les détails.
//
// CIBLES : .NET 10 (net10.0-windows) et .NET Framework 4.8 (net48).
//   Fichier autonome : pas de using implicites, pas de syntaxe C# récente (compile en C# 7.3).
//
// DÉFAUT CORRIGÉ (constaté avec NVDA, .NET 10 et .NET Framework 4.8, novembre 2026) :
//   le TextBlock qui contient le lien est un élément UIA « Text » qui porte le même nom que le lien, son unique
//   enfant ; NVDA lit cet ancêtre (nom seul) puis le lien. WPF n'a pas de AutomationProperties.AccessibilityView
//   pour retirer un élément de la vue des contrôles.
//
// MOYEN : postfix Harmony de IsControlElementCore et IsContentElementCore (méthodes protégées documentées) pour
//   un peer de TextBlock : faux quand le TextBlock ne contient qu'un Hyperlink (les autres Inline éventuels ne
//   sont que des espaces). Le TextBlock sort des vues « contrôle » et « contenu » de UIA ; le lien y reste, avec
//   son nom, son rôle, son motif Invoke et son focus. Un TextBlock qui mêle texte et lien n'est pas touché.
//
// UTILISATION :
//   CorrectifsWpf.CorrectifLienSeul.Activer();     // lève une exception si le code de WPF a changé
//   CorrectifsWpf.CorrectifLienSeul.Desactiver();  // rétablit le comportement d'origine
//   Le patch agit sur tout le processus, en mémoire seulement. À retester à chaque version de .NET.
// =====================================================================================================

using System;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Documents;
using HarmonyLib;

namespace CorrectifsWpf
{
    /// <summary>Lien seul dans un TextBlock : le TextBlock est retiré de la vue UIA, le lien reste.</summary>
    public static class CorrectifLienSeul
    {
        private const string IdHarmony = "CorrectifsWpf.CorrectifLienSeul";
        private static readonly Harmony harmony = new Harmony(IdHarmony);

        public static bool Actif { get; private set; }

        public static void Activer()
        {
            if (Actif)
            {
                return;
            }

            // Méthode effectivement appelée pour un TextBlockAutomationPeer : la sienne, ou celle d'une classe de base.
            MethodInfo controle = Trouver("IsControlElementCore");
            MethodInfo contenu = Trouver("IsContentElementCore");

            try
            {
                HarmonyMethod apres = new HarmonyMethod(typeof(CorrectifLienSeul), nameof(Apres));
                harmony.Patch(controle, postfix: apres);
                harmony.Patch(contenu, postfix: apres);
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
            Actif = false;
        }

        private static MethodInfo Trouver(string nom)
        {
            MethodInfo methode = AccessTools.Method(typeof(TextBlockAutomationPeer), nom, Type.EmptyTypes);
            if (methode == null || methode.ReturnType != typeof(bool))
            {
                throw new InvalidOperationException("TextBlockAutomationPeer." + nom + " introuvable (code de WPF différent).");
            }
            // Harmony ne patche que la méthode de la classe qui la déclare (UIElementAutomationPeer pour IsContentElementCore).
            return AccessTools.DeclaredMethod(methode.DeclaringType, nom, Type.EmptyTypes);
        }

        // La méthode patchée peut être celle d'une classe de base, commune à d'autres peers : filtrer sur le type.
        private static void Apres(AutomationPeer __instance, ref bool __result)
        {
            TextBlockAutomationPeer peer = __instance as TextBlockAutomationPeer;
            if (__result && peer != null && LienSeul((TextBlock)peer.Owner))
            {
                __result = false;
            }
        }

        // Enfants logiques plutôt que Inlines : lire Inlines convertirait un TextBlock en texte simple en contenu
        // complexe, et la méthode est appelée pour tous les TextBlock du processus.
        private static bool LienSeul(TextBlock texte)
        {
            int liens = 0;
            foreach (object enfant in LogicalTreeHelper.GetChildren(texte))
            {
                Run run = enfant as Run;
                if (enfant is Hyperlink)
                {
                    liens++;
                }
                else if (run == null || !string.IsNullOrWhiteSpace(run.Text))
                {
                    return false;
                }
            }
            return liens == 1;
        }
    }
}
