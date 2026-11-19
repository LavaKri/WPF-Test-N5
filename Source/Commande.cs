using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace AccessTest;

public enum StatutCommande
{
    EnAttente,
    Expediee,
    Livree
}

/// <summary>
/// Ligne de la grille de l'onglet DataGrid (voir Specs/DataGrid.md, section 2).
/// Son ToString() donne le nom accessible de la ligne : « Commande 1001 ».
/// </summary>
public class Commande : INotifyPropertyChanged
{
    private int numero;
    private string client;
    private DateTime date;
    private decimal montant;
    private bool payee;
    private StatutCommande statut;
    private bool prioritaire;

    public event PropertyChangedEventHandler PropertyChanged;

    // Constructeur sans paramètre : exigé par la ligne vide d'ajout du DataGrid (CanUserAddRows).
    public Commande()
    {
    }

    public int Numero
    {
        get { return numero; }
        set { Definir(ref numero, value, nameof(Numero)); }
    }

    public string Client
    {
        get { return client; }
        set { Definir(ref client, value, nameof(Client)); }
    }

    public DateTime Date
    {
        get { return date; }
        set { Definir(ref date, value, nameof(Date)); }
    }

    public decimal Montant
    {
        get { return montant; }
        set { Definir(ref montant, value, nameof(Montant)); }
    }

    public bool Payee
    {
        get { return payee; }
        set { Definir(ref payee, value, nameof(Payee)); }
    }

    public StatutCommande Statut
    {
        get { return statut; }
        set { Definir(ref statut, value, nameof(Statut)); }
    }

    public bool Prioritaire
    {
        get { return prioritaire; }
        set { Definir(ref prioritaire, value, nameof(Prioritaire)); }
    }

    public override string ToString()
    {
        return string.Format(Textes.Get("DgLigne"), Numero);
    }

    private void Definir<T>(ref T champ, T valeur, string propriete)
    {
        if (!EqualityComparer<T>.Default.Equals(champ, valeur))
        {
            champ = valeur;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propriete));
        }
    }

    /// <summary>
    /// 30 commandes, toujours les mêmes (règle fixe, pas de hasard) : dix clients qui reviennent, dates
    /// d'octobre à novembre 2026, montants variés, une commande sur deux environ payée, statuts mélangés,
    /// quelques prioritaires.
    /// </summary>
    public static ObservableCollection<Commande> CreerSource()
    {
        string[] clients = { "Martin", "Bernard", "Dubois", "Thomas", "Robert", "Richard", "Petit", "Durand", "Leroy", "Moreau" };
        ObservableCollection<Commande> commandes = new ObservableCollection<Commande>();
        for (int i = 0; i < 30; i++)
        {
            commandes.Add(new Commande
            {
                Numero = 1001 + i,
                Client = clients[i * 3 % clients.Length],
                Date = new DateTime(2026, 10, 1).AddDays(i * 2),
                Montant = 5m + i * 7919 % 1500 + i * 37 % 100 / 100m,
                Payee = i % 2 == 0 || i % 5 == 0,
                Statut = (StatutCommande)(i * 2 % 3),
                Prioritaire = i % 7 == 3
            });
        }
        return commandes;
    }
}

/// <summary>Entrée de la liste des statuts (colonne Statut) : valeur et texte traduit.</summary>
public class OptionStatut : INotifyPropertyChanged
{
    private readonly string cle;

    private OptionStatut(StatutCommande valeur, string cle)
    {
        Valeur = valeur;
        this.cle = cle;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public StatutCommande Valeur { get; }

    public string Texte
    {
        get { return Textes.Get(cle); }
    }

    // Nom accessible des entrées de la liste déroulante (sans lui : « AccessTest.OptionStatut », constaté).
    public override string ToString()
    {
        return Texte;
    }

    /// <summary>Mêmes instances pendant toute la session : le texte affiché suit la langue sans recréer la liste.</summary>
    public static IReadOnlyList<OptionStatut> Liste { get; } = new List<OptionStatut>
    {
        new OptionStatut(StatutCommande.EnAttente, "DgStatutAttente"),
        new OptionStatut(StatutCommande.Expediee, "DgStatutExpediee"),
        new OptionStatut(StatutCommande.Livree, "DgStatutLivree")
    };

    public static void RafraichirTextes()
    {
        foreach (OptionStatut option in Liste)
        {
            option.PropertyChanged?.Invoke(option, new PropertyChangedEventArgs(nameof(Texte)));
        }
    }
}

/// <summary>Nom accessible d'une cellule Priorité (icône seule) : « Prioritaire » ou « Normale ».</summary>
public class PrioriteVersNom : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Textes.Get(value is bool prioritaire && prioritaire ? "DgPrioritaire" : "DgNormale");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>Nom accessible d'une cellule Statut : le texte traduit du statut.</summary>
public class StatutVersTexte : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        foreach (OptionStatut option in OptionStatut.Liste)
        {
            if (value is StatutCommande statut && option.Valeur == statut)
            {
                return option.Texte;
            }
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>Valeur UIA d'une cellule Payée : « coché » ou « non coché » (au lieu de « True » / « False »).</summary>
public class PayeeVersEtat : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Textes.Get(value is bool payee && payee ? "DgCoche" : "DgNonCoche");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
