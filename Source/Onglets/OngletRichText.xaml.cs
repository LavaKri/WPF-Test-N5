using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using CorrectifsWpf;

namespace AccessTest;

// Largeur de page du document (Specs/Fenetre.md, 2.7.4).
public enum LargeurPage
{
    Fenetre,
    Etroite,
    Large
}

// Contenu de l'onglet RichText (Specs/RichText.md). Son menu contextuel est celui, partagé avec le TextBox, de MenuEditeurs.
public partial class OngletRichText : UserControl
{
    // Levé quand le point d'insertion bouge ou que l'affichage change : la barre d'état relit la position.
    public event EventHandler PositionModifiee;

    public OngletRichText()
    {
        InitializeComponent();

        // Piège Tab de la barre de mise en forme : correctif actif selon le menu Correctif (Specs/Correctifs.md).
        CorrectifBarreOutils.Brancher(BarMiseEnForme);

        RtbEditeur.SelectionChanged += (s, e) => PositionModifiee?.Invoke(this, EventArgs.Empty);
        // Liens du document mis en forme : interceptés, aucun navigateur n'est ouvert (Specs/RichText.md, 2c).
        RtbEditeur.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(Lien_RequestNavigate));
        RtbEditeur.PreviewKeyDown += Editeur_PreviewKeyDown;
        // Barre de mise en forme : l'état suit le curseur et chaque commande d'édition, de la barre ou du clavier
        // (Specs/RichText.md, 1b). Le RichTextBox marque ses commandes traitées : handledEventsToo.
        RtbEditeur.SelectionChanged += (s, e) => MajBarreMiseEnForme();
        RtbEditeur.AddHandler(CommandManager.ExecutedEvent,
                              new ExecutedRoutedEventHandler((s, e) => MajBarreMiseEnForme()), true);
        MajBarreMiseEnForme();
    }

    public RichTextBox Editeur => RtbEditeur;

    public void AppliquerLangue()
    {
        AutomationProperties.SetName(RtbEditeur, Textes.Get("NomRiche"));
        AppliquerLangueBarre();
    }

    // Fichier > Ouvrir et Enregistrer (Specs/Fenetre.md, 2.1).
    public void Charger(string texte, bool focaliser)
    {
        // On remplit le document existant au lieu d'en poser un nouveau : le peer UI Automation du
        // RichTextBox reste lié au document d'origine, et un lecteur d'écran ne verrait plus le texte.
        FlowDocument document = RtbEditeur.Document;
        document.Blocks.Clear();
        foreach (string ligne in texte.Replace("\r\n", "\n").Split('\n'))
        {
            document.Blocks.Add(new Paragraph(new Run(ligne)) { Margin = new Thickness(0) });
        }
        RtbEditeur.CaretPosition = document.ContentStart;
        if (focaliser)
        {
            RtbEditeur.Focus();
        }
    }

    public string LireTexte()
    {
        string texte = new TextRange(RtbEditeur.Document.ContentStart, RtbEditeur.Document.ContentEnd).Text;
        // WPF termine le texte du document par un saut de ligne après le dernier paragraphe : on le retire pour
        // qu'un fichier ouvert puis enregistré garde ses lignes (Specs/Fenetre.md, 2.1.2).
        if (texte.EndsWith("\r\n", StringComparison.Ordinal))
        {
            texte = texte.Substring(0, texte.Length - 2);
        }
        return texte;
    }

    // Item « Largeur de page » (Specs/Fenetre.md, 2.7.4) : le texte revient toujours à la ligne, à la largeur de page choisie.
    public void DefinirLargeur(LargeurPage largeur)
    {
        FlowDocument document = RtbEditeur.Document;
        if (largeur == LargeurPage.Etroite)
        {
            document.PageWidth = 300;
            RtbEditeur.ClearValue(TextBoxBase.HorizontalScrollBarVisibilityProperty);
        }
        else if (largeur == LargeurPage.Large)
        {
            document.PageWidth = 2000;
            RtbEditeur.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        }
        else
        {
            document.ClearValue(FlowDocument.PageWidthProperty);
            RtbEditeur.ClearValue(TextBoxBase.HorizontalScrollBarVisibilityProperty);
        }
        PositionModifiee?.Invoke(this, EventArgs.Empty);
    }

    // Item « Document interactif » : natif, le contenu du document (un lien) n'est actif que si IsDocumentEnabled est vrai.
    public void DefinirDocumentInteractif(bool actif)
    {
        RtbEditeur.IsDocumentEnabled = actif;
    }

    // Entrée dans un lien, document interactif et sans sélection : active le lien comme un clic (Specs/RichText.md, 2c).
    // WPF ne le fait pas au clavier. Le motif Invoke du peer du lien appelle le clic de WPF (Click, RequestNavigate, commande).
    private void Editeur_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None || !RtbEditeur.IsDocumentEnabled
            || !RtbEditeur.Selection.IsEmpty)
        {
            return;
        }
        Hyperlink lien = LienSousCurseur();
        if (lien != null && ContentElementAutomationPeer.CreatePeerForElement(lien)?.GetPattern(PatternInterface.Invoke) is IInvokeProvider activation)
        {
            e.Handled = true;
            activation.Invoke();
        }
    }

    private Hyperlink LienSousCurseur()
    {
        for (DependencyObject element = RtbEditeur.CaretPosition.Parent; element is TextElement texte; element = texte.Parent)
        {
            if (element is Hyperlink lien)
            {
                return lien;
            }
        }
        return null;
    }

    // Aucun navigateur : l'adresse est affichée (Specs/RichText.md, 2c).
    private void Lien_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        e.Handled = true;
        MessageBox.Show(Window.GetWindow(this), string.Format(Textes.Get("TexteLien"), e.Uri),
            Textes.Get("TitreLien"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // Texte de la barre d'état (ligne et colonne) quand le focus est dans le RichTextBox ; nul s'il est ailleurs.
    public string Position(IInputElement focus)
    {
        if (!ReferenceEquals(focus, RtbEditeur))
        {
            return null;
        }
        TextPointer curseur = RtbEditeur.CaretPosition;

        int decalage;
        curseur.GetLineStartPosition(-int.MaxValue, out decalage);
        int ligne = 1 - decalage;

        TextPointer debutLigne = curseur.GetLineStartPosition(0);
        int colonne = debutLigne == null ? 1 : new TextRange(debutLigne, curseur).Text.Length + 1;
        return string.Format(Textes.Get("Position"), ligne, colonne);
    }

    // ===================== Barre de mise en forme =====================

    // Méthode interne de WPF, celle des commandes ToggleBold… : elle tient compte d'une mise en forme demandée sans
    // sélection et pas encore tapée, que GetPropertyValue ignore. Introuvable : GetPropertyValue seul.
    private static readonly MethodInfo ValeurCouranteSelection = typeof(TextSelection).GetMethod("GetCurrentValue",
        BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(DependencyProperty) }, null);

    // Lettres, noms et infobulles des boutons ; le raccourci vient de la commande, dans la langue de l'interface.
    private void AppliquerLangueBarre()
    {
        AutomationProperties.SetName(BarMiseEnForme, Textes.Get("BarMiseEnForme"));
        LettreGras.Text = Textes.Get("LettreGras");
        LettreItalique.Text = Textes.Get("LettreItalique");
        LettreSouligne.Text = Textes.Get("LettreSouligne");
        NommerBouton(TglGras, "BarGras", Key.B);
        NommerBouton(TglItalique, "BarItalique", Key.I);
        NommerBouton(TglSouligne, "BarSouligne", Key.U);
        NommerBouton(RdoGauche, "BarGauche", Key.L);
        NommerBouton(RdoCentre, "BarCentre", Key.E);
        NommerBouton(RdoDroite, "BarDroite", Key.R);
    }

    // Raccourcis de l'éditeur de WPF (Ctrl+lettre). WPF ne les range pas dans la commande (InputGestures vide pour
    // les EditingCommands) : ils sont déclarés ici. Sans AcceleratorKey, WPF exposerait le texte anglais de la
    // commande (« Toggle bold ») comme raccourci (Specs/RichText.md, 1b).
    private static void NommerBouton(ButtonBase bouton, string cle, Key touche)
    {
        string nom = Textes.Get(cle);
        string raccourci = new KeyGesture(touche, ModifierKeys.Control).GetDisplayStringForCulture(CultureInfo.CurrentUICulture);
        AutomationProperties.SetName(bouton, nom);
        AutomationProperties.SetAcceleratorKey(bouton, raccourci);
        bouton.ToolTip = string.Format(Textes.Get("BarInfobulle"), nom, raccourci);
        // Sans texte d'aide, WPF exposerait l'infobulle, qui répète le raccourci : NVDA le lisait deux fois.
        // Une seule espace n'est pas lue par NVDA et empêche ce repli.
        AutomationProperties.SetHelpText(bouton, " ");
    }

    private object ValeurMiseEnForme(DependencyProperty propriete)
    {
        TextSelection selection = RtbEditeur.Selection;
        return ValeurCouranteSelection != null
            ? ValeurCouranteSelection.Invoke(selection, new object[] { propriete })
            : selection.GetPropertyValue(propriete);
    }

    // Sélection mélangée : GetPropertyValue rend UnsetValue, le bouton est relâché (aucun alignement coché).
    private void MajBarreMiseEnForme()
    {
        TglGras.IsChecked = Equals(ValeurMiseEnForme(TextElement.FontWeightProperty), FontWeights.Bold);
        TglItalique.IsChecked = Equals(ValeurMiseEnForme(TextElement.FontStyleProperty), FontStyles.Italic);
        TglSouligne.IsChecked = ValeurMiseEnForme(Inline.TextDecorationsProperty) is TextDecorationCollection decorations
                                && decorations.Any(d => d.Location == TextDecorationLocation.Underline);
        object alignement = RtbEditeur.Selection.GetPropertyValue(Block.TextAlignmentProperty);
        RdoGauche.IsChecked = Equals(alignement, TextAlignment.Left);
        RdoCentre.IsChecked = Equals(alignement, TextAlignment.Center);
        RdoDroite.IsChecked = Equals(alignement, TextAlignment.Right);
    }

    // ===================== Document mis en forme =====================

    // Contenu du RichTextBox au démarrage. Le document existant est vidé puis rempli, jamais remplacé (le peer UIA lui
    // reste lié). Textes dans la langue de démarrage, une ligne vide entre deux blocs (Specs/RichText.md, 2c).
    public void ChargerDocumentForme()
    {
        BlockCollection blocs = RtbEditeur.Document.Blocks;
        blocs.Clear();

        // Titre de niveau 1 pour les lecteurs d'écran, avec le correctif de lecture (Specs/Correctifs.md).
        Paragraph titre = new Paragraph(new Run(Textes.Get("DocTitre"))) { FontSize = 20, FontWeight = FontWeights.Bold };
        CorrectifLectureRiche.SetNiveauTitre(titre, 1);
        blocs.Add(titre);
        blocs.Add(new Paragraph());   // ligne vide entre deux blocs

        Paragraph forme = new Paragraph();
        forme.Inlines.Add(new Run(Textes.Get("DocFormeDebut")));
        forme.Inlines.Add(new Bold(new Run(Textes.Get("DocGras"))));
        forme.Inlines.Add(new Run(Textes.Get("DocFormeMilieu")));
        forme.Inlines.Add(new Italic(new Run(Textes.Get("DocItalique"))));
        forme.Inlines.Add(new Run(Textes.Get("DocFormeEt")));
        forme.Inlines.Add(new Underline(new Run(Textes.Get("DocSouligne"))));
        forme.Inlines.Add(new Run("."));
        blocs.Add(forme);
        blocs.Add(new Paragraph());   // ligne vide entre deux blocs

        blocs.Add(TitreNiveau2("DocTitreListes"));
        blocs.Add(new Paragraph(new Run(Textes.Get("DocListePuces"))));
        blocs.Add(CreerListe(TextMarkerStyle.Disc, "DocPuce1", "DocPuce2", "DocPuce3"));
        blocs.Add(new Paragraph());   // ligne vide entre deux blocs
        blocs.Add(new Paragraph(new Run(Textes.Get("DocListeNumeros"))));
        blocs.Add(CreerListe(TextMarkerStyle.Decimal, "DocNumero1", "DocNumero2", "DocNumero3"));
        blocs.Add(new Paragraph());   // ligne vide entre deux blocs

        blocs.Add(TitreNiveau2("DocTitreTableau"));
        blocs.Add(new Paragraph(new Run(Textes.Get("DocTableau"))));
        blocs.Add(CreerTableau());
        blocs.Add(new Paragraph());   // ligne vide entre deux blocs

        blocs.Add(TitreNiveau2("DocTitreLiens"));
        Paragraph lien = new Paragraph();
        lien.Inlines.Add(new Run(Textes.Get("DocLienAvant")));
        lien.Inlines.Add(new Hyperlink(new Run(Textes.Get("DocLien"))) { NavigateUri = new Uri("https://www.nvaccess.org/") });
        lien.Inlines.Add(new Run("."));
        blocs.Add(lien);
        blocs.Add(new Paragraph());   // ligne vide entre deux blocs

        blocs.Add(new Paragraph(new Run(Textes.Get("DocFin"))));

        RtbEditeur.CaretPosition = RtbEditeur.Document.ContentStart;
    }

    // Titre de section, suivi directement de son bloc (sans ligne vide).
    private static Paragraph TitreNiveau2(string cle)
    {
        Paragraph titre = new Paragraph(new Run(Textes.Get(cle))) { FontSize = 16, FontWeight = FontWeights.Bold };
        CorrectifLectureRiche.SetNiveauTitre(titre, 2);
        return titre;
    }

    private static List CreerListe(TextMarkerStyle style, params string[] cles)
    {
        List liste = new List { MarkerStyle = style };
        foreach (string cle in cles)
        {
            liste.ListItems.Add(new ListItem(new Paragraph(new Run(Textes.Get(cle)))));
        }
        return liste;
    }

    // Trois colonnes, ligne d'en-tête en gras ; bordures de la couleur du texte (lisibles en Contraste élevé).
    private static Table CreerTableau()
    {
        Table tableau = new Table { CellSpacing = 0 };
        // Première ligne = en-têtes de colonne, pour les lecteurs d'écran (correctif de lecture, Specs/Correctifs.md).
        CorrectifLectureRiche.SetLigneEntete(tableau, true);
        for (int i = 0; i < 3; i++)
        {
            tableau.Columns.Add(new TableColumn());
        }
        TableRowGroup groupe = new TableRowGroup();
        string[][] lignes =
        {
            new[] { "DocProduit", "DocQuantite", "DocPrix" },
            new[] { "DocPuce1", "3", "DocPrix1" },
            new[] { "DocPuce2", "2", "DocPrix2" },
            new[] { "DocPuce3", "1", "DocPrix3" },
        };
        for (int l = 0; l < lignes.Length; l++)
        {
            TableRow ligne = new TableRow();
            if (l == 0)
            {
                ligne.FontWeight = FontWeights.Bold;
            }
            foreach (string cle in lignes[l])
            {
                string texte = char.IsDigit(cle[0]) ? cle : Textes.Get(cle);
                TableCell cellule = new TableCell(new Paragraph(new Run(texte)))
                {
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(4, 2, 4, 2),
                };
                cellule.SetResourceReference(TableCell.BorderBrushProperty, SystemColors.WindowTextBrushKey);
                ligne.Cells.Add(cellule);
            }
            groupe.Rows.Add(ligne);
        }
        tableau.RowGroups.Add(groupe);
        return tableau;
    }
}
