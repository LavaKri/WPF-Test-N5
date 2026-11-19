using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using CorrectifsWpf;

namespace AccessTest;

// Contenu de l'onglet DataGrid (Specs/DataGrid.md).
public partial class OngletDataGrid : UserControl
{
    // Les 30 commandes (Specs/DataGrid.md, 2).
    private readonly ObservableCollection<Commande> commandes = Commande.CreerSource();

    // Levé quand la cellule courante change : la barre d'état relit la position (Specs/Fenetre.md, 4).
    public event EventHandler PositionModifiee;

    public OngletDataGrid()
    {
        InitializeComponent();

        // Les items du menu sont cochés dans le XAML : gestionnaires branchés ici, puis réglages appliqués
        // une première fois.
        GrdCommandes.ItemsSource = commandes;
        ColStatut.ItemsSource = OptionStatut.Liste;
        GrdCommandes.AddingNewItem += Commandes_AddingNewItem;
        GrdCommandes.CurrentCellChanged += (s, e) => PositionModifiee?.Invoke(this, EventArgs.Empty);
        CorrectifGrille.Brancher(GrdCommandes);
        MnuGrlUnArret.Checked += GrilleReglages_Change;
        MnuGrlUnArret.Unchecked += GrilleReglages_Change;
        MnuGrlEntetes.Checked += GrilleReglages_Change;
        MnuGrlEntetes.Unchecked += GrilleReglages_Change;
        MnuGrlUneLecture.Checked += GrilleReglages_Change;
        MnuGrlUneLecture.Unchecked += GrilleReglages_Change;
        GrilleReglages_Change(null, null);
        MnuGrlNoms.Checked += (s, e) => AppliquerNomsCellules();
        MnuGrlNoms.Unchecked += (s, e) => AppliquerNomsCellules();
        AppliquerNomsCellules();
    }

    // Les lignes tirent leur nom de Commande.ToString(), les cellules de leur contenu (Specs/DataGrid.md, 8).
    public void AppliquerLangue()
    {
        LblCommandes.Text = Textes.Get("DgCommandes");
        AutomationProperties.SetName(GrdCommandes, Textes.Get("DgCommandes"));
        ColNumero.Header = Textes.Get("DgColNumero");
        ColClient.Header = Textes.Get("DgColClient");
        ColDate.Header = Textes.Get("DgColDate");
        ColMontant.Header = Textes.Get("DgColMontant");
        ColPayee.Header = Textes.Get("DgColPayee");
        ColStatut.Header = Textes.Get("DgColStatut");
        ColPriorite.Header = Textes.Get("DgColPriorite");
        MnuGrlUnArret.Header = Textes.Get("DgUnArret");
        MnuGrlEntetes.Header = Textes.Get("DgEntetes");
        MnuGrlNoms.Header = Textes.Get("DgNoms");
        MnuGrlUneLecture.Header = Textes.Get("DgUneLecture");
        OptionStatut.RafraichirTextes();

        CorrectifGrille.TexteCroissant = Textes.Get("DgTriCroissant");
        CorrectifGrille.TexteDecroissant = Textes.Get("DgTriDecroissant");
        CorrectifGrille.MettreAJourTri(GrdCommandes);

        // Dates, montants et noms des lignes sont mis en forme au dessin de la ligne : on redessine, après avoir
        // validé une édition en cours (Refresh est refusé pendant une édition).
        GrdCommandes.CommitEdit(DataGridEditingUnit.Row, true);
        GrdCommandes.Items.Refresh();
    }

    // Ligne vide d'ajout : la nouvelle commande reçoit le numéro suivant et la date du jour.
    private void Commandes_AddingNewItem(object sender, AddingNewItemEventArgs e)
    {
        int numero = commandes.Count == 0 ? 1001 : commandes.Max(c => c.Numero) + 1;
        e.NewItem = new Commande { Numero = numero, Date = DateTime.Today, Statut = StatutCommande.EnAttente };
    }

    private void GrilleReglages_Change(object sender, RoutedEventArgs e)
    {
        CorrectifGrille.UnArret = MnuGrlUnArret.IsChecked;
        CorrectifGrille.EntetesFocalisables = MnuGrlEntetes.IsChecked;
        CorrectifGrille.UneLecture = MnuGrlUneLecture.IsChecked;
    }

    // Coché : les cellules sans texte affiché propre ont un nom et une valeur utiles (Specs/DataGrid.md, 7) :
    // Priorité « Prioritaire » / « Normale » ; Statut : le statut, valeur vide (lu une fois) ; Payée : nom d'une
    // espace (non lu, mais non vide : WPF ne le remplace pas par son nom technique), valeur « coché » / « non
    // coché », pour que le changement par Espace reste annoncé (il passe par la valeur). Décoché : natif.
    // Noms et valeurs sont traduits au redessin des lignes.
    private void AppliquerNomsCellules()
    {
        if (MnuGrlNoms.IsChecked)
        {
            ColPriorite.CellStyle = StyleNomCellule(new Binding(nameof(Commande.Prioritaire)) { Converter = new PrioriteVersNom() });
            ColStatut.CellStyle = StyleNomCellule(new Binding(nameof(Commande.Statut)) { Converter = new StatutVersTexte() });
            ColStatut.ClipboardContentBinding = new Binding { Source = string.Empty, Mode = BindingMode.OneWay };
            ColPayee.CellStyle = StyleNomCellule(" ");
            ColPayee.ClipboardContentBinding = new Binding(nameof(Commande.Payee)) { Converter = new PayeeVersEtat(), Mode = BindingMode.OneWay };
        }
        else
        {
            ColPriorite.ClearValue(DataGridColumn.CellStyleProperty);
            ColStatut.ClearValue(DataGridColumn.CellStyleProperty);
            ColPayee.ClearValue(DataGridColumn.CellStyleProperty);
            // Nulle, la liaison de copie retombe sur celle de la colonne (comportement d'origine).
            ColStatut.ClipboardContentBinding = null;
            ColPayee.ClipboardContentBinding = null;
        }
        GrdCommandes.CommitEdit(DataGridEditingUnit.Row, true);
        GrdCommandes.Items.Refresh();
    }

    // Style de cellule qui pose le nom accessible (valeur fixe ou liaison aux données).
    private static Style StyleNomCellule(object nom)
    {
        Style style = new Style(typeof(DataGridCell));
        style.Setters.Add(new Setter(AutomationProperties.NameProperty, nom));
        return style;
    }

    // Texte de la barre d'état quand le focus est dans la grille : « Commande 1003, Montant », ou le texte de
    // l'en-tête focalisé. Nul si le focus est ailleurs.
    public string Position(IInputElement focus)
    {
        if (!(focus is Visual visuel && GrdCommandes.IsAncestorOf(visuel)))
        {
            return null;
        }
        if (focus is DataGridColumnHeader entete)
        {
            return entete.Column?.Header as string ?? "";
        }
        DataGridCellInfo cellule = GrdCommandes.CurrentCell;
        if (!cellule.IsValid)
        {
            return "";
        }
        string ligne = cellule.Item == CollectionView.NewItemPlaceholder ? Textes.Get("DgNouvelleLigne") : cellule.Item.ToString();
        return string.Format(Textes.Get("DgPosition"), ligne, cellule.Column.Header);
    }
}
