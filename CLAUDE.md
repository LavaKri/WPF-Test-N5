# AccessTest — test d'accessibilité WPF

Application WPF qui sert à vérifier, avec un lecteur d'écran (NVDA, JAWS), l'accessibilité des contrôles standard,
d'abord sur .NET 10, puis sur .NET Framework 4.8.

## À lire en premier

- `Specs/` : la référence de l'application, un fichier par sujet. `Specs/Specs.md` donne le plan et ce qui est commun (règles d'accessibilité, questions ouvertes) ; les textes FR/EN sont dans la section « Textes » de chaque fichier ;
  `Specs/ChecklistNVDA.md` donne la checklist NVDA et ses résultats (à lire seulement pour préparer ou noter un test) ;
  `Specs/Fenetre.md` décrit la fenêtre, le menu, la zone d'onglets et la barre d'état ; `Specs/Correctifs.md`, le menu Correctif et ses correctifs ; `Specs/TextBox.md`, `Specs/RichText.md`, `Specs/Usuels.md`, `Specs/Choix.md`, `Specs/TreeView.md`, `Specs/DataGrid.md` et `Specs/Annonce.md` décrivent chacun un onglet.
  Toute évolution de l'application commence par une mise à jour de ces fichiers.
- `Avancement.md` : l'état d'avancement (où on en est, fait, à faire, décisions). À mettre à jour à la fin de chaque session.
- `Docs/Notes.md` : recherches préalables et sources (frameworks, accessibilité de WPF, bibliothèques tierces, liste des constats UIA détaillés). À consulter au besoin, pas à relire à chaque session.
- `Docs/Constats/` : les constats d'essai détaillés (UIA, lecteur d'écran), un fichier par thème (`Texte`, `Menus`, `Arbre-Grille-Calendrier`, `Fenetre-Annonces`, `Sources-Microsoft`), indexés par la section 5 de `Notes.md`. Ouvrir seulement celui du sujet traité.
- `Docs/` : `Notes.md` (ci-dessus). `Lisez-moi.txt` (racine) est intégré à l'exécutable (ressource incorporée) et chargé au démarrage dans le `TextBox` ; le `RichTextBox` reçoit au démarrage un document mis en forme construit par le code.

## Dossiers

