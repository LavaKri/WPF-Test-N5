using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Threading;
using CorrectifsWpf;

namespace AccessTest;

// Contenu de l'onglet Usuels (Specs/Usuels.md).
public partial class OngletUsuels : UserControl
{
    // La progression dure 5 s (Specs/Usuels.md, 3.3).
    private const int DureeProgressionMs = 5000;
    private readonly DispatcherTimer minuterieProgression = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
    private readonly Stopwatch chronoProgression = new Stopwatch();

    // Région vivante du pourcentage (Specs/Usuels.md, 3.10) : une annonce au plus toutes les 500 ms.
    private const int IntervalleAnnonceMs = 500;
    private long derniereAnnonceMs = -IntervalleAnnonceMs;

    public OngletUsuels()
    {
        InitializeComponent();

        // Branché ici et non dans le XAML : SelectedIndex="0" déclencherait le gestionnaire pendant
        // InitializeComponent, avant que TxtContact n'existe.
        CmbConfirmation.SelectionChanged += Confirmation_SelectionChanged;
        minuterieProgression.Tick += Progression_Tick;
        // Coché dans le XAML : gestionnaires branchés ici pour ne pas se déclencher pendant InitializeComponent.
        MnuResCurseur.Checked += (s, e) => AppliquerCurseurResultat();
        MnuResCurseur.Unchecked += (s, e) => AppliquerCurseurResultat();
        AppliquerCurseurResultat();
        // Correctif des liens lus deux fois (Specs/Usuels.md, 3.9) : global, appliqué dès le démarrage.
        MnuLnkLienSeul.Checked += (s, e) => AppliquerLienSeul();
        MnuLnkLienSeul.Unchecked += (s, e) => AppliquerLienSeul();
        AppliquerLienSeul();
        // Coché au démarrage : TblPourcentage est une région vivante (Specs/Usuels.md, 3.10).
        MnuBtnRegion.Checked += (s, e) => AppliquerRegionPourcentage();
        MnuBtnRegion.Unchecked += (s, e) => AppliquerRegionPourcentage();
        AppliquerRegionPourcentage();
        // Décoché au démarrage : la barre n'est pas focalisable, comme en WPF natif (Specs/Usuels.md, 3.10).
        MnuBtnTabulable.Checked += (s, e) => AppliquerTabulationBarre();
        MnuBtnTabulable.Unchecked += (s, e) => AppliquerTabulationBarre();
    }

    // Chaque contrôle reçoit son nom accessible : un Label avec Target ne le donne pas.
    // Le CheckBox et le Button tirent leur nom de leur texte visible.
    public void AppliquerLangue()
    {
        LblConfirmation.Text = Textes.Get("UsuConfirmation");
        LblContact.Text = Textes.Get("UsuContact");
        // Label : le « _ » du texte est la touche d'accès ; le nom accessible du champ, posé plus bas, n'en a pas.
        LblIdentifiant.Content = Textes.Get("UsuIdentifiantTouche");
        LblMotDePasse.Content = Textes.Get("UsuMotDePasseTouche");
        RunMotDePasseOublie.Text = Textes.Get("UsuLien");
        LblProgression.Text = Textes.Get("UsuProgression");
        LblResultat.Text = Textes.Get("UsuResultat");

        ItmAucun.Content = Textes.Get("UsuAucun");
        ItmSms.Content = Textes.Get("UsuSms");
        ItmEmail.Content = Textes.Get("UsuEmail");
        ChkResterConnecte.Content = Textes.Get("UsuResterConnecte");
        BtnSeConnecter.Content = Textes.Get("UsuSeConnecter");

        AutomationProperties.SetName(CmbConfirmation, Textes.Get("UsuConfirmation"));
        AutomationProperties.SetName(TxtContact, Textes.Get("UsuContact"));
        AutomationProperties.SetName(TxtIdentifiant, Textes.Get("UsuIdentifiant"));
        AutomationProperties.SetName(PwdMotDePasse, Textes.Get("UsuMotDePasse"));
        AutomationProperties.SetName(PrgProgression, Textes.Get("UsuProgression"));
        AfficherPourcentage();
        AutomationProperties.SetName(TxtResultat, Textes.Get("UsuResultat"));
        MnuResCurseur.Header = Textes.Get("UsuResCurseur");
        MnuLnkLienSeul.Header = Textes.Get("UsuLienSeul");
        MnuBtnRegion.Header = Textes.Get("UsuBtnRegion");
        MnuBtnTabulable.Header = Textes.Get("UsuBtnTabulable");
    }

