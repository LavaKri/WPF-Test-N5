// =====================================================================================================
// CorrectifArbreCases.cs — correctif réutilisable : un lecteur d'écran ne voit pas les cases à cocher
// des nœuds d'un TreeView WPF.
//
// AUCUNE DÉPENDANCE : WPF seulement.
//
// CIBLES : .NET 10 (net10.0-windows) et .NET Framework 4.8 (net48).
//   Fichier autonome : pas de using implicites, pas de syntaxe C# récente (compile en C# 7.3).
//
// DÉFAUT CORRIGÉ : la façon courante de cocher les nœuds d'un TreeView est de mettre une CheckBox dans
//   le modèle du nœud. Le focus clavier est alors sur le TreeViewItem, dont le peer UIA n'expose pas le
//   motif Toggle : NVDA ne dit pas qu'il y a une case, ni si elle est cochée. Si la CheckBox reste
//   focalisable, elle devient un arrêt de focus à part et NVDA lit chaque nœud deux fois.
//   Constaté avec NVDA, .NET 10, novembre 2026.
//   Le correctif ajoute le motif Toggle aux nœuds (peer de donnée qui implémente IToggleProvider) et lève
//   l'événement UIA ToggleStateProperty à chaque changement de l'état, que NVDA annonce (stateChange).
//   D'après la solution de Microsoft pour Accessibility Insights for Windows :
//   https://devblogs.microsoft.com/engineering-at-microsoft/implementing-an-accessible-checkable-wpf-tree-view
//   Portions adaptées d'Accessibility Insights for Windows, Copyright (c) Microsoft Corporation, licence MIT
//   (https://github.com/microsoft/accessibility-insights-windows).
//   Différences : l'événement est levé à chaque changement (pas seulement par Toggle()), les nœuds racines
//   sont couverts (peer du TreeView), Espace est géré par le nœud, et le correctif se désactive.
//
// UTILISATION :
//   1. Remplacer le TreeView par CorrectifsWpf.CorrectifArbreCases (xmlns:cw="clr-namespace:CorrectifsWpf").
//   2. Lier IsChecked des nœuds à l'état du modèle, par un style (les nœuds sont des NoeudACocher) :
//        <Style TargetType="cw:NoeudACocher">
//            <Setter Property="IsChecked" Value="{Binding EstCoche, Mode=TwoWay}"/>
//        </Style>
//   3. Dans le modèle du nœud, rendre la CheckBox non focalisable (Focusable="False" IsTabStop="False") :
//      le nœud est le seul arrêt de focus, Espace le coche.
//   4. CorrectifsWpf.CorrectifArbreCases.Actif = true;   // false : comportement d'origine (comparaison)
//      Lu à chaque fois : le changement agit sans recréer l'arbre.
// =====================================================================================================

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;

namespace CorrectifsWpf
{
    /// <summary>TreeView dont les nœuds (NoeudACocher) exposent le motif UIA Toggle quand le correctif est actif.</summary>
    public class CorrectifArbreCases : TreeView
    {
        /// <summary>Faux : comportement d'origine de WPF (pas de motif Toggle, pas d'événement, Espace non géré).</summary>
        public static bool Actif { get; set; }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new NoeudACocher();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is NoeudACocher;
        }

        // Les peers des nœuds racines sont créés par le peer du TreeView.
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ArbreCasesAutomationPeer(this);
        }

        internal static ToggleState EnEtat(bool? coche)
        {
            if (coche == true)
            {
                return ToggleState.On;
            }
            return coche == false ? ToggleState.Off : ToggleState.Indeterminate;
        }
    }

    /// <summary>Nœud du CorrectifArbreCases, avec l'état de sa case (IsChecked, à lier au modèle).</summary>
    public class NoeudACocher : TreeViewItem
    {
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
            "IsChecked", typeof(bool?), typeof(NoeudACocher),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, IsCheckedChange));

        public bool? IsChecked
        {
            get { return (bool?)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new NoeudACocher();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is NoeudACocher;
        }

        // Les peers des enfants sont créés par le peer du nœud parent.
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new NoeudACocherAutomationPeer(this);
        }

        // Espace sur le nœud qui a le focus (et non sur un de ses parents, que l'événement traverse aussi).
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (CorrectifArbreCases.Actif && e.Key == Key.Space && e.OriginalSource == this)
            {
                IsChecked = IsChecked != true;
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        // Quelle que soit l'origine du changement (Espace, clic sur la case, code), on prévient UIA.
        private static void IsCheckedChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!CorrectifArbreCases.Actif || !AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
            {
                return;
            }
            AutomationPeer peer = UIElementAutomationPeer.FromElement((NoeudACocher)d);
            if (peer == null)
            {
                return;
            }
            // Le peer du conteneur renvoie ses événements au peer de donnée, celui qui porte le motif Toggle.
            AutomationPeer source = peer.EventsSource ?? peer;
            source.RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty,
                CorrectifArbreCases.EnEtat((bool?)e.OldValue), CorrectifArbreCases.EnEtat((bool?)e.NewValue));
        }
    }

    internal sealed class ArbreCasesAutomationPeer : TreeViewAutomationPeer
    {
        public ArbreCasesAutomationPeer(TreeView owner) : base(owner)
        {
        }

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new NoeudACocherDataAutomationPeer(item, this, null);
        }
    }

    internal sealed class NoeudACocherAutomationPeer : TreeViewItemAutomationPeer
    {
        public NoeudACocherAutomationPeer(TreeViewItem owner) : base(owner)
        {
        }

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new NoeudACocherDataAutomationPeer(item, this, EventsSource as TreeViewDataItemAutomationPeer);
        }
    }

    /// <summary>Peer de donnée d'un nœud : c'est lui que voit le lecteur d'écran ; il ajoute le motif Toggle.</summary>
    internal sealed class NoeudACocherDataAutomationPeer : TreeViewDataItemAutomationPeer, IToggleProvider
    {
        public NoeudACocherDataAutomationPeer(object item, ItemsControlAutomationPeer itemsControlAutomationPeer,
                                              TreeViewDataItemAutomationPeer parentDataItemAutomationPeer)
            : base(item, itemsControlAutomationPeer, parentDataItemAutomationPeer)
        {
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Toggle && CorrectifArbreCases.Actif && Conteneur() != null)
            {
                return this;
            }
            return base.GetPattern(patternInterface);
        }

        public ToggleState ToggleState
        {
            get
            {
                NoeudACocher noeud = Conteneur();
                return CorrectifArbreCases.EnEtat(noeud == null ? false : noeud.IsChecked);
            }
        }

        // L'événement est levé par le changement de IsChecked (NoeudACocher.IsCheckedChange).
        public void Toggle()
        {
            NoeudACocher noeud = Conteneur();
            if (noeud != null)
            {
                noeud.IsChecked = noeud.IsChecked != true;
            }
        }

        // Le conteneur (NoeudACocher) de cette donnée, s'il est généré.
        private NoeudACocher Conteneur()
        {
            ItemsControl parent = ItemsControlAutomationPeer == null ? null : ItemsControlAutomationPeer.Owner as ItemsControl;
            return parent == null ? null : parent.ItemContainerGenerator.ContainerFromItem(Item) as NoeudACocher;
        }
    }
}
