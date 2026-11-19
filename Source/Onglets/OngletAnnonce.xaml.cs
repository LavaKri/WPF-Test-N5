using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AccessTest;

// Contenu de l'onglet Annonce (Specs/Annonce.md).
public partial class OngletAnnonce : UserControl
{
    // Répétition toutes les 5 s, identifiant d'activité fixe des notifications (Specs/Annonce.md, 3).
    private const string ActiviteAnnonce = "AccessTest.Annonce";
    private readonly DispatcherTimer minuterieAnnonce = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };

    // RaiseNotificationEvent et ses deux énumérations sont dans le runtime de .NET Framework 4.8, pas dans ses assemblys
    // de référence : appel par réflexion, pour garder les mêmes sources (Specs/Annonce.md, 6). UIAutomationTypes est
    // l'assembly de ToggleState.
    private static readonly Type TypeNotification =
        typeof(ToggleState).Assembly.GetType("System.Windows.Automation.AutomationNotificationKind");
    private static readonly Type TypeTraitement =
        typeof(ToggleState).Assembly.GetType("System.Windows.Automation.AutomationNotificationProcessing");
    private static readonly MethodInfo LeverNotification = TypeNotification == null || TypeTraitement == null
        ? null
        : typeof(AutomationPeer).GetMethod("RaiseNotificationEvent",
                                           new[] { TypeNotification, TypeTraitement, typeof(string), typeof(string) });

    // Noms des valeurs des énumérations de WPF, non traduits (Specs/Annonce.md, 2). Le gestionnaire de la sortie est
    // branché ici : SelectedIndex="0" le déclencherait pendant InitializeComponent.
    public OngletAnnonce()
    {
        InitializeComponent();

        CmbType.ItemsSource = new[] { "ItemAdded", "ItemRemoved", "ActionCompleted", "ActionAborted", "Other" };
        CmbType.SelectedItem = "Other";
        CmbTraitement.ItemsSource = new[]
        {
            "ImportantAll", "ImportantMostRecent", "All", "MostRecent", "CurrentThenMostRecent"
        };
        CmbTraitement.SelectedItem = "ImportantAll";
        // Méthode introuvable (version de WPF qui ne l'a pas) : la sortie n'est pas proposée.
        ItmSortieNotification.IsEnabled = LeverNotification != null;
        CmbPolitesse.ItemsSource = new[] { AutomationLiveSetting.Polite, AutomationLiveSetting.Assertive };
        CmbPolitesse.SelectedItem = AutomationLiveSetting.Polite;

        // Une saisie : le texte initial n'est pas retraduit au changement de langue.
        TxtMessage.Text = Textes.Get("AnnMessageInitial");
        minuterieAnnonce.Tick += (s, e) => Annoncer(true);
        CmbSortie.SelectionChanged += Sortie_SelectionChanged;
        AppliquerSortie();
    }

    // Les listes, le bouton et le bouton à bascule tirent leur nom de leur libellé ou de leur texte visible.
    // TxtDerniere n'a pas de nom posé : la région vivante lit son texte.
    public void AppliquerLangue()
    {
        LblSortie.Text = Textes.Get("AnnSortie");
        LblType.Text = Textes.Get("AnnType");
        LblTraitement.Text = Textes.Get("AnnTraitement");
        LblPolitesse.Text = Textes.Get("AnnPolitesse");
        LblMessage.Text = Textes.Get("AnnMessage");
        LblDerniere.Text = Textes.Get("AnnDerniere");

        ItmSortieAucune.Content = Textes.Get("AnnAucune");
        ItmSortieNotification.Content = Textes.Get("AnnNotification");
        ItmSortieRegion.Content = Textes.Get("AnnRegion");
        ItmSortieFocus.Content = Textes.Get("AnnFocus");
        BtnAnnoncer.Content = Textes.Get("AnnAnnoncer");
        TglRepeter.Content = Textes.Get("AnnRepeter");
        BtnDialogue.Content = Textes.Get("AnnDialogue");

        AutomationProperties.SetName(CmbSortie, Textes.Get("AnnSortie"));
        AutomationProperties.SetName(CmbType, Textes.Get("AnnType"));
        AutomationProperties.SetName(CmbTraitement, Textes.Get("AnnTraitement"));
        AutomationProperties.SetName(CmbPolitesse, Textes.Get("AnnPolitesse"));
        AutomationProperties.SetName(TxtMessage, Textes.Get("AnnMessage"));
    }

    private void Sortie_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged remonte jusqu'au TabControl : on l'arrête ici.
        e.Handled = true;
        AppliquerSortie();
    }

    // Seules les listes de la sortie choisie sont actives. Le changement de sortie désactive vraiment la précédente :
    // TxtDerniere n'est plus une région vivante, et Aucune arrête la répétition (Specs/Annonce.md, 3.2).
    private void AppliquerSortie()
    {
        bool aucune = CmbSortie.SelectedItem == ItmSortieAucune;
        bool notification = CmbSortie.SelectedItem == ItmSortieNotification;
        bool region = CmbSortie.SelectedItem == ItmSortieRegion;

        CmbType.IsEnabled = notification;
        CmbTraitement.IsEnabled = notification;
        CmbPolitesse.IsEnabled = region;
        BtnAnnoncer.IsEnabled = !aucune;
        TglRepeter.IsEnabled = !aucune;
        if (aucune)
        {
            TglRepeter.IsChecked = false;
        }
        if (!region)
        {
            TxtDerniere.ClearValue(AutomationProperties.LiveSettingProperty);
        }
    }

    private void Annoncer_Click(object sender, RoutedEventArgs e)
    {
        Annoncer(false);
    }

    // Enfoncé : une annonce tout de suite, puis toutes les 5 s (Specs/Annonce.md, 3.4).
    private void Repeter_Checked(object sender, RoutedEventArgs e)
    {
        Annoncer(false);
        minuterieAnnonce.Start();
    }

    private void Repeter_Unchecked(object sender, RoutedEventArgs e)
    {
        minuterieAnnonce.Stop();
    }

    // Boîte modale standard : la répétition continue pendant sa boucle modale, rien n'est adapté (Specs/Annonce.md, 3.5).
    private void Dialogue_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(Window.GetWindow(this), Textes.Get("AnnDialogueTexte"), Textes.Get("AnnDialogueTitre"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // Sortie, message et paramètres sont lus à chaque annonce. repetition : la sortie Focus n'agit que dans l'onglet
    // Annonce, pour ne pas voler le focus aux autres onglets (le contenu d'un onglet non affiché n'est pas visible).
    private void Annoncer(bool repetition)
    {
        string message = TxtMessage.Text;

        if (CmbSortie.SelectedItem == ItmSortieNotification)
        {
            TxtDerniere.Text = message;
            // Sur le peer de la fenêtre : elle reste dans l'arbre UIA quel que soit l'onglet affiché.
            AutomationPeer fenetre = UIElementAutomationPeer.CreatePeerForElement(Window.GetWindow(this));
            if (fenetre != null && LeverNotification != null)
            {
                LeverNotification.Invoke(fenetre, new[]
                {
                    Enum.Parse(TypeNotification, (string)CmbType.SelectedItem),
                    Enum.Parse(TypeTraitement, (string)CmbTraitement.SelectedItem),
                    message, ActiviteAnnonce
                });
            }
        }
        else if (CmbSortie.SelectedItem == ItmSortieRegion)
        {
            TxtDerniere.Text = message;
            AutomationProperties.SetLiveSetting(TxtDerniere, (AutomationLiveSetting)CmbPolitesse.SelectedItem);
            // Un nom vide n'a rien à lire. Le même texte est renvoyé tel quel : on observe s'il est relu.
            if (message.Length > 0)
            {
                AutomationPeer region = UIElementAutomationPeer.CreatePeerForElement(TxtDerniere);
                region?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
            }
        }
        else if (CmbSortie.SelectedItem == ItmSortieFocus)
        {
            if (repetition && !IsVisible)
            {
                return;
            }
            TxtDerniere.Text = message;
            TxtMessage.Focus();
            TxtMessage.CaretIndex = message.Length;
        }
    }
}
