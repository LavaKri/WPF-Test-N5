using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

namespace AccessTest;

// Menu contextuel du TextBox et du RichTextBox : correcteur orthographique, tabulation, et les réglages propres à
// chaque zone (retour à la ligne, largeur de page, sous-menu Correctif), qu'il applique par les méthodes des deux onglets.
public partial class MenuEditeurs : ContextMenu
{
    private OngletTextBox texte;
    private OngletRichText riche;

    // Items ajoutés en tête du menu à chaque ouverture (suggestions, « Ignorer tout »).
    private readonly List<Control> itemsOrthographe = new List<Control>();

    // Les items cochés dans le XAML ont leurs gestionnaires branchés ici, pour ne pas se déclencher pendant InitializeComponent.
    public MenuEditeurs()
    {
        InitializeComponent();

        MnuRetourLigne.Checked += (s, e) => texte.DefinirRetourLigne(true);
        MnuRetourLigne.Unchecked += (s, e) => texte.DefinirRetourLigne(false);
        foreach (MenuItem item in new[] { MnuLargeurFenetre, MnuLargeurEtroite, MnuLargeurLarge })
        {
            item.Checked += Largeur_Checked;
            item.Unchecked += Largeur_Unchecked;
        }
        MnuDocInteractif.Checked += DocInteractif_Change;
        MnuDocInteractif.Unchecked += DocInteractif_Change;
    }

    // Pose le menu sur les deux zones de texte et l'ouvre pour elles (suggestions recalculées à chaque ouverture).
    public void Brancher(OngletTextBox ongletTexte, OngletRichText ongletRiche)
    {
        texte = ongletTexte;
        riche = ongletRiche;
        foreach (Control editeur in new Control[] { texte.Editeur, riche.Editeur })
        {
            editeur.ContextMenu = this;
            editeur.ContextMenuOpening += Editeur_ContextMenuOpening;
        }
        texte.DefinirRetourLigne(MnuRetourLigne.IsChecked);
        riche.DefinirDocumentInteractif(MnuDocInteractif.IsChecked);
    }

    // Sous-menu Correctif (Specs/Fenetre.md, 2.7.5) : même réglage que l'item du menu principal, lié dans les deux
    // sens. Seul l'item principal applique le correctif (ses Checked / Unchecked) ; un échec le décoche, et celui-ci avec.
    public void LierLectureRiche(MenuItem itemPrincipal)
    {
        MnuCtxLectureRiche.SetBinding(MenuItem.IsCheckedProperty,
            new Binding(nameof(MenuItem.IsChecked)) { Source = itemPrincipal, Mode = BindingMode.TwoWay });
    }

    public void AppliquerLangue()
    {
        MnuCtxCorrectif.Header = Textes.Get("Correctif");
        MnuCtxLectureRiche.Header = Textes.Get("CorrLectureRiche");
        MnuDocInteractif.Header = Textes.Get("DocInteractif");
        MnuOrthographe.Header = Textes.Get("Orthographe");
        MnuTabulation.Header = Textes.Get("Tabulation");
        MnuFauteSuivante.Header = Textes.Get("FauteSuivante");
        MnuRetourLigne.Header = Textes.Get("RetourLigne");
        MnuLargeur.Header = Textes.Get("Largeur");
        MnuLargeurFenetre.Header = Textes.Get("LargeurFenetre");
        MnuLargeurEtroite.Header = Textes.Get("LargeurEtroite");
        MnuLargeurLarge.Header = Textes.Get("LargeurLarge");
    }

    public bool OrthographeActive => MnuOrthographe.IsChecked;

    private void Orthographe_Change(object sender, RoutedEventArgs e)
    {
        bool actif = MnuOrthographe.IsChecked;
        SpellCheck.SetIsEnabled(texte.Editeur, actif);
        SpellCheck.SetIsEnabled(riche.Editeur, actif);
        MnuFauteSuivante.IsEnabled = actif;
    }

    // Décoché (au démarrage) : Tab quitte la zone de texte. Coché : Tab insère une tabulation (Specs/Fenetre.md, 2.7).
    private void Tabulation_Change(object sender, RoutedEventArgs e)
    {
        bool actif = MnuTabulation.IsChecked;
        texte.Editeur.AcceptsTab = actif;
        riche.Editeur.AcceptsTab = actif;
    }

    // Choix exclusif : cocher un item décoche les autres.
    private void Largeur_Checked(object sender, RoutedEventArgs e)
    {
        foreach (MenuItem item in new[] { MnuLargeurFenetre, MnuLargeurEtroite, MnuLargeurLarge })
        {
            if (item != sender)
            {
                item.IsChecked = false;
            }
        }

        riche.DefinirLargeur(sender == MnuLargeurEtroite ? LargeurPage.Etroite
                           : sender == MnuLargeurLarge ? LargeurPage.Large
                           : LargeurPage.Fenetre);
    }

