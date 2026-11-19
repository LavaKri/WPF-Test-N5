using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CorrectifsWpf;

namespace AccessTest;

public partial class MainWindow
{
    // ===================== Correctifs =====================

    // Items « tous les correctifs » (Specs/Correctifs.md) : ceux du menu Correctif, puis les items des menus contextuels
    // des onglets (correctifs et réglages). L'item de lecture du texte enrichi du menu contextuel du RichTextBox est lié
    // à celui du menu principal : il n'est pas repris ici.
    private MenuItem[] itemsTous;

    private void BrancherItemsCorrectifs()
    {
        itemsTous = new[]
        {
            MnuLignesVides, MnuBarreEtat, MnuCalendrier, MnuBarreOutils, MnuItemsIndisponibles, MnuRaccourcisMenus,
            MnuLectureRiche, MnuTouchesAcces,
            ContenuUsuels.MnuLnkLienSeul, ContenuUsuels.MnuBtnRegion, ContenuUsuels.MnuBtnTabulable, ContenuUsuels.MnuResCurseur,
            ContenuChoix.MnuCouleurLimiter, ContenuChoix.MnuCalMasquerJours, ContenuChoix.MnuCurseurScrollbar,
            ContenuTreeView.MnuCasCorriger,
            ContenuDataGrid.MnuGrlUnArret, ContenuDataGrid.MnuGrlEntetes, ContenuDataGrid.MnuGrlNoms, ContenuDataGrid.MnuGrlUneLecture,
        };

        // Checked / Unchecked : le TogglePattern UIA change IsChecked sans Click, et un échec d'application décoche l'item.
        foreach (MenuItem item in itemsTous)
        {
            item.Checked += (s, e) => MajItemsTous();
            item.Unchecked += (s, e) => MajItemsTous();
        }
        MajItemsTous();
    }

    // Chaque item est grisé quand il n'a plus rien à faire : tout est déjà actif, ou tout est déjà désactivé.
    private void MajItemsTous()
    {
        MnuActiverTous.IsEnabled = itemsTous.Any(i => !i.IsChecked);
        MnuDesactiverTous.IsEnabled = itemsTous.Any(i => i.IsChecked);
    }

    // Chaque item agit par son propre gestionnaire (Checked / Unchecked) : un échec d'application affiche son message
    // et laisse l'item décoché.
    private void ActiverTous_Click(object sender, RoutedEventArgs e)
    {
        DefinirTous(true);
    }

    private void DesactiverTous_Click(object sender, RoutedEventArgs e)
    {
        DefinirTous(false);
    }

    private void DefinirTous(bool actif)
    {
        foreach (MenuItem item in itemsTous)
        {
            item.IsChecked = actif;
        }
    }

    // Checked/Unchecked plutôt que Click : le TogglePattern UIA du MenuItem change IsChecked sans Click,
    // et le démarrage coche l'item par le code.
    private void LignesVides_Change(object sender, RoutedEventArgs e)
    {
        if (!MnuLignesVides.IsChecked)
        {
            CorrectifLigneVide.Desactiver();
            return;
        }

        try
        {
            CorrectifLigneVide.Activer();
        }
        catch (Exception ex)
        {
            MnuLignesVides.IsChecked = false;
            AfficherErreur(string.Format(Textes.Get("CorrectifEchec"), ex.GetBaseException().Message));
        }
    }

    private void BarreEtat_Change(object sender, RoutedEventArgs e)
    {
        CorrectifBarreEtat.Actif = MnuBarreEtat.IsChecked;
    }

    private void Calendrier_Change(object sender, RoutedEventArgs e)
    {
        CorrectifCalendrier.Actif = MnuCalendrier.IsChecked;
    }

    private void BarreOutils_Change(object sender, RoutedEventArgs e)
    {
        CorrectifBarreOutils.Actif = MnuBarreOutils.IsChecked;
    }

    private void ItemsIndisponibles_Change(object sender, RoutedEventArgs e)
    {
        if (!MnuItemsIndisponibles.IsChecked)
        {
            CorrectifMenuIndisponible.Desactiver();
            return;
        }

        try
        {
            CorrectifMenuIndisponible.Activer();
        }
        catch (Exception ex)
        {
            MnuItemsIndisponibles.IsChecked = false;
            AfficherErreur(string.Format(Textes.Get("CorrectifEchec"), ex.GetBaseException().Message));
        }
    }

    private void LectureRiche_Change(object sender, RoutedEventArgs e)
    {
        if (!MnuLectureRiche.IsChecked)
        {
            CorrectifLectureRiche.Desactiver();
            return;
        }

        try
        {
            CorrectifLectureRiche.Activer();
        }
        catch (Exception ex)
        {
            MnuLectureRiche.IsChecked = false;
            AfficherErreur(string.Format(Textes.Get("CorrectifEchec"), ex.GetBaseException().Message));
        }
    }

    private void RaccourcisMenus_Change(object sender, RoutedEventArgs e)
    {
        if (!MnuRaccourcisMenus.IsChecked)
        {
            CorrectifRaccourciMenu.Desactiver();
            return;
        }

        try
        {
            CorrectifRaccourciMenu.Activer();
        }
        catch (Exception ex)
        {
            MnuRaccourcisMenus.IsChecked = false;
            AfficherErreur(string.Format(Textes.Get("CorrectifEchec"), ex.GetBaseException().Message));
        }
    }

    private void TouchesAcces_Change(object sender, RoutedEventArgs e)
    {
        if (!MnuTouchesAcces.IsChecked)
        {
            CorrectifToucheAcces.Desactiver();
            return;
        }

        try
        {
            CorrectifToucheAcces.Activer();
        }
        catch (Exception ex)
        {
            MnuTouchesAcces.IsChecked = false;
            AfficherErreur(string.Format(Textes.Get("CorrectifEchec"), ex.GetBaseException().Message));
        }
    }
}