| Dossier | Rôle |
|---|---|
| `Specs` | La spécification (voir plus haut). |
| `Docs` | Notes de recherche (voir plus haut). |
| `Source` | Les sources (XAML, C#) et l'unique projet `AccessTest.csproj`, **multi-cible** (`net10.0-windows` et `net48`). Implémente `Specs/`. `MainWindow` est une classe partielle : `MainWindow.xaml.cs` (constructeur, chargement, langue, barre d'état), `MainWindow.Menus.cs` (Fichier, Aide), `MainWindow.Correctifs.cs`, `Onglets/MainWindow.Editeurs.cs` (menu contextuel partagé et F7). Chaque onglet est un `UserControl` dans `Source/Onglets/` (`OngletTextBox`, `OngletRichText`, `OngletUsuels`, `OngletChoix`, `OngletTreeView`, `OngletDataGrid`, `OngletAnnonce` : un `.xaml` et un `.xaml.cs` chacun), placé dans son `TabItem` de `MainWindow.xaml` sous le nom `ContenuXxx` ; il s'initialise dans son constructeur et expose `AppliquerLangue()` ; TextBox, RichText et DataGrid exposent aussi `Position(focus)` et `PositionModifiee` pour la barre d'état. Le menu contextuel commun à TextBox et RichText est `MenuEditeurs` (un `ContextMenu` en XAML, `Onglets/MenuEditeurs.xaml` et `.xaml.cs`), créé par `MainWindow`. |
| `Net10` | Binaires .NET 10 seulement (`Net10\Debug\AccessTest.exe`), produits par la compilation de `Source`. Non versionné. |
| `Net48` | Binaires .NET Framework 4.8 seulement (`Net48\Debug\AccessTest.exe`). Non versionné. |
| `Correctifs` | Correctifs WPF réutilisables, hors de tout projet, un fichier par correctif (liste plus bas), inclus dans `Source/AccessTest.csproj` par lien `<Compile Include>`. Ceux qui dépendent de `Lib.Harmony` sont marqués ; les autres n'ont aucune dépendance. Doit rester autonome : `using` explicites, C# 7.3, aucune dépendance à AccessTest (vérifié par `Controle`). |
| `Controle` | `ControleCorrectifs.csproj` : projet de contrôle qui compile `Correctifs/*.cs` seuls, en net48, C# 7.3, sans `using` implicites. Il échoue sur toute syntaxe trop récente ou tout `using` manquant. Ne produit rien d'utile. |

Un seul projet compile les deux cibles à partir des mêmes sources (XAML et code-behind).
Pour que ce soit possible : `Nullable` désactivé, `LangVersion` latest, pas de syntaxe ni d'API réservée à une seule cible. Une API absente des assemblys de référence de 4.8 (mais présente dans son runtime) s'appelle par réflexion, comme `RaiseNotificationEvent` (`Specs/Annonce.md`, 6).

## Commandes

Depuis `Source` :

```powershell
dotnet build                    # les deux cibles : Net10\Debug et Net48\Debug
dotnet run -f net10.0-windows   # ou -f net48
```

Lanceurs à la racine : `Lance Net10.bat`, `Lance Net48.bat`.

Vérification du code, après chaque modification (règles dans `.editorconfig` à la racine) :

```powershell
dotnet build                          # analyseurs .NET et règles de style : 0 avertissement attendu
dotnet format --verify-no-changes     # mise en forme, ordre des using, fins de ligne ; sans l'option : corrige
dotnet build ..\Controle              # Correctifs autonomes, en C# 7.3
```

- `AnalysisLevel` vaut `latest-recommended` (`AccessTest.csproj`) ; les exceptions (CA1305, CA1863) sont justifiées dans `.editorconfig`.
- Fins de ligne : CRLF sur la copie de travail, LF dans le dépôt. Un fichier écrit en LF est signalé par `dotnet format` (erreur `ENDOFLINE`) ; `dotnet format whitespace` le corrige.

Relevé UI Automation d'une application compilée, utile pour comparer deux versions :

```powershell
.\Audit-UIA.ps1 -Exe .\Net10\Debug\AccessTest.exe -Detail -Sortie releve.txt
```

- Un seul SDK .NET 10 suffit à compiler les cibles `net10.0-windows` **et** `net48` (compilation `net48` validée avec le SDK seul, sans Visual Studio).
- Dans un projet WPF, `System.IO` n'est pas dans les `using` implicites : l'ajouter à la main.

## Conventions

- Répondre en français.
- Git : **ne jamais committer sans que l'utilisateur le demande** ; quand il le demande, committer **directement sur `main`**, sans créer de branche.
- Publication GitHub : la branche **`Public`** ne contient que des instantanés de publication (« Version 0.1 », « Version 0.2 »…), sans l'historique de `main`. Elle ne reçoit **jamais de commit à la main** : on la fait avancer d'un commit à chaque publication, à partir de l'arbre de `main` (`git commit-tree main^{tree} -p Public -m "Version X.Y"`, puis `git branch -f Public <commit>`), avec l'étiquette `vX.Y`. Vers GitHub, ne pousser que `Public` (`git push origin Public:main`), jamais `main` ni `--all`. Pas de pull request ni de contribution : `Public` n'est jamais fusionnée dans `main`.
- Les x:Name des contrôles suivent `Specs/` et servent d'`AutomationId` pour les tests UIA.
- Les textes FR/EN sont dans `Textes.cs`.
- Un gestionnaire d'événement de sélection (`SelectionChanged`) branché dans le XAML se déclenche pendant `InitializeComponent` si le contrôle a une sélection initiale : il plante si le gestionnaire lit un contrôle déclaré plus bas. Brancher ces gestionnaires dans le constructeur, après `InitializeComponent`.
- Lancer un test UIA : `Start-Process` sur l'exécutable, puis `System.Windows.Automation` (`InvokePattern`, `ExpandCollapsePattern`, `SelectionItemPattern`, `TogglePattern`). `SendKeys` pour la saisie.
  Les sous-menus fermés ne sont pas dans l'arbre : déplier avant de chercher un item.

## Pièges WPF

Détail et sources : `Docs/Constats/` (index : `Docs/Notes.md`, section 5). Description des correctifs : `Specs/Correctifs.md`.

Règles à respecter en codant :

- `Label` + `Target` ne donne ni nom ni `LabeledBy` : poser `AutomationProperties.Name` ou `LabeledBy`.
- `StatusBarItem` n'est pas exposé en UIA : poser le nom accessible sur le contrôle de contenu.
- `ListView` + `GridView` est exposé comme un `DataGrid` ; ses lignes s'appellent comme le `ToString()` de l'objet.
- `RichTextBox` : ne jamais remplacer `Document` après coup (le peer UIA reste lié à l'ancien, NVDA ne lit plus rien) ; vider et remplir le document existant.
- `MenuItem` à cocher : le `TogglePattern` change `IsChecked` sans `Click` ; brancher `Checked` / `Unchecked`.
- `ScrollBar` isolée : non focalisable, donc inutilisable au clavier ; pour régler une valeur, utiliser un `Slider`.
- Pas de thème Fluent pour l'instant (Aero2).
- Les huit correctifs du menu Correctif sont **cochés au démarrage**, comme *Lire les liens une seule fois* (menu contextuel du lien de l'onglet Usuels) ; pour voir le comportement natif de WPF (test NVDA ou UIA), décocher les huit et cet item.

Comportements natifs conservés tels quels (à ne pas corriger) :

- `Calendar` en `SingleDate` : la flèche sélectionne le jour atteint, Espace ne fait rien dans la vue des jours, NVDA n'annonce pas la sélection.
- Région vivante levée sur un élément d'un onglet non affiché : NVDA lit le nom de l'onglet (« Annonce ») au lieu du message. La notification UIA levée sur la fenêtre est lue dans tous les onglets.
- `RichTextBox` : même avec `IsDocumentEnabled` vrai, un lien n'est pas activable au clavier (Entrée ne fait rien). L'onglet RichText l'active lui-même par Entrée (code de l'application, `Specs/RichText.md`, 2c).

Défauts de WPF corrigés (menu Correctif, `Correctifs/`) :

| Défaut natif | Correctif |
|---|---|
| Lignes vides lues de travers (dotnet/wpf#7190) | `CorrectifLigneVide` (Harmony) |
| NVDA+Fin ne trouve pas la `StatusBar` | `CorrectifBarreEtat` |
| `Calendar` : Maj+Tab laisse le focus sur le jour | `CorrectifCalendrier` |
| `ToolBar` : Tab et Maj+Tab tournent dans la barre | `CorrectifBarreOutils` |
| Item de menu désactivé : non focalisable, jamais annoncé | `CorrectifMenuIndisponible` (Harmony) |
| Raccourci de menu jamais annoncé ; bouton lié à une commande : texte de commande (« ToggleBold ») annoncé comme raccourci | `CorrectifRaccourciMenu` (Harmony) ; une infobulle qui répète le raccourci se neutralise par un `HelpText` d'une espace |
| `TreeView` à cases : NVDA ne voit pas la case (pas de motif `Toggle`) | `CorrectifArbreCases` |
| `DataGrid` : plusieurs arrêts de tabulation, en-têtes non focalisables, cellules lues deux fois | `CorrectifGrille` |
| Touche d'accès exposée par la lettre seule (« i » au lieu de « Alt+I ») ; `AutomationProperties.AccessKey` ignorée | `CorrectifToucheAcces` (Harmony) |
| `Hyperlink` seul dans un `TextBlock` : nom lu deux fois (le `TextBlock` porte le même nom) | `CorrectifLienSeul` (Harmony ; item dans le menu contextuel du lien de l'onglet Usuels) |
| `RichTextBox` : titres, liens, listes et cellules de tableau non reconnus ; contenu « non disponible » tant que `IsDocumentEnabled` est faux | `CorrectifLectureRiche` (Harmony ; titres marqués par `NiveauTitre`, en-tête de tableau par `LigneEntete`) |

## État d'avancement

Voir `Avancement.md`. En fin de session, le mettre à jour (où on en est, cases à cocher ; résultats NVDA dans `Specs/ChecklistNVDA.md`) sans recopier son contenu ici.