    // Décocher l'item coché le recoche : il en faut toujours un.
    private void Largeur_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!MnuLargeurFenetre.IsChecked && !MnuLargeurEtroite.IsChecked && !MnuLargeurLarge.IsChecked)
        {
            ((MenuItem)sender).IsChecked = true;
        }
    }

    private void DocInteractif_Change(object sender, RoutedEventArgs e)
    {
        riche.DefinirDocumentInteractif(MnuDocInteractif.IsChecked);
    }

    // ===================== Faute suivante =====================

    // Le menu est ouvert depuis la zone de texte de l'onglet affiché : c'est elle que vise l'item.
    private void FauteSuivante_Click(object sender, RoutedEventArgs e)
    {
        FauteSuivante(PlacementTarget == riche.Editeur);
    }

    // Sélectionne le mot mal orthographié qui suit la sélection (ou le curseur), en reprenant au début au bout du texte.
    public void FauteSuivante(bool dansRiche)
    {
        bool trouve = dansRiche ? FauteSuivanteRiche() : FauteSuivanteTexte();
        if (!trouve)
        {
            System.Media.SystemSounds.Beep.Play();
        }
    }

    private bool FauteSuivanteTexte()
    {
        TextBox editeur = texte.Editeur;
        int depart = editeur.SelectionStart + editeur.SelectionLength;
        int index = editeur.GetNextSpellingErrorCharacterIndex(depart, LogicalDirection.Forward);
        if (index < 0 && depart > 0)
        {
            index = editeur.GetNextSpellingErrorCharacterIndex(0, LogicalDirection.Forward);
            // Un mot fautif qui commence à la position 0 n'est pas « suivant » de la position 0 : on le teste aussi.
            if (index < 0 && editeur.GetSpellingError(0) != null)
            {
                index = 0;
            }
        }
        if (index < 0)
        {
            return false;
        }

        editeur.Focus();
        editeur.Select(index, Math.Max(1, editeur.GetSpellingErrorLength(index)));
        editeur.ScrollToLine(editeur.GetLineIndexFromCharacterIndex(index));
        return true;
    }

    private bool FauteSuivanteRiche()
    {
        RichTextBox editeur = riche.Editeur;
        TextPointer position = editeur.GetNextSpellingErrorPosition(editeur.Selection.End, LogicalDirection.Forward);
        if (position == null)
        {
            position = editeur.GetNextSpellingErrorPosition(editeur.Document.ContentStart, LogicalDirection.Forward);
        }
        TextRange faute = position == null ? null : editeur.GetSpellingErrorRange(position);
        if (faute == null)
        {
            return false;
        }

        editeur.Focus();
        editeur.Selection.Select(faute.Start, faute.End);
        faute.Start.Paragraph?.BringIntoView();
        return true;
    }

    // ===================== Ouverture =====================

    // Le menu est partagé par les deux zones de texte : les suggestions sont recalculées pour le contrôle
    // qui l'ouvre, et retirées à l'ouverture suivante.
    private void Editeur_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        MnuRetourLigne.Visibility = sender == texte.Editeur ? Visibility.Visible : Visibility.Collapsed;
        MnuLargeur.Visibility = sender == riche.Editeur ? Visibility.Visible : Visibility.Collapsed;
        MnuCtxCorrectif.Visibility = MnuLargeur.Visibility;

        foreach (Control ancien in itemsOrthographe)
        {
            Items.Remove(ancien);
        }
        itemsOrthographe.Clear();

        if (!MnuOrthographe.IsChecked)
        {
            return;
        }
        SpellingError erreur = ErreurSelonWpf(sender);
        if (erreur == null)
        {
            return;
        }

        IInputElement cible = (IInputElement)sender;
        // Comme le menu natif de WPF : les commandes retrouvent la faute par la même détection.
        // Le « _ » d'un en-tête de MenuItem est une touche d'accès : on le double pour le garder tel quel.
        MenuItem suggestions = new MenuItem { Header = Textes.Get("OrthoSuggestions") };
        foreach (string suggestion in erreur.Suggestions)
        {
            suggestions.Items.Add(new MenuItem
            {
                Header = suggestion.Replace("_", "__"),
                Command = EditingCommands.CorrectSpellingError,
                CommandParameter = suggestion,
                CommandTarget = cible
            });
        }
        if (suggestions.Items.Count == 0)
        {
            suggestions.Items.Add(new MenuItem { Header = Textes.Get("OrthoAucune"), IsEnabled = false });
        }
        itemsOrthographe.Add(suggestions);
        itemsOrthographe.Add(new MenuItem
        {
            Header = Textes.Get("OrthoIgnorer"),
            Command = EditingCommands.IgnoreSpellingError,
            CommandTarget = cible
        });
        itemsOrthographe.Add(new Separator());

        for (int i = 0; i < itemsOrthographe.Count; i++)
        {
            Items.Insert(i, itemsOrthographe[i]);
        }
    }

    // Détection de WPF, sans modification (Specs/Fenetre.md, 2.7.1) : TextEditor.GetSpellingErrorAtSelection, méthode
    // interne du menu natif et des commandes CorrectSpellingError / IgnoreSpellingError, appelée par réflexion.
    // Au clic droit, WPF a déjà placé le curseur sous la souris avant ce gestionnaire.
    private static readonly Type TypeEditeur = typeof(TextBoxBase).Assembly.GetType("System.Windows.Documents.TextEditor");
    private static readonly MethodInfo LireEditeur =
        TypeEditeur?.GetMethod("_GetTextEditor", BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly MethodInfo LireFaute =
        TypeEditeur?.GetMethod("GetSpellingErrorAtSelection", BindingFlags.NonPublic | BindingFlags.Instance);

    private static SpellingError ErreurSelonWpf(object controle)
    {
        if (LireEditeur == null || LireFaute == null)
        {
            return null;
        }
        object editeur = LireEditeur.Invoke(null, new[] { controle });
        return editeur == null ? null : LireFaute.Invoke(editeur, null) as SpellingError;
    }
}
