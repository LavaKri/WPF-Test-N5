using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using CorrectifsWpf;

namespace AccessTest;

public partial class MainWindow
{
    private static readonly string Framework = RuntimeInformation.FrameworkDescription;

    public MainWindow()
    {
        DefinirCulture("FR");
        InitializeComponent();

        PreviewKeyDown += Fenetre_PreviewKeyDown;
        AddHandler(Keyboard.GotKeyboardFocusEvent,
                   new KeyboardFocusChangedEventHandler((s, e) => MajStatut(e.NewFocus)));

        // Chaque onglet est un UserControl (dossier Onglets) qui s'initialise seul. Ici, seulement ce qui relie les
        // onglets à la fenêtre : le menu contextuel des deux zones de texte et la barre d'état.
        menuEditeurs.Brancher(ContenuTextBox, ContenuRichText);
        menuEditeurs.LierLectureRiche(MnuLectureRiche);
        BrancherItemsCorrectifs();
        ContenuTextBox.PositionModifiee += (s, e) => MajPosition(Keyboard.FocusedElement);
        ContenuRichText.PositionModifiee += (s, e) => MajPosition(Keyboard.FocusedElement);
        ContenuDataGrid.PositionModifiee += (s, e) => MajPosition(Keyboard.FocusedElement);

        // Menu Fichier : OpenFolderDialog absente en .NET Framework 4.8 (Specs/Fenetre.md, 2.1.3).
        MnuChoixDossier.IsEnabled = TypeDialogueDossier != null;
        dossierDepart = TrouverDossier("Docs");
        AppliquerLangue("FR");
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Le focus démarre sur l'en-tête de l'onglet (Specs/Fenetre.md, 3), pas dans l'éditeur.
        TabTextBox.Focus();

        // Contenu des deux zones de texte, sans leur donner le focus (Specs/Fenetre.md, 2.5).
        if (!ContenuTextBox.ChargerLisezMoi())
        {
            AfficherErreur(string.Format(Textes.Get("Introuvable"), "Lisez-moi.txt"));
        }
        ContenuRichText.ChargerDocumentForme();

        // Les huit correctifs sont actifs dès le démarrage (Specs/Correctifs.md). Cocher l'item déclenche
        // Checked, qui applique le correctif ; en cas d'échec, le gestionnaire redécoche l'item.
        MnuLignesVides.IsChecked = true;
        MnuBarreEtat.IsChecked = true;
        MnuCalendrier.IsChecked = true;
        MnuBarreOutils.IsChecked = true;
        MnuItemsIndisponibles.IsChecked = true;
        MnuRaccourcisMenus.IsChecked = true;
        MnuLectureRiche.IsChecked = true;
        MnuTouchesAcces.IsChecked = true;
    }

    // Rectangle UIA de la zone cliente quand le correctif est actif, pour que NVDA+Fin trouve la barre d'état.
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new CorrectifBarreEtat(this);
    }

    // ===================== Langue =====================

    private static void DefinirCulture(string code)
    {
        var culture = new CultureInfo(code == "EN" ? "en-US" : "fr-FR");
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    private void AppliquerLangue(string code)
    {
        Textes.Langue = code;
        DefinirCulture(code);
        Language = XmlLanguage.GetLanguage(code == "EN" ? "en-US" : "fr-FR");

        MnuFR.IsChecked = code == "FR";
        MnuEN.IsChecked = code == "EN";

        Title = string.Format(Textes.Get("Titre"), Textes.NomApplication, Framework);
        MnuFichier.Header = Textes.Get("Fichier");
        MnuOuvrir.Header = Textes.Get("Ouvrir");
        MnuEnregistrer.Header = Textes.Get("Enregistrer");
        MnuChoixDossier.Header = Textes.Get("ChoixDossier");
        MnuToujoursGrisee.Header = Textes.Get("ToujoursGrisee");
        MnuLangue.Header = Textes.Get("Langue");
        MnuQuitter.Header = Textes.Get("Quitter");
        MnuEdition.Header = Textes.Get("Edition");
        MnuAnnuler.Header = Textes.Get("Annuler");
        MnuRetablir.Header = Textes.Get("Retablir");
        MnuCouper.Header = Textes.Get("Couper");
        MnuCopier.Header = Textes.Get("Copier");
        MnuColler.Header = Textes.Get("Coller");
        MnuToutSel.Header = Textes.Get("ToutSel");
        MnuCorrectif.Header = Textes.Get("Correctif");
        MnuLignesVides.Header = Textes.Get("LignesVides");
        MnuBarreEtat.Header = Textes.Get("BarreEtat");
        MnuCalendrier.Header = Textes.Get("CorrCalendrier");
        MnuBarreOutils.Header = Textes.Get("CorrBarreOutils");
        MnuItemsIndisponibles.Header = Textes.Get("CorrItemsIndisponibles");
        MnuRaccourcisMenus.Header = Textes.Get("CorrRaccourcisMenus");
        MnuLectureRiche.Header = Textes.Get("CorrLectureRiche");
        MnuTouchesAcces.Header = Textes.Get("CorrTouchesAcces");
        MnuActiverTous.Header = Textes.Get("CorrActiverTous");
        MnuDesactiverTous.Header = Textes.Get("CorrDesactiverTous");
        MnuAide.Header = Textes.Get("Aide");
        MnuAPropos.Header = Textes.Get("APropos");

        menuEditeurs.AppliquerLangue();
        ContenuTextBox.AppliquerLangue();
        ContenuRichText.AppliquerLangue();
        ContenuUsuels.AppliquerLangue();
        ContenuChoix.AppliquerLangue();
        ContenuTreeView.AppliquerLangue();
        ContenuDataGrid.AppliquerLangue();
        ContenuAnnonce.AppliquerLangue();

        MajStatut(Keyboard.FocusedElement);
    }

    private void Langue_Click(object sender, RoutedEventArgs e)
    {
        // Choix exclusif : recliquer sur l'item coché ne doit pas le décocher.
        AppliquerLangue(sender == MnuEN ? "EN" : "FR");
    }

    // ===================== Barre d'état =====================

    private void MajStatut(IInputElement focus)
    {
        DefinirChampStatut(StatutType, "NomType", focus == null ? "" : focus.GetType().Name);
        MajPosition(focus);
    }

    // Le StatusBarItem n'est pas exposé en UI Automation : le nom accessible (« étiquette : valeur »)
    // est posé sur le TextBlock, dont le texte visible ne contient que la valeur.
    private static void DefinirChampStatut(TextBlock champ, string cleNom, string valeur)
    {
        champ.Text = valeur;
        string nom = Textes.Get(cleNom);
        AutomationProperties.SetName(champ, valeur.Length == 0 ? nom : nom + " : " + valeur);
    }

    // Chaque onglet donne le texte de sa position quand le focus est chez lui, et rien (nul) sinon.
    private void MajPosition(IInputElement focus)
    {
        string texte = ContenuTextBox.Position(focus) ?? ContenuRichText.Position(focus) ?? ContenuDataGrid.Position(focus) ?? "";
        DefinirChampStatut(StatutPosition, "NomPosition", texte);
    }
}
