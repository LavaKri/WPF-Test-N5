// =====================================================================================================
// CorrectifGrille.cs — correctif réutilisable : tabulation et tri au clavier d'un DataGrid WPF.
//
// AUCUNE DÉPENDANCE : WPF seulement.
//
// LICENCE : MIT (LICENSE à la racine du dépôt). Portions adaptées d'Accessibility Insights for Windows,
//   Copyright (c) Microsoft Corporation, licence MIT (https://github.com/microsoft/accessibility-insights-windows).
//
// CIBLES : .NET 10 (net10.0-windows) et .NET Framework 4.8 (net48).
//   Fichier autonome : pas de using implicites, pas de syntaxe C# récente (compile en C# 7.3).
//
// DÉFAUTS CORRIGÉS (deux réglages indépendants) :
//   1. UnArret : Tab passe de cellule en cellule, le clavier met longtemps à sortir de la grille (piège de
//      tabulation). Le correctif fait des cellules un seul arrêt : Tab et Maj+Tab sortent de la grille.
//      D'après Microsoft, Accessibility Insights for Windows (CustomDataGrid) :
//      https://github.com/microsoft/accessibility-insights-windows (SharedUx/Controls/CustomControls)
//   2. EntetesFocalisables : les en-têtes de colonne ne sont pas focalisables, le tri est impossible au
//      clavier et l'ordre de tri n'est pas exposé. Le correctif rend les en-têtes focalisables (un arrêt
//      avant les cellules, Espace ou Entrée trie) et expose l'ordre par AutomationProperties.ItemStatus,
//      avec l'événement UIA de changement de propriété. D'après Guy Barker (Microsoft), « Common approaches
//      for enhancing the programmatic accessibility of your Win32, WinForms and WPF apps: Part 4 – WPF ».
//   3. UneLecture : NVDA lit deux fois le contenu d'une cellule de texte, une fois comme nom, une fois comme
//      valeur (motif Value). WPF tire cette valeur de la liaison ClipboardContentBinding de la colonne
//      (DataGrid.CellAutomationValueHolder.GetValue) ; pour une DataGridBoundColumn, elle vaut par défaut la
//      liaison de la colonne. Le correctif pose sur les DataGridTextColumn une liaison qui donne une chaîne
//      vide, et rétablit le contenu copié (Ctrl+C) par CopyingCellClipboardContent.
//      Effet de bord : la valeur de ces cellules n'est plus lisible ni modifiable par le motif UIA Value.
//
// UTILISATION, après InitializeComponent :
//   CorrectifsWpf.CorrectifGrille.Brancher(maGrille);              // un appel par DataGrid
//   CorrectifsWpf.CorrectifGrille.TexteCroissant = "tri croissant";  // textes d'ItemStatus, traduits
//   CorrectifsWpf.CorrectifGrille.TexteDecroissant = "tri décroissant";
//   CorrectifsWpf.CorrectifGrille.UnArret = true;                  // false : comportement d'origine
//   CorrectifsWpf.CorrectifGrille.EntetesFocalisables = true;      // false : comportement d'origine
//   CorrectifsWpf.CorrectifGrille.UneLecture = true;               // false : comportement d'origine
//   Les réglages s'appliquent tout de suite à toutes les grilles branchées.
// =====================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CorrectifsWpf
{
    /// <summary>Un seul arrêt de tabulation pour les cellules, et en-têtes de colonne utilisables au clavier.</summary>
    public static class CorrectifGrille
    {
        private static readonly List<DataGrid> grilles = new List<DataGrid>();
        // Style d'en-tête d'origine de chaque grille, rétabli quand les en-têtes ne sont plus focalisables.
        private static readonly Dictionary<DataGrid, Style> stylesOrigine = new Dictionary<DataGrid, Style>();
        // Liaison de copie d'origine de chaque colonne de texte modifiée, rétablie quand UneLecture est désactivé.
        private static readonly Dictionary<DataGridTextColumn, BindingBase> liaisonsOrigine = new Dictionary<DataGridTextColumn, BindingBase>();
        private static readonly HashSet<DataGridColumn> colonnesSuivies = new HashSet<DataGridColumn>();
        // Sert à évaluer la liaison d'origine pour le contenu copié.
        private static readonly DependencyProperty ContenuCopieProperty =
            DependencyProperty.RegisterAttached("ContenuCopie", typeof(string), typeof(CorrectifGrille));
        private static bool unArret;
        private static bool entetesFocalisables;
        private static bool uneLecture;

        public static string TexteCroissant { get; set; } = "sorted ascending";
        public static string TexteDecroissant { get; set; } = "sorted descending";

        public static bool UnArret
        {
            get { return unArret; }
            set
            {
                unArret = value;
                foreach (DataGrid grille in grilles)
                {
                    AppliquerUnArret(grille);
                }
            }
        }

        public static bool EntetesFocalisables
        {
            get { return entetesFocalisables; }
            set
            {
                entetesFocalisables = value;
                foreach (DataGrid grille in grilles)
                {
                    AppliquerEntetes(grille);
                }
            }
        }

        public static bool UneLecture
        {
            get { return uneLecture; }
            set
            {
                uneLecture = value;
                foreach (DataGrid grille in grilles)
                {
                    AppliquerUneLecture(grille);
                }
            }
        }

        public static void Brancher(DataGrid grille)
        {
            if (grilles.Contains(grille))
            {
                return;
            }
            grilles.Add(grille);
            grille.PreviewKeyDown += ToucheEnfoncee;
            grille.Sorting += TriDemande;
            AppliquerUnArret(grille);
            AppliquerEntetes(grille);
            AppliquerUneLecture(grille);
        }

        // Activé : la valeur UIA des cellules de texte est vide (le nom reste), la copie garde le vrai contenu.
        private static void AppliquerUneLecture(DataGrid grille)
        {
            foreach (DataGridColumn colonne in grille.Columns)
            {
                DataGridTextColumn texte = colonne as DataGridTextColumn;
                if (texte == null)
                {
                    continue;
                }
                if (uneLecture && !liaisonsOrigine.ContainsKey(texte))
                {
                    liaisonsOrigine[texte] = texte.ClipboardContentBinding;
                    // Une liaison nulle ne suffit pas : la colonne retomberait sur sa propre liaison.
                    texte.ClipboardContentBinding = new Binding { Source = string.Empty, Mode = BindingMode.OneWay };
                    if (colonnesSuivies.Add(texte))
                    {
                        texte.CopyingCellClipboardContent += CopieCellule;
                    }
                }
                else if (!uneLecture && liaisonsOrigine.TryGetValue(texte, out BindingBase liaison))
                {
                    texte.ClipboardContentBinding = liaison;
                    liaisonsOrigine.Remove(texte);
                }
            }
        }

        // Copie (Ctrl+C) : on remet le contenu que donnait la liaison d'origine.
        private static void CopieCellule(object sender, DataGridCellClipboardEventArgs e)
        {
            DataGridTextColumn colonne = sender as DataGridTextColumn;
            BindingBase origine;
            if (colonne == null || !liaisonsOrigine.TryGetValue(colonne, out origine) || origine == null)
            {
                return;
            }
            FrameworkElement cible = new FrameworkElement { DataContext = e.Item };
            BindingOperations.SetBinding(cible, ContenuCopieProperty, origine);
            e.Content = cible.GetValue(ContenuCopieProperty);
            BindingOperations.ClearBinding(cible, ContenuCopieProperty);
        }

        /// <summary>À appeler après un changement de langue : les textes d'ordre de tri sont reposés.</summary>
        public static void MettreAJourTri(DataGrid grille)
        {
            grille.Dispatcher.BeginInvoke(new Action(() => PoserEtatsTri(grille)), DispatcherPriority.Background);
        }

        private static void AppliquerUnArret(DataGrid grille)
        {
            if (unArret)
            {
                KeyboardNavigation.SetTabNavigation(grille, KeyboardNavigationMode.Once);
            }
            else
            {
                grille.ClearValue(KeyboardNavigation.TabNavigationProperty);
            }
        }

        private static void AppliquerEntetes(DataGrid grille)
        {
            if (entetesFocalisables)
            {
                if (!stylesOrigine.TryGetValue(grille, out Style origine))
                {
                    origine = grille.ColumnHeaderStyle;
                    stylesOrigine[grille] = origine;
                }
                Style style = new Style(typeof(DataGridColumnHeader), origine);
                style.Setters.Add(new Setter(UIElement.FocusableProperty, true));
                style.Setters.Add(new Setter(Control.IsTabStopProperty, true));
                // L'en-tête de remplissage (à droite de la dernière colonne, sans colonne) est placé en premier
                // dans le modèle de WPF : focalisable, il recevait le focus au premier Tab, sans nom (constaté).
                DataTrigger remplissage = new DataTrigger
                {
                    Binding = new Binding("Column") { RelativeSource = RelativeSource.Self },
                    Value = null
                };
                remplissage.Setters.Add(new Setter(UIElement.FocusableProperty, false));
                remplissage.Setters.Add(new Setter(Control.IsTabStopProperty, false));
                style.Triggers.Add(remplissage);
                grille.ColumnHeaderStyle = style;
            }
            else if (stylesOrigine.TryGetValue(grille, out Style ancien))
            {
                grille.ColumnHeaderStyle = ancien;
                stylesOrigine.Remove(grille);
            }
            MettreAJourTri(grille);
        }

        private static void ToucheEnfoncee(object sender, KeyEventArgs e)
        {
            ModifierKeys modificateurs = Keyboard.Modifiers;
            if (e.Key != Key.Tab || (modificateurs != ModifierKeys.None && modificateurs != ModifierKeys.Shift))
            {
                return;
            }
            DataGrid grille = (DataGrid)sender;
            bool arriere = modificateurs == ModifierKeys.Shift;

            if (Keyboard.FocusedElement is DataGridColumnHeader)
            {
                if (!entetesFocalisables)
                {
                    return;
                }
                if (arriere)
                {
                    Sortir(grille, true);
                }
                else
                {
                    FocaliserCellule(grille);
                }
                e.Handled = true;
                return;
            }

            if (!unArret)
            {
                return;
            }
            // Une édition en cours est validée, comme le ferait Tab dans la grille d'origine.
            grille.CommitEdit(DataGridEditingUnit.Row, true);
            if (arriere && entetesFocalisables)
            {
                FocaliserEntete(grille);
            }
            else
            {
                Sortir(grille, arriere);
            }
            e.Handled = true;
        }

        // Déplace le focus hors de la grille. La grille est rendue non tabulable le temps du déplacement, pour
        // que le focus ne s'arrête pas sur la grille elle-même (même moyen que CorrectifCalendrier). La boucle
        // reprend la méthode de Microsoft (CustomDataGrid), bornée par sécurité.
        private static void Sortir(DataGrid grille, bool arriere)
        {
            FocusNavigationDirection sens = arriere ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next;
            object valeurLocale = grille.ReadLocalValue(Control.IsTabStopProperty);
            grille.IsTabStop = false;
            try
            {
                for (int i = 0; i < 1000 && grille.IsKeyboardFocusWithin; i++)
                {
                    UIElement focus = Keyboard.FocusedElement as UIElement;
                    if (focus == null || !focus.MoveFocus(new TraversalRequest(sens)))
                    {
                        break;
                    }
                }
            }
            finally
            {
                if (valeurLocale == DependencyProperty.UnsetValue)
                {
                    grille.ClearValue(Control.IsTabStopProperty);
                }
                else
                {
                    grille.SetValue(Control.IsTabStopProperty, valeurLocale);
                }
            }
        }

        // En-tête de la colonne courante, ou le premier en-tête.
        private static void FocaliserEntete(DataGrid grille)
        {
            DataGridColumn colonne = grille.CurrentColumn;
            DataGridColumnHeader premier = null;
            foreach (DataGridColumnHeader entete in TrouverEnfants<DataGridColumnHeader>(grille))
            {
                if (entete.Column == null)
                {
                    continue;   // en-tête de remplissage, à droite de la dernière colonne
                }
                if (premier == null)
                {
                    premier = entete;
                }
                if (entete.Column == colonne)
                {
                    entete.Focus();
                    return;
                }
            }
            if (premier != null)
            {
                premier.Focus();
            }
            else
            {
                Sortir(grille, true);
            }
        }

        // Cellule courante, ou la première cellule de la première ligne.
        private static void FocaliserCellule(DataGrid grille)
        {
            if (grille.Items.Count == 0 || grille.Columns.Count == 0)
            {
                Sortir(grille, false);
                return;
            }
            DataGridCellInfo courante = grille.CurrentCell;
            object ligne = courante.IsValid ? courante.Item : grille.Items[0];
            DataGridColumn colonne = courante.IsValid ? courante.Column : grille.Columns[0];
            grille.ScrollIntoView(ligne, colonne);
            grille.UpdateLayout();
            DataGridRow conteneur = grille.ItemContainerGenerator.ContainerFromItem(ligne) as DataGridRow;
            FrameworkElement contenu = conteneur == null ? null : colonne.GetCellContent(conteneur);
            DataGridCell cellule = contenu == null ? null : contenu.Parent as DataGridCell;
            if (cellule != null)
            {
                cellule.Focus();
            }
            else
            {
                Sortir(grille, false);
            }
        }

        // Le tri est fait par la grille après l'événement : on lit l'ordre une fois la grille à jour.
        private static void TriDemande(object sender, DataGridSortingEventArgs e)
        {
            MettreAJourTri((DataGrid)sender);
        }

        private static void PoserEtatsTri(DataGrid grille)
        {
            foreach (DataGridColumnHeader entete in TrouverEnfants<DataGridColumnHeader>(grille))
            {
                if (entete.Column == null)
                {
                    continue;
                }
                string ancien = AutomationProperties.GetItemStatus(entete);
                string nouveau = "";
                if (entetesFocalisables && entete.Column.SortDirection.HasValue)
                {
                    nouveau = entete.Column.SortDirection.Value == ListSortDirection.Ascending ? TexteCroissant : TexteDecroissant;
                }
                if (ancien == nouveau)
                {
                    continue;
                }
                AutomationProperties.SetItemStatus(entete, nouveau);
                AutomationPeer peer = UIElementAutomationPeer.FromElement(entete);
                if (peer != null)
                {
                    AutomationPeer source = peer.EventsSource ?? peer;
                    source.RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty, ancien, nouveau);
                }
            }
        }

        private static IEnumerable<T> TrouverEnfants<T>(DependencyObject parent) where T : DependencyObject
        {
            int nombre = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < nombre; i++)
            {
                DependencyObject enfant = VisualTreeHelper.GetChild(parent, i);
                T trouve = enfant as T;
                if (trouve != null)
                {
                    yield return trouve;
                }
                foreach (T descendant in TrouverEnfants<T>(enfant))
                {
                    yield return descendant;
                }
            }
        }
    }
}
