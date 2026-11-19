using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using CorrectifsWpf;

namespace AccessTest;

// Contenu de l'onglet TreeView (Specs/TreeView.md).
public partial class OngletTreeView : UserControl
{
    // Une copie de la source par arbre (Specs/TreeView.md, 2).
    private readonly ObservableCollection<Noeud> sourceMenu = Noeud.CreerSource();
    private readonly ObservableCollection<Noeud> sourceCases = Noeud.CreerSource();
    private int compteurNoeuds;

    public OngletTreeView()
    {
        InitializeComponent();

        ArbMenu.ItemsSource = sourceMenu;
        ArbCases.ItemsSource = sourceCases;
        // « Corriger les cases » est coché dans le XAML : gestionnaires branchés ici pour ne pas se déclencher
        // pendant InitializeComponent, puis réglage appliqué une première fois.
        MnuCasCorriger.Checked += CasCorriger_Change;
        MnuCasCorriger.Unchecked += CasCorriger_Change;
        CasCorriger_Change(null, null);
        MnuCasPropager.Checked += CasPropager_Change;
        MnuCasPropager.Unchecked += CasPropager_Change;
    }

    // Les nœuds tirent leur nom de Noeud.ToString() : aucun nom accessible n'est posé dessus (Specs/TreeView.md, 2).
    public void AppliquerLangue()
    {
        LblArbMenu.Text = Textes.Get("TvArbre");
        LblArbCases.Text = Textes.Get("TvArbreCases");
        MnuArbDevelopper.Header = Textes.Get("TvDevelopper");
        MnuArbReduire.Header = Textes.Get("TvReduire");
        MnuArbAjouter.Header = Textes.Get("TvAjouter");
        MnuArbProfond.Header = Textes.Get("TvProfond");
        MnuCasDevelopper.Header = Textes.Get("TvDevelopper");
        MnuCasReduire.Header = Textes.Get("TvReduire");
        MnuCasAjouter.Header = Textes.Get("TvAjouter");
        MnuCasProfond.Header = Textes.Get("TvProfond");
        MnuCasCorriger.Header = Textes.Get("TvCorriger");
        MnuCasPropager.Header = Textes.Get("TvPropager");

        AutomationProperties.SetName(ArbMenu, Textes.Get("TvArbre"));
        AutomationProperties.SetName(ArbCases, Textes.Get("TvArbreCases"));

        Noeud.RafraichirNoms(sourceMenu);
        Noeud.RafraichirNoms(sourceCases);
    }

    // Les quatre items sont communs aux deux menus : l'arbre visé est celui du menu qui contient l'item.
    private TreeView ArbreDe(object item)
    {
        return ItemsControl.ItemsControlFromItemContainer((DependencyObject)item) == CtxCases ? ArbCases : ArbMenu;
    }

    private ObservableCollection<Noeud> SourceDe(TreeView arbre)
    {
        return arbre == ArbCases ? sourceCases : sourceMenu;
    }

    private void ArbDevelopper_Click(object sender, RoutedEventArgs e)
    {
        DefinirDeveloppe(SourceDe(ArbreDe(sender)), true);
    }

    // Si le nœud sélectionné se retrouve caché, on ne fait rien de plus : c'est le comportement natif à observer.
    private void ArbReduire_Click(object sender, RoutedEventArgs e)
    {
        DefinirDeveloppe(SourceDe(ArbreDe(sender)), false);
    }

    // Enfant du nœud sélectionné, sinon nouveau nœud racine. Le parent est développé, le nouveau nœud est
    // sélectionné et reçoit le focus.
    private void ArbAjouter_Click(object sender, RoutedEventArgs e)
    {
        TreeView arbre = ArbreDe(sender);
        ObservableCollection<Noeud> source = SourceDe(arbre);
        Noeud parent = arbre.SelectedItem as Noeud;
        Noeud nouveau = new Noeud("TvNouveau", ++compteurNoeuds);
        (parent == null ? source : parent.Enfants).Add(nouveau);
        if (parent != null)
        {
            parent.EstDeveloppe = true;
        }
        SelectionnerEtFocaliser(arbre, nouveau);
    }

