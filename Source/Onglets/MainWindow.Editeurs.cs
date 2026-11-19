using System.Windows.Input;

namespace AccessTest;

public partial class MainWindow
{
    // Menu contextuel partagé par les zones de texte des onglets TextBox et RichText (Specs/Fenetre.md, 2.7) :
    // posé sur les deux par le constructeur.
    private readonly MenuEditeurs menuEditeurs = new MenuEditeurs();

    // F7 : faute suivante, seulement si le correcteur est actif et dans les onglets TextBox et RichText (Specs/Fenetre.md, 2.7.2).
    private void Fenetre_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F7 && Keyboard.Modifiers == ModifierKeys.None && menuEditeurs.OrthographeActive
            && (TabPrincipal.SelectedItem == TabTextBox || TabPrincipal.SelectedItem == TabRichText))
        {
            e.Handled = true;
            menuEditeurs.FauteSuivante(TabPrincipal.SelectedItem == TabRichText);
        }
    }
}
