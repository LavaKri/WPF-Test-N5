// =====================================================================================================
// CorrectifCalendrier.cs — correctif réutilisable : Maj+Tab reste bloqué sur un jour d'un Calendar WPF.
//
// AUCUNE DÉPENDANCE : WPF seulement.
//
// CIBLES : .NET 10 (net10.0-windows) et .NET Framework 4.8 (net48).
//   Fichier autonome : pas de using implicites, pas de syntaxe C# récente (compile en C# 7.3).
//
// DÉFAUT CORRIGÉ : dans un Calendar, Tab depuis un jour sort du contrôle, mais Maj+Tab laisse le focus sur
//   le jour (piège clavier). Constaté avec NVDA, .NET 10, novembre 2026.
//   Cause probable, d'après le code source de WPF (Calendar.cs) : le Calendar est lui-même un arrêt de
//   tabulation, et un gestionnaire de classe GotFocus renvoie le focus au jour dès que le Calendar le
//   reçoit. En ordre inverse, l'élément qui précède un jour est le Calendar : Maj+Tab y va, et le focus
//   revient aussitôt au jour.
//   Le correctif rend le Calendar non tabulable le temps de déplacer le focus vers l'arrière : le focus
//   saute le conteneur et va à l'élément d'avant. Tab, les flèches et le menu contextuel ne changent pas.
//   Confirmé avec NVDA, .NET 10, novembre 2026.
//
// NON CORRIGÉ, À SAVOIR : en sélection simple (SingleDate), une flèche sélectionne le jour atteint (la
//   sélection suit le focus) et Espace ne fait rien dans la vue des jours (code de WPF, Calendar.ProcessSelection).
//   NVDA n'annonce pas cette sélection : l'événement ElementSelected de WPF vient d'un peer pris dans le cache
//   du CalendarAutomationPeer, pas de l'élément qui a le focus. Le relever depuis le jour focalisé fait dire
//   « sélectionné » à NVDA à chaque flèche (essayé), ce qui n'apporte rien puisque le jour atteint est
//   toujours sélectionné : retiré.
//
// UTILISATION, après InitializeComponent :
//   CorrectifsWpf.CorrectifCalendrier.Brancher(monCalendrier);   // un appel par Calendar
//   CorrectifsWpf.CorrectifCalendrier.Actif = true;              // false : comportement d'origine (comparaison)
//   Actif est lu à chaque touche. Le calendrier déroulant d'un DatePicker n'est pas concerné (non branché).
// =====================================================================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CorrectifsWpf
{
    /// <summary>Maj+Tab depuis un jour d'un Calendar sort du contrôle au lieu de revenir sur le jour.</summary>
    public static class CorrectifCalendrier
    {
        /// <summary>Faux : comportement d'origine de WPF. Lu à chaque touche.</summary>
        public static bool Actif { get; set; }

        public static void Brancher(Calendar calendrier)
        {
            calendrier.PreviewKeyDown += ToucheEnfoncee;
        }

        public static void Debrancher(Calendar calendrier)
        {
            calendrier.PreviewKeyDown -= ToucheEnfoncee;
        }

        private static void ToucheEnfoncee(object sender, KeyEventArgs e)
        {
            if (!Actif || e.Key != Key.Tab || Keyboard.Modifiers != ModifierKeys.Shift)
            {
                return;
            }
            Calendar calendrier = (Calendar)sender;
            UIElement focus = Keyboard.FocusedElement as UIElement;
            // Focus sur le Calendar lui-même (jours masqués, par exemple) : Maj+Tab sort déjà normalement.
            if (focus == null || focus == calendrier)
            {
                return;
            }

            // On rétablit exactement la valeur d'avant (locale, ou celle du style si aucune n'était posée).
            object valeurLocale = calendrier.ReadLocalValue(Control.IsTabStopProperty);
            calendrier.IsTabStop = false;
            try
            {
                focus.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
            }
            finally
            {
                if (valeurLocale == DependencyProperty.UnsetValue)
                {
                    calendrier.ClearValue(Control.IsTabStopProperty);
                }
                else
                {
                    calendrier.SetValue(Control.IsTabStopProperty, valeurLocale);
                }
            }
            e.Handled = true;
        }
    }
}
