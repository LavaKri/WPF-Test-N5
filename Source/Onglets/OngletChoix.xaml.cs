using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CorrectifsWpf;

namespace AccessTest;

// Contenu de l'onglet Choix (Specs/Choix.md).
public partial class OngletChoix : UserControl
{
    public OngletChoix()
    {
        InitializeComponent();

        // Menus contextuels : les gestionnaires sont branchés ici, car IsChecked="True" dans le XAML
        // déclencherait Checked pendant InitializeComponent. Le masquage des jours (décoché au démarrage) est posé
        // au premier affichage du calendrier.
        MnuCurseurScrollbar.Checked += (s, e) => AppliquerVisibiliteScrollbar();
        MnuCurseurScrollbar.Unchecked += (s, e) => AppliquerVisibiliteScrollbar();
        AppliquerVisibiliteScrollbar();
        MnuCouleurLimiter.Checked += (s, e) => AppliquerLimiteCouleur();
        MnuCouleurLimiter.Unchecked += (s, e) => AppliquerLimiteCouleur();
        AppliquerLimiteCouleur();
        MnuCalMasquerJours.Checked += (s, e) => AppliquerMasqueJours();
        MnuCalMasquerJours.Unchecked += (s, e) => AppliquerMasqueJours();
        CalCalendrier.Loaded += (s, e) => AppliquerMasqueJours();
        CalCalendrier.DisplayDateChanged += (s, e) => ReappliquerMasqueJours();
        CalCalendrier.DisplayModeChanged += (s, e) => ReappliquerMasqueJours();
        // Piège Maj+Tab du calendrier : correctif actif selon le menu Correctif (Specs/Correctifs.md).
        CorrectifCalendrier.Brancher(CalCalendrier);

        DatDate.SelectedDate = DateTime.Today;
    }

    // Le GroupBox, les RadioButton, la CheckBox et le ToggleButton tirent leur nom de leur texte visible
    // (celui du groupe est son en-tête). Les autres contrôles reçoivent un nom accessible.
    public void AppliquerLangue()
    {
        GrpCouleur.Header = Textes.Get("ChxCouleur");
        RdoRouge.Content = Textes.Get("ChxRouge");
        RdoVert.Content = Textes.Get("ChxVert");
        RdoBleu.Content = Textes.Get("ChxBleu");
        MnuCouleurLimiter.Header = Textes.Get("ChxCouleurLimiter");

        LblQcm.Text = Textes.Get("ChxQcm");
        ItmNord.Content = Textes.Get("ChxNord");
        ItmSud.Content = Textes.Get("ChxSud");
        ItmEst.Content = Textes.Get("ChxEst");
        ItmOuest.Content = Textes.Get("ChxOuest");

        ChkTroisEtats.Content = Textes.Get("ChxTroisEtats");
        TglBascule.Content = Textes.Get("ChxBascule");
        MnuCalDemain.Header = Textes.Get("ChxCalDemain");
        MnuCalMasquerJours.Header = Textes.Get("ChxCalMasquer");

        LblCalendrier.Text = Textes.Get("ChxCalendrier");
        LblDate.Text = Textes.Get("ChxDate");
        LblCurseur.Text = Textes.Get("ChxCurseur");
        MnuCurseurScrollbar.Header = Textes.Get("ChxCurseurScrollbar");
        LblDefilement.Text = Textes.Get("ChxDefilement");

        AutomationProperties.SetName(LstQcm, Textes.Get("ChxQcm"));
        AutomationProperties.SetName(CalCalendrier, Textes.Get("ChxCalendrier"));
        AutomationProperties.SetName(DatDate, Textes.Get("ChxDate"));
        AutomationProperties.SetName(SldCurseur, Textes.Get("ChxCurseur"));
        AutomationProperties.SetName(ScbDefilement, Textes.Get("ChxDefilement"));
        AutomationProperties.SetHelpText(DatDate, Textes.Get("ChxAideDate"));

        // Le texte du DatePicker est mis en forme avec la langue en vigueur quand la date est posée :
        // on la repose pour qu'il suive la langue de l'application.
        DateTime? date = DatDate.SelectedDate;
        DatDate.SelectedDate = null;
        DatDate.SelectedDate = date;
    }

    // Coché : un seul arrêt de tabulation (Once) et flèches contenues dans le groupe. Décoché : valeurs par défaut de WPF.
    // Limite connue : avec Once, le premier arrêt est le premier bouton, pas le bouton coché (Specs/Choix.md, 3).
    private void AppliquerLimiteCouleur()
    {
        if (MnuCouleurLimiter.IsChecked)
        {
            KeyboardNavigation.SetTabNavigation(PnlCouleur, KeyboardNavigationMode.Once);
            KeyboardNavigation.SetDirectionalNavigation(PnlCouleur, KeyboardNavigationMode.Contained);
        }
        else
        {
            PnlCouleur.ClearValue(KeyboardNavigation.TabNavigationProperty);
            PnlCouleur.ClearValue(KeyboardNavigation.DirectionalNavigationProperty);
        }
    }

    // Décoché, la barre de défilement et son libellé sont masqués (la synchronisation avec le curseur continue).
    private void AppliquerVisibiliteScrollbar()
    {
        Visibility visibilite = MnuCurseurScrollbar.IsChecked ? Visibility.Visible : Visibility.Collapsed;
        LblDefilement.Visibility = visibilite;
        ScbDefilement.Visibility = visibilite;
    }

    private void CalDemain_Click(object sender, RoutedEventArgs e)
    {
        DateTime demain = DateTime.Today.AddDays(1);
        CalCalendrier.SelectedDate = demain;
        CalCalendrier.DisplayDate = demain;
    }

    // Coché, l'item masque les jours (CalendarDayButton) sans toucher au modèle du Calendar : on les retrouve
    // dans l'arbre visuel. Hidden garde la taille du calendrier.
    private void AppliquerMasqueJours()
    {
        Visibility visibilite = MnuCalMasquerJours.IsChecked ? Visibility.Hidden : Visibility.Visible;
        foreach (CalendarDayButton jour in TrouverEnfants<CalendarDayButton>(CalCalendrier))
        {
            jour.Visibility = visibilite;
        }
    }

    // Le calendrier met à jour ses jours quand le mois ou la vue change : on réapplique une fois la mise en page faite.
    private void ReappliquerMasqueJours()
    {
        Dispatcher.BeginInvoke(new Action(AppliquerMasqueJours), DispatcherPriority.Loaded);
    }

    private static IEnumerable<T> TrouverEnfants<T>(DependencyObject parent) where T : DependencyObject
    {
        int nombre = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < nombre; i++)
        {
            DependencyObject enfant = VisualTreeHelper.GetChild(parent, i);
            if (enfant is T trouve)
            {
                yield return trouve;
            }
            foreach (T descendant in TrouverEnfants<T>(enfant))
            {
                yield return descendant;
            }
        }
    }
}
