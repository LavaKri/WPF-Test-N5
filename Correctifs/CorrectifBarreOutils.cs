// =====================================================================================================
// CorrectifBarreOutils.cs — correctif réutilisable : Tab ne sort pas d'une ToolBar WPF.
//
// AUCUNE DÉPENDANCE : WPF seulement.
//
// CIBLES : .NET 10 (net10.0-windows) et .NET Framework 4.8 (net48).
//   Fichier autonome : pas de using implicites, pas de syntaxe C# récente (compile en C# 7.3).
//
// DÉFAUT CORRIGÉ : dans une ToolBar, Tab et Maj+Tab tournent d'un bouton à l'autre sans jamais sortir de la
//   barre ; seuls Échap, ou Espace sur un bouton, en sortent (le focus revient à l'élément d'où l'on venait).
//   Constaté avec NVDA, .NET 10, novembre 2026.
//   Cause, d'après le code source de WPF (ToolBar.cs, constructeur statique) : la classe ToolBar règle
//   KeyboardNavigation.TabNavigation sur Cycle. Une barre d'outils Windows est d'habitude un seul arrêt de
//   tabulation, parcouru aux flèches.
//   Poser seulement TabNavigation = Once sur la barre NE SUFFIT PAS (essayé, NVDA, .NET 10, novembre 2026) :
//   Tab tournait toujours dans la barre. Cause probable : la ToolBar est une portée de focus
//   (FocusManager.IsFocusScope) et la navigation par Tab, partie d'un bouton, ne remonte pas au-delà.
//   Le correctif :
//   - pose TabNavigation = Once (valeur locale, elle l'emporte sur celle de la classe) : en revenant dans la
//     barre par Tab ou Maj+Tab, le focus va au dernier bouton qui l'a eu ;
//   - intercepte Tab et Maj+Tab dans la barre (PreviewKeyDown) : les éléments de la barre sont rendus non
//     tabulables et la barre transparente (TabNavigation = Continue) le temps de déplacer le focus depuis le
//     parent visuel de la barre (hors de la portée de focus), vers l'avant ou l'arrière ; puis tout est rétabli.
//   Flèches (en boucle), Échap et Espace ne changent pas.
//
// UTILISATION, après InitializeComponent :
//   CorrectifsWpf.CorrectifBarreOutils.Brancher(maBarre);   // un appel par ToolBar
//   CorrectifsWpf.CorrectifBarreOutils.Actif = true;        // false : comportement d'origine (comparaison)
//   Actif s'applique aussitôt à toutes les barres branchées, et aux barres branchées ensuite.
// =====================================================================================================

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CorrectifsWpf
{
    /// <summary>Une ToolBar devient un seul arrêt de tabulation : Tab et Maj+Tab en sortent.</summary>
    public static class CorrectifBarreOutils
    {
        private static readonly List<ToolBar> Barres = new List<ToolBar>();
        private static bool actif;

        /// <summary>Faux : comportement d'origine de WPF (Tab tourne dans la barre).</summary>
        public static bool Actif
        {
            get { return actif; }
            set
            {
                actif = value;
                foreach (ToolBar barre in Barres)
                {
                    Appliquer(barre);
                }
            }
        }

        public static void Brancher(ToolBar barre)
        {
            if (!Barres.Contains(barre))
            {
                Barres.Add(barre);
                barre.PreviewKeyDown += ToucheEnfoncee;
            }
            Appliquer(barre);
        }

        /// <summary>Rend à la barre le comportement d'origine et l'oublie.</summary>
        public static void Debrancher(ToolBar barre)
        {
            Barres.Remove(barre);
            barre.PreviewKeyDown -= ToucheEnfoncee;
            barre.ClearValue(KeyboardNavigation.TabNavigationProperty);
        }

        // Sans valeur locale, la barre reprend la valeur de sa classe (Cycle).
        private static void Appliquer(ToolBar barre)
        {
            if (actif)
            {
                KeyboardNavigation.SetTabNavigation(barre, KeyboardNavigationMode.Once);
            }
            else
            {
                barre.ClearValue(KeyboardNavigation.TabNavigationProperty);
            }
        }

        private static void ToucheEnfoncee(object sender, KeyEventArgs e)
        {
            ModifierKeys modificateurs = Keyboard.Modifiers;
            if (!actif || e.Key != Key.Tab || (modificateurs != ModifierKeys.None && modificateurs != ModifierKeys.Shift))
            {
                return;
            }
            ToolBar barre = (ToolBar)sender;
            Sortir(barre, modificateurs == ModifierKeys.Shift);
            e.Handled = true;
        }

        // Déplace le focus hors de la barre, depuis son parent visuel. Les arrêts de tabulation de la barre (boutons,
        // et parties du modèle comme le bouton de débordement) sont neutralisés le temps du déplacement, et la barre
        // n'est plus un groupe (Continue) : le focus ne peut pas y revenir. Chaque valeur est rétablie à l'identique.
        private static void Sortir(ToolBar barre, bool arriere)
        {
            List<Control> arrets = new List<Control>();
            ChercherArrets(barre, arrets);
            List<object> valeurs = new List<object>();
            foreach (Control arret in arrets)
            {
                valeurs.Add(arret.ReadLocalValue(Control.IsTabStopProperty));
                arret.IsTabStop = false;
            }
            object navigation = barre.ReadLocalValue(KeyboardNavigation.TabNavigationProperty);
            KeyboardNavigation.SetTabNavigation(barre, KeyboardNavigationMode.Continue);
            try
            {
                UIElement depart = VisualTreeHelper.GetParent(barre) as UIElement ?? barre;
                depart.MoveFocus(new TraversalRequest(arriere ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next));
            }
            finally
            {
                Retablir(barre, KeyboardNavigation.TabNavigationProperty, navigation);
                for (int i = 0; i < arrets.Count; i++)
                {
                    Retablir(arrets[i], Control.IsTabStopProperty, valeurs[i]);
                }
            }
        }

        private static void ChercherArrets(DependencyObject parent, List<Control> arrets)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject enfant = VisualTreeHelper.GetChild(parent, i);
                Control controle = enfant as Control;
                if (controle != null && controle.IsTabStop)
                {
                    arrets.Add(controle);
                }
                ChercherArrets(enfant, arrets);
            }
        }

        private static void Retablir(DependencyObject element, DependencyProperty propriete, object valeurLocale)
        {
            if (valeurLocale == DependencyProperty.UnsetValue)
            {
                element.ClearValue(propriete);
            }
            else
            {
                element.SetValue(propriete, valeurLocale);
            }
        }
    }
}
