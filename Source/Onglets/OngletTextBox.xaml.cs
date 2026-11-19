using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace AccessTest;

// Contenu de l'onglet TextBox (Specs/TextBox.md). Son menu contextuel est celui, partagé avec le RichTextBox, de MenuEditeurs.
public partial class OngletTextBox : UserControl
{
    // Levé quand le point d'insertion bouge ou que l'affichage change : la barre d'état relit la position.
    public event EventHandler PositionModifiee;

    public OngletTextBox()
    {
        InitializeComponent();

        TxtEditeur.SelectionChanged += (s, e) => PositionModifiee?.Invoke(this, EventArgs.Empty);
    }

    public TextBox Editeur => TxtEditeur;

    public void AppliquerLangue()
    {
        AutomationProperties.SetName(TxtEditeur, Textes.Get("NomTexte"));
    }

    // Lisez-moi.txt (racine du dépôt) est intégré à l'exécutable (ressource incorporée, AccessTest.csproj) : l'application
    // n'a besoin d'aucun fichier à côté d'elle (Specs/Fenetre.md, 2.5). Faux si la ressource est introuvable.
    public bool ChargerLisezMoi()
    {
        using (Stream flux = typeof(OngletTextBox).Assembly.GetManifestResourceStream("Lisez-moi.txt"))
        {
            if (flux == null)
            {
                return false;
            }
            using (StreamReader lecteur = new StreamReader(flux, Encoding.UTF8))
            {
                TxtEditeur.Text = lecteur.ReadToEnd();
            }
        }
        TxtEditeur.CaretIndex = 0;
        return true;
    }

    // Fichier > Ouvrir et Enregistrer (Specs/Fenetre.md, 2.1).
    public void Charger(string texte, bool focaliser)
    {
        TxtEditeur.Text = texte;
        TxtEditeur.CaretIndex = 0;
        if (focaliser)
        {
            TxtEditeur.Focus();
        }
    }

    public string LireTexte()
    {
        return TxtEditeur.Text;
    }

    // Item « Retour à la ligne automatique » (Specs/Fenetre.md, 2.7.3). Coché : retour à la ligne automatique. Décoché :
    // une ligne du texte par ligne affichée, défilement horizontal. Le RichTextBox garde son retour natif, qu'aucun
    // réglage ne coupe.
    public void DefinirRetourLigne(bool retour)
    {
        if (retour)
        {
            TxtEditeur.TextWrapping = TextWrapping.Wrap;
            TxtEditeur.ClearValue(TextBoxBase.HorizontalScrollBarVisibilityProperty);
        }
        else
        {
            TxtEditeur.TextWrapping = TextWrapping.NoWrap;
            TxtEditeur.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        }
        PositionModifiee?.Invoke(this, EventArgs.Empty);
    }

    // Texte de la barre d'état (ligne et colonne) quand le focus est dans le TextBox ; nul s'il est ailleurs.
    public string Position(IInputElement focus)
    {
        if (!ReferenceEquals(focus, TxtEditeur))
        {
            return null;
        }
        int index = TxtEditeur.CaretIndex;
        int ligneIndex = TxtEditeur.GetLineIndexFromCharacterIndex(index);
        if (ligneIndex < 0)
        {
            ligneIndex = 0;
        }
        int debutLigne = TxtEditeur.GetCharacterIndexFromLineIndex(ligneIndex);
        int colonne = index - Math.Max(debutLigne, 0) + 1;
        return string.Format(Textes.Get("Position"), ligneIndex + 1, colonne);
    }
}
