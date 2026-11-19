using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace AccessTest;

public partial class MainWindow
{
    private void Quitter_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // Dossier de départ des boîtes Ouvrir, Enregistrer et Choix d'un dossier : Docs au démarrage, puis le dossier
    // choisi (Specs/Fenetre.md, 2.1.3). Nul : Windows choisit.
    private string dossierDepart;

    // OpenFolderDialog n'existe qu'à partir de .NET 8 : cherchée par réflexion, nulle en .NET Framework 4.8.
    private static readonly Type TypeDialogueDossier =
        typeof(Microsoft.Win32.OpenFileDialog).Assembly.GetType("Microsoft.Win32.OpenFolderDialog");

    // Ouvrir et Enregistrer ne servent que dans les onglets TextBox et RichText, même si le focus est sur l'en-tête.
    private void Fichier_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = TabPrincipal.SelectedItem == TabTextBox || TabPrincipal.SelectedItem == TabRichText;
    }

    private void Ouvrir_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var dialogue = new Microsoft.Win32.OpenFileDialog
        {
            Filter = Textes.Get("FiltreOuvrir"),
            InitialDirectory = dossierDepart ?? "",
        };
        if (dialogue.ShowDialog(this) == true)
        {
            ChargerChemin(dialogue.FileName, focaliser: true);
        }
    }

    private void Enregistrer_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var dialogue = new Microsoft.Win32.SaveFileDialog
        {
            Filter = Textes.Get("FiltreEnregistrer"),
            DefaultExt = ".txt",
            InitialDirectory = dossierDepart ?? "",
        };
        if (dialogue.ShowDialog(this) != true)
        {
            return;
        }

        Control editeur;
        string texte;
        if (TabPrincipal.SelectedItem == TabRichText)
        {
            editeur = ContenuRichText.Editeur;
            texte = ContenuRichText.LireTexte();
        }
        else
        {
            editeur = ContenuTextBox.Editeur;
            texte = ContenuTextBox.LireTexte();
        }

        try
        {
            File.WriteAllText(dialogue.FileName, texte, new UTF8Encoding(false));
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            AfficherErreur(string.Format(Textes.Get("NonEnregistre"), dialogue.FileName, ex.Message));
            return;
        }
        editeur.Focus();
    }

    // Appel par réflexion : mêmes sources pour net48, où l'item est désactivé (Specs/Fenetre.md, 2.1.3).
    private void ChoixDossier_Click(object sender, RoutedEventArgs e)
    {
        if (TypeDialogueDossier == null)
        {
            return;
        }
        object dialogue = Activator.CreateInstance(TypeDialogueDossier);
        TypeDialogueDossier.GetProperty("InitialDirectory").SetValue(dialogue, dossierDepart ?? "");
        var resultat = (bool?)TypeDialogueDossier.GetMethod("ShowDialog", new[] { typeof(Window) })
                                                 .Invoke(dialogue, new object[] { this });
        if (resultat != true)
        {
            return;
        }

        dossierDepart = (string)TypeDialogueDossier.GetProperty("FolderName").GetValue(dialogue);
        MessageBox.Show(this, string.Format(Textes.Get("TexteDossier"), dossierDepart),
            Textes.Get("TitreDossier"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // Cherche le dossier à côté de l'exécutable puis dans les dossiers parents.
    private static string TrouverDossier(string nom)
    {
        DirectoryInfo dossier = new DirectoryInfo(AppContext.BaseDirectory);
        while (dossier != null)
        {
            string chemin = Path.Combine(dossier.FullName, nom);
            if (Directory.Exists(chemin))
            {
                return chemin;
            }
            dossier = dossier.Parent;
        }
        return null;
    }

    // Charge un fichier dans la zone de texte de l'onglet actif (Fichier > Ouvrir).
    private void ChargerChemin(string chemin, bool focaliser)
    {
        string texte;
        try
        {
            texte = File.ReadAllText(chemin, Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            AfficherErreur(string.Format(Textes.Get("Illisible"), chemin, ex.Message));
            return;
        }

        if (TabPrincipal.SelectedItem == TabRichText)
        {
            ContenuRichText.Charger(texte, focaliser);
        }
        else
        {
            ContenuTextBox.Charger(texte, focaliser);
        }
    }

    private void AfficherErreur(string message)
    {
        MessageBox.Show(this, message, Textes.Get("TitreErreur"), MessageBoxButton.OK, MessageBoxImage.Error);
    }

    // Item de vérification, toujours indisponible : ce message ne doit jamais paraître (Specs/Fenetre.md, 2.1).
    private void ToujoursGrisee_Click(object sender, RoutedEventArgs e)
    {
        AfficherErreur(Textes.Get("ToujoursGriseeReagi"));
    }

    // ===================== Menu Aide =====================

    private void APropos_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this, string.Format(Textes.Get("TexteAPropos"), Textes.NomApplication, Textes.VersionApplication, Framework),
            Textes.Get("TitreAPropos"), MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
