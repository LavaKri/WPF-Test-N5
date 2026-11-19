# Fenêtre, menu, zone d'onglets et barre d'état — AccessTest

Partie de la spécification qui décrit la fenêtre principale et ses trois zones, de haut en bas : menu, zone d'onglets, barre d'état.
Voir `Specs.md` pour l'ensemble (plan, règles d'accessibilité, questions ouvertes) et `ChecklistNVDA.md` pour la checklist NVDA, et `TextBox.md` et `RichText.md` pour le contenu des onglets.

## 1. Fenêtre

- Une seule fenêtre principale, **maximisée au démarrage** (`WindowState="Maximized"`).
- Titre : `<nom> — <framework>`, par exemple `WPF-Test-N5 — .NET 10.0.15` ou `WPF-Test-N5 — .NET Framework 4.8.10008.0`
  (valeur de `RuntimeInformation.FrameworkDescription`). Le nom est le `<Product>` de `Source/AccessTest.csproj`, **seule source** du nom : le titre et « À propos » le lisent dans l'assembly (`Textes.NomApplication`). Le nom du fichier exécutable reste `AccessTest.exe` (nom du projet).
- Disposition verticale, de haut en bas : menu, zone d'onglets (occupe tout l'espace restant), barre d'état.

## 2. Menu

Menu classique (`Menu`) en haut de la fenêtre, pleine largeur. Menus, de gauche à droite : **Fichier**, **Édition**, **Correctif**, **Aide**.

Le menu **n'est pas dans l'ordre de tabulation** : Tab et Maj+Tab ne s'y arrêtent jamais (`IsTabStop="False"` et `KeyboardNavigation.TabNavigation="None"` sur le `Menu`). On y entre avec Alt ou F10 (ou une touche d'accès), on s'y déplace avec les flèches, on en sort avec Échap.

### 2.1 Fichier

| Item | Sous-items | Comportement |
|---|---|---|
| **Ouvrir...** (Ctrl+O) | — | Boîte de dialogue standard d'ouverture de fichier (`Microsoft.Win32.OpenFileDialog`) ; le fichier choisi est chargé dans la zone de texte de l'onglet actif (2.1.1). **Disponible seulement dans les onglets TextBox et RichText.** |
| **Enregistrer...** (Ctrl+S) | — | Boîte de dialogue standard d'enregistrement (`Microsoft.Win32.SaveFileDialog`) ; enregistre le texte de la zone de texte de l'onglet actif (2.1.2). **Disponible seulement dans les onglets TextBox et RichText.** |
| **Choix d'un dossier...** | — | Boîte de dialogue standard de choix de dossier (`Microsoft.Win32.OpenFolderDialog`) ; le dossier choisi devient le dossier de départ d'Ouvrir et d'Enregistrer (2.1.3). **Disponible dans tous les onglets, mais seulement en .NET 10** : grisé en .NET Framework 4.8, qui n'a pas cette boîte. |
| **Toujours grisée** | — | Item de vérification, **toujours indisponible** (grisé), pour tester le correctif des items indisponibles (`Correctifs.md`) en .NET 10 et en .NET Framework 4.8. Il ne fait rien. |
| **Langue** | **FR** (coché par défaut), **EN** | Choix exclusif : un seul est coché à la fois. Change la langue de l'application (voir 2.6). |
| **Quitter** | — | Ferme l'application. |

Ordre : Ouvrir, Enregistrer, Choix d'un dossier, Toujours grisée, séparateur, Langue, séparateur, Quitter.

