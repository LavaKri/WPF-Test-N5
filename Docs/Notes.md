# Notes de recherche

Contexte et sources rassemblés avant la spécification. Les points marqués **(non vérifié)** viennent de ma connaissance générale ou de résultats de recherche non confirmés. Date : 2026-11-13.

## 1. Frameworks : noms et différences

| Désignation officielle | Versions | Plateformes | Statut |
|---|---|---|---|
| .NET Framework | 1.0 à 4.8.1 | Windows | Maintenance seule, inclus dans Windows |
| .NET Core | 1.0 à 3.1 | Windows, Linux, macOS | Plus supporté |
| .NET (sans « Core ») | 5, 6, 7, 8, 9, 10… | Windows, Linux, macOS | Produit actuel |

- Moniker du `.csproj` : `net48` = .NET Framework 4.8 ; `net8.0-windows`, `net10.0-windows` = .NET moderne pour WPF. Avec un point : moderne ; sans point : .NET Framework.
- .NET Standard n'est pas un runtime : une application WPF ne peut pas le cibler. WPF ne fonctionne ni sur Mono, Xamarin, MAUI ni Blazor.
- Versions paires (6, 8, 10) = LTS ; impaires = STS. La fin de support de .NET 8 est annoncée vers novembre 2026 **(non vérifié, voir la page « .NET Support Policy » de Microsoft)**.

### Déploiement

- .NET Framework 4.8 est préinstallé sur Windows 11 et sur Windows 10 depuis la 1903. 4.8.1 : Windows 11 22H2 et suivants seulement.
- .NET 10 n'est pas inclus dans Windows. Pour exécuter : runtime Desktop (`Microsoft.WindowsDesktop.App`). Pour développer : SDK.
- Publication : dépendante du framework (léger, runtime requis) ou autonome `--self-contained` (environ 70 à 150 Mo, moins avec `PublishSingleFile` + compression).
- Un seul SDK .NET 10 compile les cibles `net10.0-windows` et `net48` pour des projets au format SDK. Validé ici pour WPF, sans Visual Studio. Les anciens projets (`packages.config`) demandent Visual Studio Build Tools.

### Machine de l'utilisateur

SDK .NET 10.0.404 installé le 2026-11-13. Runtimes présents : 8.0.23 et 10.0.11 (NETCore.App et WindowsDesktop.App). NVDA déjà utilisé nativement.

## 2. Accessibilité de WPF

