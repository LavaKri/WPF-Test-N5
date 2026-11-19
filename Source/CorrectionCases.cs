using System.ComponentModel;

namespace AccessTest;

/// <summary>
/// Réglage « Corriger les cases » du second arbre de l'onglet TreeView (Specs/TreeView.md, section 5).
/// Objet unique, lié dans le XAML pour que le changement agisse sans recréer l'arbre.
/// </summary>
public class CorrectionCases : INotifyPropertyChanged
{
    public static CorrectionCases Instance { get; } = new CorrectionCases();

    private bool actif = true;

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>Coché au démarrage, comme le menu Correctif.</summary>
    public bool Actif
    {
        get { return actif; }
        set
        {
            if (actif != value)
            {
                actif = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Actif)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CaseFocalisable)));
            }
        }
    }

    /// <summary>Corrigé : la case n'est plus un arrêt de focus, le nœud est le seul arrêt.</summary>
    public bool CaseFocalisable
    {
        get { return !actif; }
    }
}