    // Premier nœud du niveau le plus profond (« Archives » tant qu'on n'a rien ajouté).
    private void ArbProfond_Click(object sender, RoutedEventArgs e)
    {
        TreeView arbre = ArbreDe(sender);
        ObservableCollection<Noeud> source = SourceDe(arbre);
        Noeud profond = null;
        int niveauProfond = -1;
        ChercherPlusProfond(source, 0, ref profond, ref niveauProfond);
        if (profond == null)
        {
            return;
        }
        DevelopperJusqua(source, profond);
        SelectionnerEtFocaliser(arbre, profond);
    }

    // « Corriger les cases » : la case devient non focalisable (liaison à CorrectionCases) et le correctif
    // ajoute le motif UIA Toggle aux nœuds et gère Espace (Correctifs/CorrectifArbreCases.cs).
    // Coché : propagation aux enfants et état indéterminé des parents, recalculés une fois en cochant (Specs/TreeView.md, 5.1).
    private void CasPropager_Change(object sender, RoutedEventArgs e)
    {
        Noeud.Propager = MnuCasPropager.IsChecked;
        if (Noeud.Propager)
        {
            Noeud.RecalculerParents(sourceCases);
        }
    }

    private void CasCorriger_Change(object sender, RoutedEventArgs e)
    {
        CorrectionCases.Instance.Actif = MnuCasCorriger.IsChecked;
        CorrectifArbreCases.Actif = MnuCasCorriger.IsChecked;
    }

    private static void DefinirDeveloppe(IEnumerable<Noeud> noeuds, bool developpe)
    {
        foreach (Noeud noeud in noeuds)
        {
            noeud.EstDeveloppe = developpe;
            DefinirDeveloppe(noeud.Enfants, developpe);
        }
    }

    private static void ChercherPlusProfond(IEnumerable<Noeud> noeuds, int niveau, ref Noeud profond, ref int niveauProfond)
    {
        foreach (Noeud noeud in noeuds)
        {
            if (niveau > niveauProfond)
            {
                profond = noeud;
                niveauProfond = niveau;
            }
            ChercherPlusProfond(noeud.Enfants, niveau + 1, ref profond, ref niveauProfond);
        }
    }

    // Développe les ancêtres de la cible ; renvoie vrai si elle a été trouvée.
    private static bool DevelopperJusqua(IEnumerable<Noeud> noeuds, Noeud cible)
    {
        foreach (Noeud noeud in noeuds)
        {
            if (noeud == cible)
            {
                return true;
            }
            if (DevelopperJusqua(noeud.Enfants, cible))
            {
                noeud.EstDeveloppe = true;
                return true;
            }
        }
        return false;
    }

    // Le TreeViewItem n'existe qu'une fois ses parents développés et la mise en page faite : on attend.
    private void SelectionnerEtFocaliser(TreeView arbre, Noeud noeud)
    {
        noeud.EstSelectionne = true;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            arbre.UpdateLayout();
            TreeViewItem conteneur = TrouverConteneur(arbre, noeud);
            if (conteneur != null)
            {
                conteneur.Focus();
                conteneur.BringIntoView();
            }
        }), DispatcherPriority.Loaded);
    }

    private static TreeViewItem TrouverConteneur(ItemsControl parent, Noeud cible)
    {
        foreach (object element in parent.Items)
        {
            TreeViewItem conteneur = parent.ItemContainerGenerator.ContainerFromItem(element) as TreeViewItem;
            if (conteneur == null)
            {
                continue;
            }
            if (element == cible)
            {
                return conteneur;
            }
            if (conteneur.IsExpanded)
            {
                conteneur.UpdateLayout();
                TreeViewItem trouve = TrouverConteneur(conteneur, cible);
                if (trouve != null)
                {
                    return trouve;
                }
            }
        }
        return null;
    }
}