    // « SMS » ou « Email » active la ligne du contact ; « Aucun » la désactive (le texte saisi est conservé).
    private void Confirmation_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged remonte jusqu'au TabControl : on l'arrête ici.
        e.Handled = true;
        TxtContact.IsEnabled = CmbConfirmation.SelectedIndex > 0;
    }

    // Pas de validation. Le focus passe au résultat AVANT la désactivation du bouton : désactiver un contrôle
    // qui a le focus le lui ferait perdre, et le lecteur d'écran ne saurait plus où il est (Specs/Usuels.md, 3.3).
    private void SeConnecter_Click(object sender, RoutedEventArgs e)
    {
        TxtResultat.Text = Textes.Get("UsuEnCours");
        TxtResultat.Focus();
        BtnSeConnecter.IsEnabled = false;
        PrgProgression.Value = 0;
        chronoProgression.Restart();
        derniereAnnonceMs = -IntervalleAnnonceMs;
        AfficherPourcentage();
        minuterieProgression.Start();
    }

    // Le lien n'a pas de NavigateUri : il ne navigue nulle part. Le focus passe au résultat pour que le message soit lu
    // (Specs/Usuels.md, 3.8).
    private void MotDePasseOublie_Click(object sender, RoutedEventArgs e)
    {
        TxtResultat.Text = Textes.Get("UsuLienActive");
        TxtResultat.Focus();
    }

    private void Progression_Tick(object sender, EventArgs e)
    {
        int pourcent = (int)Math.Min(100, chronoProgression.ElapsedMilliseconds * 100 / DureeProgressionMs);
        PrgProgression.Value = pourcent;
        AfficherPourcentage();
        if (pourcent >= 100)
        {
            minuterieProgression.Stop();
            BtnSeConnecter.IsEnabled = true;
            TxtResultat.Text = ComposerResultat();
            TxtResultat.CaretIndex = 0;
        }
    }

    // Par défaut, texte seul : ni région vivante ni notification UIA, rien n'est annoncé à chaque pas (Specs/Usuels.md, 3.3).
    // Si MnuBtnRegion est coché, TblPourcentage est une région vivante : LiveRegionChanged est levé au plus toutes les
    // 500 ms, et à 100 % (comme la sortie « Région vivante » de l'onglet Annonce), le lecteur lit le nom du TextBlock,
    // c'est-à-dire son texte. Le texte, lui, suit chaque pas.
    private void AfficherPourcentage()
    {
        string texte = string.Format(Textes.Get("UsuPourcentage"), (int)PrgProgression.Value);
        if (texte == TblPourcentage.Text)
        {
            return;
        }

        TblPourcentage.Text = texte;
        long ecoule = chronoProgression.ElapsedMilliseconds;
        if (MnuBtnRegion.IsChecked && (PrgProgression.Value >= 100 || ecoule - derniereAnnonceMs >= IntervalleAnnonceMs))
        {
            derniereAnnonceMs = ecoule;
            UIElementAutomationPeer.CreatePeerForElement(TblPourcentage)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
    }

    // Coché : TblPourcentage devient une région vivante assertive, une annonce coupe la précédente ; décoché : texte seul
    // (Specs/Usuels.md, 3.10).
    private void AppliquerRegionPourcentage()
    {
        if (MnuBtnRegion.IsChecked)
        {
            AutomationProperties.SetLiveSetting(TblPourcentage, AutomationLiveSetting.Assertive);
        }
        else
        {
            TblPourcentage.ClearValue(AutomationProperties.LiveSettingProperty);
        }
    }

    // Coché : la barre devient un arrêt de tabulation (focalisable) ; décoché : valeur native de WPF, non focalisable.
    private void AppliquerTabulationBarre()
    {
        if (MnuBtnTabulable.IsChecked)
        {
            PrgProgression.Focusable = true;
        }
        else
        {
            PrgProgression.ClearValue(FocusableProperty);
        }
    }

    // Coché : le point d'insertion du champ en lecture seule est visible (Specs/Usuels.md, 3.6).
    private void AppliquerCurseurResultat()
    {
        TxtResultat.IsReadOnlyCaretVisible = MnuResCurseur.IsChecked;
    }

    // En cas d'échec (code de WPF différent), message et case décochée, comme les correctifs du menu principal.
    private void AppliquerLienSeul()
    {
        if (!MnuLnkLienSeul.IsChecked)
        {
            CorrectifLienSeul.Desactiver();
            return;
        }

        try
        {
            CorrectifLienSeul.Activer();
        }
        catch (Exception ex)
        {
            MnuLnkLienSeul.IsChecked = false;
            MessageBox.Show(string.Format(Textes.Get("CorrectifEchec"), ex.GetBaseException().Message),
                            Textes.Get("TitreErreur"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Un champ par ligne, « Nom : valeur », dans l'ordre du formulaire (Specs/Usuels.md, 3.4).
    private string ComposerResultat()
    {
        // Le contact est repris tel quel, même si sa ligne est désactivée ; le mot de passe est en clair (appli d'essai).
        string[][] champs =
        {
            new[] { "UsuConfirmation", (CmbConfirmation.SelectedItem as ComboBoxItem)?.Content as string ?? "" },
            new[] { "UsuContact", TxtContact.Text },
            new[] { "UsuIdentifiant", TxtIdentifiant.Text },
            new[] { "UsuMotDePasse", PwdMotDePasse.Password },
            new[] { "UsuResterConnecte", Textes.Get(ChkResterConnecte.IsChecked == true ? "UsuOui" : "UsuNon") },
        };
        return string.Join(Environment.NewLine, champs.Select(c => Textes.Get(c[0]) + " : " + c[1]));
    }
}
