# Onglet TextBox — AccessTest

Spécification de l'onglet **TextBox**. Voir `Fenetre.md` pour la zone d'onglets, le menu et la barre d'état, et `Specs.md` pour les règles d'accessibilité, `ChecklistNVDA.md` pour la checklist NVDA.

## 1. Contrôle

- Un `TextBox` plein onglet (`x:Name="TxtEditeur"`), multiligne :
  - `AcceptsReturn="True"` : Entrée insère un retour à la ligne ;
  - `AcceptsTab` : **faux au démarrage** (Tab sort du contrôle) ; l'item *Tab insère une tabulation* du menu contextuel (`Fenetre.md`, 2.7) le passe à vrai : Tab insère alors une tabulation et ne permet plus de quitter le contrôle au clavier ;
  - `TextWrapping="Wrap"` : retour à la ligne automatique au démarrage ; l'item *Retour à la ligne automatique* du menu contextuel (`Fenetre.md`, 2.7.3) le coupe (`NoWrap`, barre de défilement horizontale `Auto`) ;
  - `VerticalScrollBarVisibility="Auto"` : barre de défilement verticale quand elle est utile.
- En-tête d'onglet (`x:Name="TabTextBox"`) : « TextBox », non traduit.
- Contenu : un `UserControl` à part, `OngletTextBox` (`Source/Onglets/OngletTextBox.xaml` et `.xaml.cs`), placé dans l'onglet sous le nom `ContenuTextBox`. Le `x:Name` ci-dessus reste celui de son `TextBox`. Le menu contextuel est celui, partagé avec le RichText, de `MenuEditeurs` (`Fenetre.md`, 2.7) ; la fenêtre ne fait qu'appliquer la langue, charger et enregistrer le texte, et lire la position pour la barre d'état.
- Nom accessible (`AutomationProperties.Name`) : « Zone de texte brut » / « Plain text area ».
- Au démarrage, cet onglet est sélectionné et son en-tête reçoit le focus (Tab mène ensuite au `TextBox`).

## 2. Chargement d'un fichier

*Fichier > Ouvrir...* (`Fenetre.md`, 2.1.1) pour un fichier au choix ; *Fichier > Enregistrer...* enregistre le texte (`Fenetre.md`, 2.1.2). Chargement :

- le texte remplace le contenu actuel ;
- le curseur est placé au début du texte, et le `TextBox` reçoit le focus.

Au démarrage, le `TextBox` contient le texte de `Lisez-moi.txt`, intégré à l'exécutable à la compilation, curseur au début, sans prendre le focus (`Fenetre.md`, 2.5 et 3).

## 2b. Menu contextuel

Le `TextBox` a le menu contextuel décrit dans `Fenetre.md`, 2.7 (partagé avec le `RichTextBox`) : **Retour à la ligne automatique** (cochée au démarrage, seulement dans ce contrôle) et **Correcteur orthographique** (case à cocher, décochée au démarrage, avec un item Suggestions en tête du menu sur un mot mal orthographié). Il remplace le menu contextuel natif.

## 3. Position dans la barre d'état

Le champ 1 de la barre d'état donne `Ligne {0}, colonne {1}`, numérotée à partir de 1 :

- ligne : numéro de la ligne affichée qui contient le curseur (`GetLineIndexFromCharacterIndex`) ;
- colonne : position du curseur dans cette ligne (`CaretIndex` moins l'indice du premier caractère de la ligne).

C'est la ligne affichée, comme à la lecture aux flèches. Avec *Retour à la ligne automatique* décoché (menu contextuel), elle coïncide avec la ligne du texte.

## 4. Lecture par un lecteur d'écran

- Type de contrôle UIA : `Edit`, avec le motif `Text`. Il se lit ligne par ligne aux flèches.
- Les lignes vides sont lues de travers sans correctif : le lecteur lit la ligne du dessous (bug WPF dotnet/wpf#7190). Le menu **Correctif > Corriger les lignes vides** (cochée au démarrage) le corrige. Voir `Correctifs.md`, et point 13 de `ChecklistNVDA.md`.
- Points de `ChecklistNVDA.md` qui concernent cet onglet : 1, 4, 5, 6, 8, 13.

## Textes

Textes FR/EN de l'onglet (`Textes.cs` les reprend ; le menu et la barre d'état sont dans `Fenetre.md`, section 5).

| Élément | FR | EN |
|---|---|---|
| Menu contextuel du `TextBox` seulement : retour à la ligne automatique (à cocher) | _Retour à la ligne automatique | _Word wrap |
| Nom accessible du `TextBox` | Zone de texte brut | Plain text area |