- Principe : WPF expose les contrôles par UI Automation (UIA), via des `AutomationPeer`. NVDA, JAWS et Narrator lisent ces informations.
- Les améliorations d'accessibilité de .NET Framework 4.7.1 à 4.8 sont incluses par défaut dans .NET moderne, sans interrupteur de retour en arrière ([source](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/windows-forms-accessibility-improvements)). À noter : cette page parle surtout de Windows Forms.
- .NET Framework 4.7.1 : régions vivantes (`LiveSetting`), `Expander` annoncé comme groupe, `DataGridCell`, `ComboBox`, `PasswordBox`, améliorations du contraste élevé ([source](https://devblogs.microsoft.com/dotnet/net-framework-4-7-1-accessibility-and-wpf-improvements/)). Pour une application ciblant une version antérieure, elles sont activables par des interrupteurs `AppContext`.
- Bouton non signalé comme élément de contrôle : corrigé dans .NET Framework 4.8.1 et .NET Core 3.1+ ([source](https://learn.microsoft.com/en-us/accessibility-tools-docs/items/wpf/button_iscontrolelementtruerequired)).
- Info-bulles conformes WCAG 2.1 et lues par NVDA/Narrator : annoncées pour 4.8.1. Équivalent .NET 10 **(non vérifié)**.
- Régression .NET 8 : `AutomationPeer` des `ItemsControl` inaccessibles aux outils UIA ([Telerik](https://www.telerik.com/products/wpf/documentation/knowledge-base/kb-common-net8-automationpeers-issue), tickets dotnet/wpf 8715 et 8679). Statut dans les dernières 8.0.x **(non vérifié)**. Ne concerne pas forcément .NET 10.
- Fuite mémoire UIA sur contrôles virtualisés (`DataGrid`, `ListView`) : correctif dans `dotnet/wpf` ([PR 11657](https://gh.r-mj.com/dotnet/wpf/pull/11657)). Version de livraison **(non vérifié)**.
- Aucun comparatif publié NVDA/JAWS contrôle par contrôle entre .NET Framework 4.8 et .NET 10 n'a été trouvé. D'où ce projet.

### Thème Fluent (.NET 9 et suivants)

- `ThemeMode` : `Light`, `Dark`, `System`, `None` (défaut, thème Aero2). Définir `ThemeMode` en code est expérimental (avertissement WPF0001) ([source](https://learn.microsoft.com/dotnet/desktop/wpf/whats-new/net90)).
- Bug : plantage `XamlParseException` avec le thème Fluent en contraste élevé, corrigé dans .NET 9.0.1 ([dotnet/wpf n° 10043](https://github.com/dotnet/wpf/issues/10043)).
- Le projet utilise Aero2 pour l'instant.

### Comparaison .NET 10 / .NET Framework 4.8 (ancienne application de test)

Arbres UIA identiques pour les contrôles standard. Seuls écarts : jours abrégés du `Calendar` (`L` en .NET 10, `lu` en 4.8) et ponctuation d'un nom de cellule du `DataGrid`.

## 3. Contrôles WPF d'édition de texte

| Contrôle | Usage | Remarques |
|---|---|---|
| `TextBox` | Texte brut | `SpellCheck.IsEnabled`, `MaxLength`, `AcceptsReturn`, `AcceptsTab`, `IsReadOnly` |
| `PasswordBox` | Saisie masquée | `Password` n'est pas une propriété de dépendance (pas de liaison directe) |
| `RichTextBox` | Texte formaté | Contenu en `FlowDocument` ; pas de barre d'outils fournie |
| `ComboBox` (`IsEditable`) | Liste ou saisie libre | `TextBox` interne |
| `DatePicker` | Date | `DatePickerTextBox` interne |

Cellules `DataGridTextColumn` : `TextBox` en mode édition. Absents en natif : champ numérique à flèches, masque de saisie, éditeur de code, barre de mise en forme.

## 4. Bibliothèques tierces envisagées

- **Extended WPF Toolkit** (Xceed Software) : environ 48 contrôles (`IntegerUpDown`, `MaskedTextBox`, `DateTimePicker`, `PropertyGrid`…). Dépôt [xceedsoftware/wpftoolkit](https://github.com/xceedsoftware/wpftoolkit), NuGet `Extended.Wpf.Toolkit`. Version 5.1.2 indiquée en juin 2026 **(non vérifié)**. **Licence** : depuis la 4.0.0, Xceed Community License, **usage non commercial uniquement** ; édition payante Plus pour le commercial. Accessibilité : aucune information trouvée.
- **AvalonEdit** (équipe icsharpcode, licence MIT depuis la 5.0) : éditeur de code (numéros de ligne, coloration, repli, complétion). Dernière version indiquée : 6.3.1.120 ([NuGet](https://www.nuget.org/packages/AvalonEdit/)). Une contribution a ajouté un support basique de `TextPattern` UIA ; étendue du support lecteur d'écran **non vérifiée**.
- Alternatives commerciales : DevExpress, Telerik, Syncfusion (accessibilité documentée à part).
- Principe retenu : contrôles natifs d'abord, tiers seulement si nécessaire et après test NVDA/JAWS.

## 5. Constats d'essai (UIA, ancienne application de test)

Comparaison .NET 10.0.15 / .NET Framework 4.8.10008.0 : environ 395 éléments par côté, arbres identiques pour les contrôles statiques. Écarts : jours du `Calendar` (`L M M J V S D` en .NET 10, `lu ma me…` en 4.8) ; ponctuation d'un nom de cellule du `DataGrid` (point en .NET 10, virgule en 4.8). Rien n'a été testé avec un lecteur d'écran, ni les régions vivantes, la validation et les dialogues.

Défauts constatés dans les deux frameworks :
- `Label` + `Target` : pas de nom ni de `LabeledBy` pour `TextBox`, `PasswordBox`, `ComboBox`, `Slider`, `ListBox`, `DataGrid`, `TreeView`. Corrigé par `AutomationProperties.LabeledBy` ou `Name`.
- `ListView` + `GridView` exposé comme `DataGrid` ; lignes nommées par `ToString()`.
- Cellules à case à cocher du `DataGrid` sans nom (« Élément : … Index d'affichage de colonne : 3 »).
- Image et bouton à icône seule sans nom ; un `Border` cliquable n'a pas de rôle bouton.
- Chaque `Label` est exposé deux fois (avec et sans `_` de la touche d'accès) : lecture double possible dans NVDA **(à vérifier)**. Relevé des peers par programme console (.NET 10, 2026-11-18) : `LabelAutomationPeer` (`Text`, nom « Identifiant ») avec un enfant `TextBlockAutomationPeer` (`Text`, nom « _Identifiant ») ; le champ cible expose `AccessKey` = « I » (déduite du `Label` par WPF), aussi vue par un client UIA ; relevé UIA du contrôle compilé (`OngletUsuels`) : « I » et « M », lettre seule, sans « Alt+ ». NVDA, réglage des raccourcis coché, dit la lettre seule en fin d'annonce au focus du champ (utilisateur, 2026-11-18). `AutomationProperties.AccessKey` posée sur le champ est ignorée (`GetAccessKey()` du peer et propriété UIA vides, même avec un `Label`) : ce n'est pas un contournement.
- `Hyperlink` dans un `TextBlock` : le `TextBlockAutomationPeer` (`Text`, nom = le texte) a pour enfant le `HyperlinkAutomationPeer` (`Hyperlink`, même nom, motif `Invoke`, aucun enfant). NVDA lit « Mot de passe oublié, mot de passe oublié, lien » au focus du lien (constat de l'utilisateur, 2026-11-18). **Hypothèse** : NVDA annonce l'ancêtre `Text` (nom, sans rôle pour un texte statique) puis le lien. Piste **retenue** (2026-11-18) sous forme de patch Harmony, `CorrectifLienSeul` (`Specs/Correctifs.md`) ; l'essai d'origine : un `TextBlock` dont le peer (sous-classe de `TextBlockAutomationPeer`) renvoie faux à `IsControlElementCore` et `IsContentElementCore` ; vérifié par programme console : le lien reste enfant, motif `Invoke` conservé. `AutomationProperties.AccessibilityView` n'existe pas dans WPF.
- `StatusBarItem` non exposé en UIA.

### Constats détaillés

Un constat par essai (symptôme, cause, solutions étudiées, ce qui a été retenu), regroupés par thème dans `Docs/Constats/`.

| Constat | Fichier |
|---|---|
| Ligne vide dans `TextBox` / `RichTextBox` : lecture de la ligne voisine | [`Texte.md`](Constats/Texte.md) |
| `RichTextBox` : texte illisible avec NVDA | [`Texte.md`](Constats/Texte.md) |
| Document mis en forme dans un `RichTextBox` : seul le tableau est signalé | [`Texte.md`](Constats/Texte.md) |
| Suggestions d'orthographe dans un menu contextuel personnalisé | [`Texte.md`](Constats/Texte.md) |
| Réutiliser le correctif des lignes vides dans un autre projet | [`Texte.md`](Constats/Texte.md) |
| Items de menu désactivés : sautés au clavier, donc invisibles pour NVDA | [`Menus.md`](Constats/Menus.md) |
| Raccourci d'un item de menu (« Ctrl+O ») jamais annoncé | [`Menus.md`](Constats/Menus.md) |
| Cases à cocher dans un `TreeView` : état invisible pour NVDA | [`Arbre-Grille-Calendrier.md`](Constats/Arbre-Grille-Calendrier.md) |
| `Calendar` : piège Maj+Tab et lecture des jours | [`Arbre-Grille-Calendrier.md`](Constats/Arbre-Grille-Calendrier.md) |
| `DataGrid` : contenu des cellules lu deux fois par NVDA | [`Arbre-Grille-Calendrier.md`](Constats/Arbre-Grille-Calendrier.md) |
| Barre d'état introuvable par NVDA (NVDA+Fin) | [`Fenetre-Annonces.md`](Constats/Fenetre-Annonces.md) |
| Région vivante dans un onglet non affiché | [`Fenetre-Annonces.md`](Constats/Fenetre-Annonces.md) |
| `ProgressBar` : pas de texte, valeur non lue par JAWS | [`Fenetre-Annonces.md`](Constats/Fenetre-Annonces.md) |
| Autres contrôles corrigés par Microsoft (Accessibility Insights, Guy Barker) | [`Sources-Microsoft.md`](Constats/Sources-Microsoft.md) |

## 6. Outils de test

- NVDA (en usage), Narrator (`Win` + `Ctrl` + `Entrée`), JAWS (démo d'environ 40 minutes par session sans licence).
- Accessibility Insights for Windows (gratuit, inspection de l'arbre UIA). Identifiants `winget` de NVDA (`NVAccess.NVDA`) et d'Accessibility Insights **(non vérifiés)** : confirmer avec `winget search`.
- `Audit-UIA.ps1` : relevé UIA d'une application compilée.
