# Spécifications — AccessTest (test d'accessibilité WPF)

Ce fichier est l'entrée de la spécification : il donne le plan, puis ce qui est commun à toute l'application.
Un seul projet, `Source/AccessTest.csproj`, compile les deux cibles à partir des mêmes sources (XAML et code-behind) : toute modification s'applique aux deux.

| Cible (`TargetFrameworks`) | Binaires | État |
|---|---|---|
| `net10.0-windows` (.NET 10) | `Net10\Debug\AccessTest.exe` | **En cours** : cible des tests NVDA jusqu'ici |
| `net48` (.NET Framework 4.8) | `Net48\Debug\AccessTest.exe` | Compilée depuis le 2026-11-16, **pas encore lancée ni testée** |

Qualité du code, vérifiée à chaque modification (commandes dans `CLAUDE.md`) : analyseurs .NET (`AnalysisLevel` `latest-recommended`) et règles de style du `.editorconfig` de la racine, **0 avertissement** à la compilation des deux cibles ; mise en forme conforme à `dotnet format` ; correctifs compilables seuls en C# 7.3 (projet `Controle`, `Correctifs.md`).

But : comparer le comportement des contrôles WPF avec un lecteur d'écran (NVDA, JAWS), d'abord sur .NET 10, puis sur .NET Framework 4.8.

Les points marqués **(hypothèse)** sont une interprétation à confirmer. Voir la section « Questions ouvertes ».

## 1. Fichiers de la spécification

| Fichier | Contenu |
|---|---|
| `Specs.md` | Ce fichier : plan, règles d'accessibilité, questions ouvertes (les textes FR/EN sont dans chaque fichier). |
| `ChecklistNVDA.md` | La checklist NVDA (points numérotés) avec les résultats des tests, un seul tableau. |
| `Fenetre.md` | La fenêtre principale, le menu (Fichier, Édition, Aide), le menu contextuel des zones de texte, la zone d'onglets et la barre d'état. |
| `Correctifs.md` | Le menu **Correctif** et chacun de ses correctifs de défauts de WPF. |
| `TextBox.md` | L'onglet **TextBox** : le `TextBox` multiligne. |
| `RichText.md` | L'onglet **RichText** : la barre de mise en forme (`ToolBar` : gras, italique, souligné, alignement) et le `RichTextBox`. |
| `Usuels.md` | L'onglet **Usuels** : formulaire de connexion (liste, champs, mot de passe, case à cocher, bouton, lien, libellés à touche d'accès, barre de progression, zone de résultat en lecture seule). |
| `Choix.md` | L'onglet **Choix** : contrôles de choix simple et multiple (boutons radio, liste multiple, case à trois états, bouton à bascule, calendrier, sélecteur de date). |
| `TreeView.md` | L'onglet **TreeView** : deux arbres (« Dossiers », « Dossiers à cocher ») liés à une même source de données (trois branches), chacun avec un menu contextuel, le second avec une case à cocher par nœud. |
| `DataGrid.md` | L'onglet **DataGrid** : grille de commandes liée à une source de données (une colonne par type courant), menu contextuel de deux corrections (un seul arrêt de tabulation, tri au clavier). |
| `Annonce.md` | L'onglet **Annonce** : envoyer un texte au lecteur d'écran par une sortie au choix (événement de notification UIA, région vivante, focus), avec ses paramètres, une fois ou toutes les 5 secondes. |

## 2. Textes

Les textes FR/EN sont répartis : ceux du menu, des menus contextuels des zones de texte et de la barre d'état dans `Fenetre.md`, section 5 ; ceux de chaque onglet dans le fichier de l'onglet, section « Textes ». `Textes.cs` les reprend.

## 3. Règles d'accessibilité (valables pour tout l'écran)

1. Tout contrôle interactif a un nom accessible non vide (`AutomationProperties.Name`, ou texte visible propre au contrôle : bouton, item de menu).
2. Tout est utilisable au clavier seul, dans un ordre de tabulation logique : menu (Alt ou F10), onglets, éditeur, barre d'état.
3. Le focus clavier est toujours visible.
4. Aucune information n'est transmise par la couleur seule.
5. Contrastes suffisants, y compris en thème Contraste élevé de Windows. Thème WPF par défaut (Aero2), pas de thème Fluent pour l'instant.
6. Contrôles standard WPF uniquement, sans `ControlTemplate` personnalisé.

## 4. Checklist NVDA

Voir `ChecklistNVDA.md` (actions, attendu et résultats des tests, un seul tableau).

## 5. Questions ouvertes

| # | Question | Hypothèse retenue |
|---|---|---|
| — | Aucune question ouverte (Q6, barre de mise en forme du RichText, tranchée le 2026-11-17 : `RichText.md`, 1b). | — |
