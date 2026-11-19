using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace AccessTest;

/// <summary>
/// Nœud de la source de données des deux arbres de l'onglet TreeView (voir Specs/TreeView.md, section 2).
/// Son ToString() renvoie le nom : c'est lui qui donne le nom accessible du TreeViewItem.
/// </summary>
public class Noeud : INotifyPropertyChanged
{
    private readonly string cle;
    private readonly int numero;
    private bool estDeveloppe;
    private bool estSelectionne;
    private bool? estCoche = false;
    private Noeud parent;

    /// <param name="cle">Clé du texte dans Textes.cs.</param>
    /// <param name="numero">Si positif, injecté dans le texte ({0}) : « Nouveau nœud 3 ».</param>
    public Noeud(string cle, int numero = 0)
    {
        this.cle = cle;
        this.numero = numero;
        Enfants.CollectionChanged += Enfants_CollectionChanged;
    }

    /// <summary>
    /// Vrai : cocher un nœud coche ses descendants, et chaque parent prend l'état de ses enfants (indéterminé s'ils
    /// diffèrent). Statique : seul le second arbre utilise EstCoche (Specs/TreeView.md, 5.1).
    /// </summary>
    public static bool Propager { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    public string Nom
    {
        get
        {
            string texte = Textes.Get(cle);
            return numero > 0 ? string.Format(texte, numero) : texte;
        }
    }

    public ObservableCollection<Noeud> Enfants { get; } = new ObservableCollection<Noeud>();

    public bool EstDeveloppe
    {
        get { return estDeveloppe; }
        set { Definir(ref estDeveloppe, value, nameof(EstDeveloppe)); }
    }

    public bool EstSelectionne
    {
        get { return estSelectionne; }
        set { Definir(ref estSelectionne, value, nameof(EstSelectionne)); }
    }

    public bool? EstCoche
    {
        get { return estCoche; }
        set
        {
            if (estCoche == value)
            {
                return;
            }
            DefinirCoche(value);
            if (Propager)
            {
                if (value.HasValue)
                {
                    foreach (Noeud enfant in Enfants)
                    {
                        enfant.CocherDescendants(value.Value);
                    }
                }
                parent?.RecalculerCoche();
            }
        }
    }

    public override string ToString()
    {
        return Nom;
    }

    // Change l'état sans propager (la propagation passe par les méthodes ci-dessous).
    private void DefinirCoche(bool? valeur)
    {
        estCoche = valeur;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EstCoche)));
    }

    private void CocherDescendants(bool valeur)
    {
        if (estCoche != valeur)
        {
            DefinirCoche(valeur);
        }
        foreach (Noeud enfant in Enfants)
        {
            enfant.CocherDescendants(valeur);
        }
    }

    // Coché si tous les enfants le sont, non coché si aucun, indéterminé sinon ; puis le parent, jusqu'à la racine.
    private void RecalculerCoche()
    {
        bool? etat = EtatDesEnfants();
        if (etat != estCoche)
        {
            DefinirCoche(etat);
        }
        parent?.RecalculerCoche();
    }

    private bool? EtatDesEnfants()
    {
        if (Enfants.Count == 0)
        {
            return estCoche;
        }
        bool? premier = Enfants[0].estCoche;
        foreach (Noeud enfant in Enfants)
        {
            if (enfant.estCoche != premier)
            {
                return null;
            }
        }
        return premier;
    }

    private void Enfants_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (Noeud enfant in e.NewItems)
            {
                enfant.parent = this;
            }
        }
        if (e.OldItems != null)
        {
            foreach (Noeud enfant in e.OldItems)
            {
                enfant.parent = null;
            }
        }
        if (Propager && Enfants.Count > 0)
        {
            RecalculerCoche();
        }
    }

    /// <summary>Recalcule l'état des parents à partir des feuilles, quand la propagation est activée.</summary>
    public static void RecalculerParents(IEnumerable<Noeud> noeuds)
    {
        foreach (Noeud noeud in noeuds)
        {
            RecalculerParents(noeud.Enfants);
            if (noeud.Enfants.Count > 0)
            {
                bool? etat = noeud.EtatDesEnfants();
                if (etat != noeud.estCoche)
                {
                    noeud.DefinirCoche(etat);
                }
            }
        }
    }

    private void Definir(ref bool champ, bool valeur, string propriete)
    {
        if (champ != valeur)
        {
            champ = valeur;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propriete));
        }
    }

    /// <summary>Relit les noms après un changement de langue (l'état développé, sélectionné ou coché est conservé).</summary>
    public static void RafraichirNoms(IEnumerable<Noeud> noeuds)
    {
        foreach (Noeud noeud in noeuds)
        {
            noeud.PropertyChanged?.Invoke(noeud, new PropertyChangedEventArgs(nameof(Nom)));
            RafraichirNoms(noeud.Enfants);
        }
    }

    /// <summary>
    /// Trois branches : Documents (trois enfants), Corbeille (aucun enfant), Projets (un seul enfant à chaque
    /// niveau, six niveaux). Tous réduits, aucun sélectionné ni coché.
    /// </summary>
    public static ObservableCollection<Noeud> CreerSource()
    {
        Noeud archives = new Noeud("TvArchives");
        Noeud annexes = new Noeud("TvAnnexes") { Enfants = { archives } };
        Noeud specs = new Noeud("TvSpecs") { Enfants = { annexes } };
        Noeud net10 = new Noeud("TvNet10") { Enfants = { specs } };
        Noeud accessTest = new Noeud("TvAccessTest") { Enfants = { net10 } };

        return new ObservableCollection<Noeud>
        {
            new Noeud("TvDocuments")
            {
                Enfants = { new Noeud("TvRapports"), new Noeud("TvFactures"), new Noeud("TvCourriers") }
            },
            new Noeud("TvCorbeille"),
            new Noeud("TvProjets") { Enfants = { accessTest } }
        };
    }
}