**Items indisponibles** (grisés) : Ouvrir et Enregistrer hors des onglets TextBox et RichText, Choix d'un dossier en .NET Framework 4.8, Toujours grisée toujours ; de même, dans le menu Édition, les commandes qui ne peuvent pas agir, et dans le menu contextuel des zones de texte, *Faute suivante* sans correcteur et « (aucune suggestion) ». Nativement, WPF les rend non focalisables : les flèches les sautent et NVDA ne les annonce jamais (constat de l'utilisateur, 2026-11-17). Avec le correctif *Atteindre les items indisponibles* (`Correctifs.md`), coché au démarrage, ils restent atteignables aux flèches, grisés, annoncés « indisponible », et sans effet. Ce sont des `MenuItem` ordinaires : l'indisponibilité est posée comme d'habitude, par `IsEnabled="False"` (Choix d'un dossier, Toujours grisée, Faute suivante) ou par la commande (`CanExecute` faux : Ouvrir, Enregistrer, Édition).

Il n'y a plus d'item *Charger* (supprimé le 2026-11-18) : le contenu des deux zones de texte est chargé au démarrage (2.5), et *Ouvrir* charge un fichier au choix.

But : tester avec le lecteur d'écran les boîtes de dialogue **natives de Windows** qu'ouvre WPF (elles ne sont pas des fenêtres WPF : leurs textes suivent la langue de Windows, pas celle de l'application), et le retour du focus à leur fermeture.

#### 2.1.1 Ouvrir

- Commande standard `ApplicationCommands.Open` : le raccourci Ctrl+O et le texte « Ctrl+O » affiché dans l'item viennent de la commande. L'item et le raccourci sont **désactivés** (`CanExecute` faux, item grisé, annoncé « indisponible ») quand l'onglet sélectionné n'est ni TextBox ni RichText. Dans ces deux onglets, ils agissent même si le focus est ailleurs que dans la zone de texte (sur l'en-tête de l'onglet, par exemple), comme F7 (2.7.2).
- Filtres : « Texte et Markdown (\*.txt;\*.md) » (au démarrage), « Texte (\*.txt) », « Markdown (\*.md) ». Un seul fichier.
- Dossier de départ : celui de 2.1.3. Titre de la boîte : celui de Windows (non posé par l'application).
- Le fichier est chargé (2.5) : encodage UTF-8, texte qui remplace le contenu, curseur au début, focus donné à la zone de texte. Dans le `RichTextBox`, un paragraphe simple par ligne ; le Markdown n'est **pas** interprété (le fichier `.md` est lu comme du texte).
- Annuler : rien ne change ; le focus revient où WPF le remet (comportement natif, à observer).
- Fichier illisible : `MessageBox` d'erreur avec le chemin et la raison.

#### 2.1.2 Enregistrer

- Commande standard `ApplicationCommands.Save` (Ctrl+S) : mêmes règles de disponibilité que Ouvrir.
- La boîte s'ouvre **à chaque fois** (comme un « Enregistrer sous ») : l'application ne retient pas le nom du fichier ouvert ou enregistré. Nom proposé : aucun. Écrasement d'un fichier existant : confirmation de Windows (`OverwritePrompt`, valeur par défaut).
- Filtres : « Texte (\*.txt) » (au démarrage), « Markdown (\*.md) » ; l'extension du filtre choisi est ajoutée si le nom n'en a pas.
- Dossier de départ : celui de 2.1.3.
- Contenu enregistré, **texte brut, UTF-8 sans BOM** :
  - `TextBox` : son texte, tel quel ;
  - `RichTextBox` : le texte du document (`TextRange` du début à la fin), un paragraphe par ligne, sans le saut de ligne que WPF ajoute après le dernier paragraphe (un fichier ouvert puis enregistré sans modification garde ses lignes). La mise en forme éventuelle est perdue.
- Après l'enregistrement, le focus est donné à la zone de texte. Rien n'est annoncé (pas de message de confirmation : on note ce que NVDA dit à la fermeture de la boîte).
- Échec d'écriture : `MessageBox` d'erreur avec le chemin et la raison.

#### 2.1.3 Choix d'un dossier

- `OpenFolderDialog` n'existe qu'à partir de .NET 8 : elle est **cherchée par réflexion** (type `Microsoft.Win32.OpenFolderDialog` dans l'assembly de `OpenFileDialog`), pour garder les mêmes sources pour les deux cibles. Introuvable (.NET Framework 4.8) : l'item est **désactivé** dès le démarrage (annoncé « indisponible »).
- Un seul dossier. Dossier de départ : le dossier de départ actuel (ci-dessous).
- Dossier choisi : il devient le **dossier de départ** d'Ouvrir, d'Enregistrer et de Choix d'un dossier, pour la session (il n'est pas enregistré), puis une `MessageBox` d'information affiche son chemin (titre « Choix d'un dossier », texte : section 5 ci-dessous). Annuler : rien ne change, pas de message.
- **Dossier de départ au démarrage** : le dossier `Docs` du dépôt, cherché en remontant les dossiers à partir de l'exécutable (comme les fichiers de 2.5). Introuvable : aucun dossier imposé (Windows choisit).

### 2.2 Édition

Items classiques, branchés sur les commandes standard WPF (`ApplicationCommands`) et appliqués au contrôle qui a le focus clavier :

| Item | Commande | Raccourci |
|---|---|---|
| Annuler | `Undo` | Ctrl+Z |
| Rétablir | `Redo` | Ctrl+Y |
| Couper | `Cut` | Ctrl+X |
| Copier | `Copy` | Ctrl+C |
| Coller | `Paste` | Ctrl+V |
| Tout sélectionner | `SelectAll` | Ctrl+A |

Les items sont désactivés quand le contrôle focalisé ne peut pas exécuter la commande.
Le focus doit revenir au contrôle d'édition après l'exécution (piège classique : la commande doit cibler le contrôle qui avait le focus avant l'ouverture du menu).

### 2.3 Correctif

Voir `Correctifs.md`.

### 2.4 Aide

| Item | Comportement |
|---|---|
| **À propos** | Ouvre une boîte de dialogue standard (`MessageBox`, bouton OK). Contenu : nom de l'application (même source que le titre), description, version (« Version 0.1 » : le `<Version>` de `Source/AccessTest.csproj`, lu dans l'assembly par `Textes.VersionApplication`), framework en cours (`RuntimeInformation.FrameworkDescription`). |

### 2.5 Contenu au démarrage et ouverture de fichiers

Décision de l'utilisateur (2026-11-18) : plus de menu *Charger* ; chaque zone de texte a son contenu dès le démarrage, et l'application est **autonome** (aucun fichier à côté de l'exécutable).

- **TextBox, au démarrage** : le texte de `Lisez-moi.txt` (racine du dépôt), **intégré à l'exécutable à la compilation** (ressource incorporée, `EmbeddedResource`, nom logique `Lisez-moi.txt`), lu en UTF-8. Curseur au début.
- **RichTextBox, au démarrage** : le document mis en forme, construit par l'application (`RichText.md`, 2c), dans la langue de démarrage (français). Il n'est pas retraduit au changement de langue (il peut avoir été modifié). Curseur au début.
- Au démarrage, **aucune des deux zones ne prend le focus** : il reste sur l'en-tête de l'onglet TextBox (section 3).
- **Fichier > Ouvrir** (2.1.1) : le fichier choisi remplace le contenu de la zone de texte de l'onglet actif (`TextBox.md` et `RichText.md`, section 2), encodage UTF-8, curseur au début, focus donné à la zone de texte. Fichier illisible : `MessageBox` d'erreur avec le chemin et la raison.

### 2.6 Changement de langue

- Langues : FR (défaut) et EN.
- Les textes propres à l'application (menus, nom des éléments, barre d'état, boîte « À propos ») basculent immédiatement. Table de traduction : section 5 ci-dessous.
- Pour les contrôles « compatibles » (ceux dont WPF fournit lui-même des textes : menu contextuel des champs, boutons du `MessageBox`, etc.) : appliquer la culture choisie à l'interface (`CurrentUICulture`) et à la propriété `Language` des éléments.
  **À vérifier** : ces textes changent-ils sans redémarrage, et diffèrent-ils entre .NET Framework 4.8 et .NET 10 ?
- La langue de lecture de NVDA est indépendante de ce réglage.

### 2.7 Menu contextuel des zones de texte

Le `TextBox` (onglet TextBox) et le `RichTextBox` (onglet RichText) ont le **même menu contextuel** (une seule instance de `ContextMenu`, partagée : la case à cocher a un seul état). Ce menu est la classe `MenuEditeurs` (`Source/Onglets/MenuEditeurs.xaml` et `.xaml.cs`, un `ContextMenu`), créée par la fenêtre et posée sur les deux zones par `Brancher` ; les réglages propres à une zone sont appliqués par les méthodes de son onglet. On l'ouvre avec la touche Applications, Maj+F10 ou le clic droit.

| Item | Sous-items | Comportement |
|---|---|---|
| **Correcteur orthographique** | — | Case à cocher, **décochée au démarrage** : active `SpellCheck.IsEnabled` sur le `TextBox` **et** le `RichTextBox`. |
| **Faute suivante (F7)** | — | Place la sélection sur le **prochain mot mal orthographié** de la zone de texte de l'onglet actif et lui donne le focus (2.7.2). **Disponible seulement quand le correcteur est actif** : item grisé sinon, et F7 ne fait rien. |
| **Tab insère une tabulation** | — | Case à cocher, **décochée au démarrage** : `AcceptsTab` du `TextBox` **et** du `RichTextBox`. Décochée, Tab (et Maj+Tab) quittent le contrôle ; cochée, Tab insère une tabulation et ne permet plus de quitter le contrôle (c'est le défaut constaté par l'utilisateur). Le menu contextuel reste accessible (Applications ou Maj+F10) pour la décocher. **À vérifier** : Ctrl+Tab sort-il du contrôle quand Tab insère une tabulation ? |
| **Retour à la ligne automatique** | — | **Seulement dans le menu ouvert depuis le `TextBox`** (2.7.3). Case à cocher, **cochée au démarrage** : `TextWrapping="Wrap"`. Décochée : `NoWrap`, une ligne du texte reste une seule ligne affichée, barre de défilement horizontale `Auto`. |
| **Largeur de page** | **Fenêtre**, **Étroite (300 pixels)**, **Large (2 000 pixels)** | **Seulement dans le menu ouvert depuis le `RichTextBox`** (2.7.4). Choix exclusif, **Fenêtre** coché au démarrage : largeur de page du document (`FlowDocument.PageWidth`). |
| **Correctif** | **Améliorer la lecture du texte enrichi**, **Document interactif** | **Seulement dans le menu ouvert depuis le `RichTextBox`** (2.7.5). Sous-menu des réglages qui changent ce qu'un lecteur d'écran trouve dans le document (demande de l'utilisateur, 2026-11-18). |
| **Suggestions** | une entrée par suggestion | Item et **Ignorer tout** ajoutés **en tête** du menu, seulement si le correcteur est actif et que le menu s'ouvre sur un mot mal orthographié (2.7.1). |

- Ce menu **remplace** le menu contextuel natif de WPF dans ces deux contrôles (Couper, Copier, Coller n'y sont plus). Ces actions restent dans le menu Édition et aux raccourcis clavier.
- Textes et touches d'accès : section 5 ci-dessous.
- Comme pour le menu Correctif, la case agit sur `Checked` / `Unchecked` et non sur `Click`.

#### 2.7.1 Suggestions d'orthographe

- À chaque ouverture (`ContextMenuOpening`), on retire les items de l'ouverture précédente (le menu est partagé), puis, si le correcteur est actif, on cherche la faute avec **la détection de WPF, sans modification** : la méthode interne `TextEditor.GetSpellingErrorAtSelection`, celle du menu contextuel natif et des commandes `EditingCommands.CorrectSpellingError` / `IgnoreSpellingError`. Elle n'est pas publique : elle est appelée par réflexion. Si elle est introuvable (autre version de WPF), pas de suggestions.
  - Règle de WPF (code source, `TextEditorSpelling.cs`) : sans sélection, le caractère du côté où penche le curseur (`LogicalDirection`), ou l'autre côté s'il n'y a rien ou un blanc ; avec une sélection, le premier caractère non blanc de la sélection.
  - **Souris** : au clic droit hors de la sélection, WPF place lui-même le curseur sous le pointeur (gestionnaire de classe, exécuté avant celui de l'application, même avec un menu personnalisé). La même détection sert donc à la souris et au clavier (touche Applications, Maj+F10).
  - **Confirmé par l'utilisateur** (2026-11-16) : les suggestions sont proposées du curseur sur la première lettre jusqu'au curseur sur l'espace qui suit le mot, compris (sur l'espace, le caractère vers l'avant est un blanc : WPF regarde alors en arrière, la dernière lettre du mot).
- Si une faute est trouvée, on insère en tête, comme le menu natif : l'item **Suggestions**, dont les sous-items sont les suggestions (commande `EditingCommands.CorrectSpellingError`), ou « (aucune suggestion) » désactivé s'il n'y en a pas ; puis **Ignorer tout** (`EditingCommands.IgnoreSpellingError`) ; puis un séparateur. Les commandes retrouvent la faute par la même méthode : ce qui est corrigé est ce qui est proposé.
- Les suggestions n'ont pas de touche d'accès (le `_` est doublé). Touches d'accès du menu : S (Suggestions), I (Ignorer tout), `o` (Correcteur orthographique), F (Faute suivante), T (Tab insère une tabulation), R (Retour à la ligne automatique), L (Largeur de page : F, t, L), C (Correctif : e, D) en FR ; S, I, `c`, N, T, W, h (Page width : W, N, i), F (Fixes : x, I) en EN.
- Le correcteur utilise la langue de l'élément (`Language`, fr-FR ou en-US) et le dictionnaire Windows correspondant. **À vérifier** : le changement de langue de l'application est-il pris en compte sans redécocher la case ?

#### 2.7.2 Faute suivante (F7)

- Disponible dans les onglets **TextBox** et **RichText**, seulement si *Correcteur orthographique* est coché. L'item est désactivé (`IsEnabled`) quand le correcteur est décoché ; la touche **F7** (interceptée au niveau de la fenêtre, `PreviewKeyDown`) n'a alors aucun effet. F7 agit sur la zone de texte de l'onglet actif, même si le focus est ailleurs dans l'onglet (par exemple sur l'en-tête).
- Recherche à partir de la **fin de la sélection** (ou du curseur s'il n'y a pas de sélection), vers l'avant (`GetNextSpellingErrorCharacterIndex` pour le `TextBox`, `GetNextSpellingErrorPosition` pour le `RichTextBox`). Au bout du texte, la recherche **reprend au début** ; si le texte ne contient aucune faute, un bip système retentit et rien ne bouge.
- Le mot trouvé est **sélectionné** (longueur : `GetSpellingErrorLength` / `GetSpellingErrorRange`), la zone de texte reçoit le focus et fait défiler le texte pour le montrer. La sélection permet à F7 de passer au mot suivant, et de corriger en tapant par-dessus ou par le menu contextuel (2.7.1).
- La barre d'état (position) se met à jour comme à chaque déplacement du curseur.

#### 2.7.3 Retour à la ligne automatique (TextBox seulement)

- Le menu étant partagé, l'item est **masqué** (`Visibility="Collapsed"`) quand le menu s'ouvre depuis le `RichTextBox`, et visible depuis le `TextBox` (`ContextMenuOpening`, avant tout autre traitement).
- Pourquoi pas le `RichTextBox` : il affiche un `FlowDocument`, fait de paragraphes que WPF recompose toujours selon la largeur disponible ; aucun réglage natif ne coupe ce retour (pas de `TextWrapping`). Le couper demanderait un artifice (largeur de page géante), contraire au but de l'application, qui teste le comportement natif (décision de l'utilisateur, 2026-11-16).
- Les gestionnaires `Checked` / `Unchecked` sont branchés dans le constructeur : l'item est coché dans le XAML.

#### 2.7.4 Largeur de page (RichTextBox seulement)

- Pendant de 2.7.3 : le sous-menu est **masqué** quand le menu s'ouvre depuis le `TextBox`, visible depuis le `RichTextBox`.
- Le texte revient toujours à la ligne (comportement natif), mais à la largeur choisie : propriété native `FlowDocument.PageWidth`, sur le document **existant** (il n'est jamais remplacé, constat de `CLAUDE.md`).

| Choix | `PageWidth` | Barre de défilement horizontale | But |
|---|---|---|---|
| **Fenêtre** (démarrage) | non définie (`ClearValue`) : largeur de la zone | native | Comportement natif. |
| **Étroite (300 pixels)** | 300 | native | Lignes du texte coupées au même endroit quelle que soit la taille de la fenêtre (essais reproductibles). |
| **Large (2 000 pixels)** | 2 000 | `Auto` | Page plus large que la zone : défilement horizontal, ligne plus large que la zone. |

- Choix exclusif : cocher un item décoche les autres ; décocher l'item coché le recoche (il en faut toujours un). Gestionnaires sur `Checked` / `Unchecked` (constat de `CLAUDE.md` sur le `TogglePattern`), branchés dans le constructeur (**Fenêtre** est coché dans le XAML).

#### 2.7.5 Correctif (RichTextBox seulement)

- Comme 2.7.4 : le sous-menu (`MnuCtxCorrectif`) est **masqué** quand le menu s'ouvre depuis le `TextBox`.
- **Améliorer la lecture du texte enrichi** (`MnuCtxLectureRiche`) : case à cocher, **le même réglage** que l'item du menu principal *Correctif* (`Correctifs.md`), qui reste en place. Les deux items sont liés dans les deux sens (liaison `IsChecked` bidirectionnelle) : cocher ou décocher l'un change l'autre, et le correctif est appliqué une seule fois, par l'item du menu principal. Échec d'application : les deux sont décochés. **Confirmé** par l'utilisateur (2026-11-18) : la coche se propage entre les deux items.
- **Document interactif** (`MnuDocInteractif`, ancien *Activer les liens*, déplacé dans ce sous-menu le 2026-11-18) : case à cocher, **cochée au démarrage** (demande de l'utilisateur, 2026-11-18 ; décochée avant) : `RichTextBox.IsDocumentEnabled`. Renommé à la demande de l'utilisateur : la propriété active **tout** le contenu du document (liens, contrôles incorporés), pas seulement les liens. Décochée (natif) : un lien du document est du texte inerte, et tout le contenu est « désactivé » pour UIA. Cochée : le lien s'active (`RichText.md`, 2c). Ce n'est pas un correctif mais un réglage natif ; il est rangé ici parce qu'il change ce que NVDA annonce (« non disponible »). Agit sur `Checked` / `Unchecked`, branchés dans le constructeur (l'item est coché dans le XAML) ; l'état est appliqué au `RichTextBox` par `Brancher`.

#### À propos de « Ignorer tout »

« Ignorer tout » (`EditingCommands.IgnoreSpellingError`) demande à la zone de texte d'**ignorer toutes les occurrences de ce mot** : le soulignement disparaît sur chacune. Rien n'est ajouté au dictionnaire ni enregistré : l'effet dure le temps de la session de la zone de texte. Il n'a pas d'équivalent « ignorer une seule fois ».

## 3. Zone d'onglets

`TabControl` qui occupe tout l'espace entre le menu et la barre d'état. Sept onglets, dans cet ordre :

| Onglet | Contenu | Spécification |
|---|---|---|
| **TextBox** | `TextBox` multiligne, plein onglet | `TextBox.md` |
| **RichText** | Barre de mise en forme (`ToolBar`) et `RichTextBox` | `RichText.md` |
| **Usuels** | Formulaire de connexion (plusieurs contrôles) | `Usuels.md` |
| **Choix** | Contrôles de choix simple et multiple | `Choix.md` |
| **TreeView** | Deux arbres (« Dossiers », « Dossiers à cocher » avec une case par nœud) | `TreeView.md` |
| **DataGrid** | Grille de commandes (une colonne par type courant) | `DataGrid.md` |
| **Annonce** | Envoi d'un texte au lecteur d'écran (notification UIA, région vivante, focus) | `Annonce.md` |

- Le nom accessible de chaque contrôle est fixé par `AutomationProperties.Name`. Un `Label` avec `Target` ne suffit pas :
  le relevé UIA a montré qu'il ne produit ni nom ni `LabeledBy`, en .NET Framework 4.8 comme en .NET 10.
- Au démarrage, l'onglet **TextBox** est sélectionné et **son en-tête reçoit le focus** (pas l'éditeur) : Tab mène ensuite à l'éditeur.
- Changer d'onglet ne déplace **pas** le focus automatiquement (comportement natif de WPF) : sinon la navigation aux flèches dans les en-têtes serait coupée. On atteint l'éditeur avec Tab.
- Les en-têtes d'onglet sont atteignables au clavier (Ctrl+Tab / Ctrl+Maj+Tab, flèches quand l'en-tête a le focus).

## 4. Barre d'état

`StatusBar` tout en bas, sous la zone d'onglets, avec deux champs de texte :

| Champ | Contenu |
|---|---|
| **1 — Position** | Dans le `TextBox` ou le `RichTextBox` des onglets du même nom : `Ligne {0}, colonne {1}` (numérotation à partir de 1). Dans la grille de l'onglet DataGrid : `{ligne}, {colonne}` (nom de la ligne et en-tête de la colonne en cours, `DataGrid.md`, section 9). Ailleurs, y compris dans les champs des onglets Usuels, Choix, TreeView et Annonce : vide. |
| **2 — Type de contrôle** | Nom du type WPF de l'élément qui a le focus clavier (`TextBox`, `RichTextBox`, `TabItem`, `MenuItem`, `ComboBox`, `PasswordBox`, `CheckBox`, `Button`, `RadioButton`, `ListBoxItem`, `TreeViewItem`, `DataGridCell`…). Nom du type non traduit. |

- Mise à jour à chaque déplacement du curseur (`SelectionChanged`) et à chaque changement de focus (`GotKeyboardFocus`).
- Le `StatusBarItem` n'est pas exposé en UI Automation (constaté sur .NET 10) : un nom posé dessus serait perdu. Le nom accessible est donc posé sur le `TextBlock` du champ, sous la forme `<nom> : <valeur>` (par exemple « Position du curseur : Ligne 2, colonne 4 »). Le texte visible ne contient que la valeur. Noms dans la table des textes de `Specs.md`.
- **À vérifier avec NVDA** : NVDA+Fin lit-il bien nom et valeur ? Sans correctif, NVDA ne trouve pas la barre d'état : voir *Correctif > Corriger la barre d'état* (`Correctifs.md`).
- Pas de région vivante (`LiveSetting`) : la position change à chaque frappe, NVDA répéterait en continu. L'utilisateur lit la barre d'état à la demande (NVDA+Fin).
- Calcul de la ligne et de la colonne : voir `TextBox.md` et `RichText.md`. La ligne comptée est toujours la ligne affichée (comportement natif, celui de la lecture aux flèches). Dans le `TextBox`, avec *Retour à la ligne automatique* décoché (menu contextuel, 2.7.3), ligne affichée et ligne du texte coïncident.

## 5. Textes

Textes FR/EN de la fenêtre, des menus (y compris les menus contextuels des zones de texte) et de la barre d'état. Ceux de chaque onglet sont dans le fichier de l'onglet, section « Textes ».

| Élément | FR | EN |
|---|---|---|
| Menu Fichier | _Fichier | _File |
| Ouvrir | _Ouvrir... | _Open... |
| Enregistrer | _Enregistrer... | _Save... |
| Choix d'un dossier | Choix d'un _dossier... | Choose a _folder... |
| Toujours grisée (item de vérification) | _Toujours grisée | _Always grayed |
| Toujours grisée : message si l'item réagissait (ne doit jamais paraître) | « Toujours grisée » a réagi : le correctif des items indisponibles ne fonctionne pas. | "Always grayed" reacted: the unavailable menu items fix does not work. |
| Filtres d'Ouvrir | Texte et Markdown (\*.txt;\*.md) / Texte (\*.txt) / Markdown (\*.md) | Text and Markdown (\*.txt;\*.md) / Text (\*.txt) / Markdown (\*.md) |
| Filtres d'Enregistrer | Texte (\*.txt) / Markdown (\*.md) | Text (\*.txt) / Markdown (\*.md) |
| Dossier choisi (titre / texte du message) | Choix d'un dossier / Dossier de départ d'Ouvrir et d'Enregistrer :\n{0} | Choose a folder / Starting folder for Open and Save:\n{0} |
| Échec d'enregistrement (message) | Impossible d'enregistrer le fichier : {0}\n{1} | Cannot save file: {0}\n{1} |
| Langue | La_ngue | Lan_guage |
| FR / EN | FR / EN | FR / EN |
| Quitter | _Quitter | E_xit |
| Menu Édition | _Édition | _Edit |
| Annuler / Rétablir | Annuler / Rétablir | Undo / Redo |
| Couper / Copier / Coller | Couper / Copier / Coller | Cut / Copy / Paste |
| Tout sélectionner | Tout sélectionner | Select all |
| Menu Correctif | _Correctif | _Fixes |
| Corriger les lignes vides | Corriger les _lignes vides | Fix _blank lines |
| Corriger la barre d'état | Corriger la _barre d'état | Fix _status bar |
| Corriger le calendrier | Corriger le _calendrier | Fix the _calendar |
| Corriger la barre d'outils | Corriger la barre d'_outils | Fix the _toolbar |
| Atteindre les items indisponibles | Atteindre les items _indisponibles | Reach _unavailable menu items |
| Annoncer les raccourcis des menus | Annoncer les _raccourcis des menus | Report menu s_hortcuts |
| Améliorer la lecture du texte enrichi | Améliorer la lecture du texte _enrichi | Improve rich te_xt reading |
| Annoncer les touches d'accès avec Alt | Annoncer les touches d'_accès avec Alt | Report _access keys with Alt |
| Activer tous les correctifs | Activer _tous les correctifs | _Enable all fixes |
| Désactiver tous les correctifs | _Désactiver tous les correctifs | _Disable all fixes |
| Échec d'un correctif (message) | Le correctif n'a pas pu être appliqué : {0} | The fix could not be applied: {0} |
| Menu contextuel des zones de texte : correcteur (à cocher) | Correcteur _orthographique | Spell _checker |
| Menu contextuel des zones de texte : faute suivante | _Faute suivante (F7) | _Next error (F7) |
| Menu contextuel des zones de texte : tabulation (à cocher) | _Tab insère une tabulation | _Tab inserts a tab character |
| Menu contextuel des zones de texte : suggestions (sous-menu) | _Suggestions | _Suggestions |
| Menu contextuel des zones de texte : ignorer | _Ignorer tout | _Ignore all |
| Menu contextuel des zones de texte : sans suggestion | (aucune suggestion) | (no suggestions) |
| Menu Aide | _Aide | _Help |
| À propos | À _propos | _About |
| Onglets | TextBox, RichText, Usuels, Choix, TreeView, DataGrid, Annonce | TextBox, RichText, Usuels, Choix, TreeView, DataGrid, Annonce |
| Position (nom accessible) | Position du curseur | Caret position |
| Position (valeur) | Ligne {0}, colonne {1} | Line {0}, column {1} |
| Type (nom accessible) | Type de contrôle | Control type |
