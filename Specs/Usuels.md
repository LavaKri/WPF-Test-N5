# Onglet Usuels — AccessTest

Spécification de l'onglet **Usuels** : un formulaire de connexion qui réunit les contrôles les plus courants (liste déroulante, champ texte, mot de passe, case à cocher, bouton, lien, barre de progression, zone de résultat en lecture seule), avec des libellés à touche d'accès pour l'identifiant et le mot de passe.
Voir `Fenetre.md` pour la zone d'onglets, le menu et la barre d'état, et `Specs.md` pour les règles d'accessibilité, `ChecklistNVDA.md` pour la checklist NVDA.

## 1. Onglet

- En-tête (`x:Name="TabUsuels"`) : « Usuels », non traduit (l'onglet s'appelait « Simple » jusqu'au 2026-11-16).
- Contenu : un `UserControl` à part, `OngletUsuels` (`Source/Onglets/OngletUsuels.xaml` et `.xaml.cs`), placé dans l'onglet sous le nom `ContenuUsuels`. Les `x:Name` ci-dessous sont ceux de ce contrôle ; ils restent les `AutomationId` des éléments. La fenêtre ne fait qu'appliquer la langue (`AppliquerLangue`).
- Placé **après** l'onglet RichText (troisième onglet, suivi de Choix, TreeView et DataGrid). Au démarrage, l'onglet sélectionné et le focus restent ceux de l'onglet TextBox.
- Pas de validation, pas de vrai compte : le formulaire ne vérifie rien et n'affiche aucun message d'erreur.

## 2. Disposition

Une grille de deux colonnes de largeur égale (`*` / `*`), une ligne par entrée, de haut en bas. Aucune largeur maximale (décision de l'utilisateur, 2026-11-16) : les colonnes se partagent toute la largeur ; le texte du libellé est aligné à droite, donc collé au contrôle.

- **Colonne de gauche** : le libellé, texte **aligné à droite**, centré verticalement sur le contrôle. Un petit écart le sépare du contrôle. C'est un `TextBlock`, sauf pour l'identifiant et le mot de passe, où c'est un `Label` à touche d'accès (3.7) ; le `Label` reprend le même alignement, le même écart et un remplissage nul.
- **Colonne de droite** : le contrôle, qui occupe toute la largeur de la colonne (sauf la case à cocher, le bouton et le lien, qui gardent leur taille naturelle, alignés à gauche).

| # | Libellé (colonne de gauche) | Contrôle (colonne de droite) | `x:Name` du contrôle |
|---|---|---|---|
| 1 | Confirmation | `ComboBox` non éditable : Aucun, SMS, Email | `CmbConfirmation` |
| 2 | Contact de confirmation | `TextBox` d'une ligne | `TxtContact` |
| 3 | _Identifiant (`Label`) | `TextBox` d'une ligne | `TxtIdentifiant` |
| 4 | _Mot de passe (`Label`) | `PasswordBox` | `PwdMotDePasse` |
| 5 | *(vide)* | `CheckBox` « Rester connecté » | `ChkResterConnecte` |
| 6 | *(vide)* | `Button` « Se connecter » | `BtnSeConnecter` |
| 7 | *(vide)* | `TextBlock` qui contient le `Hyperlink` « Mot de passe oublié ? » | `Hyperlink` : `LnkMotDePasseOublie` |
| 8 | Progression | `ProgressBar` suivie de son pourcentage en chiffres (`TextBlock`) | `PrgProgression`, `TblPourcentage` |
| 9 | Résultat | `TextBox` multiligne en lecture seule | `TxtResultat` |

Les libellés ont pour `x:Name` `LblConfirmation`, `LblContact`, `LblIdentifiant`, `LblMotDePasse`, `LblProgression` et `LblResultat`. Les trois entrées de la liste sont des `ComboBoxItem` : `ItmAucun`, `ItmSms`, `ItmEmail`. Le `TextBlock` du lien s'appelle `TblMotDePasseOublie` et le texte du lien est un `Run` : `RunMotDePasseOublie`.
Les textes FR/EN sont dans la section « Textes » en fin de fichier.

## 3. Comportement

### 3.1 Confirmation et contact

- Au démarrage, « Aucun » est sélectionné et la ligne 2 (`TxtContact`) est **désactivée** (`IsEnabled="False"`).
- « SMS » ou « Email » **active** la ligne 2 ; revenir à « Aucun » la désactive. Le texte déjà saisi est conservé.
- Le libellé et le nom accessible de la ligne 2 ne changent pas selon le choix **(hypothèse)** : « Contact de confirmation ».

### 3.2 Champs obligatoires

Identifiant et mot de passe sont déclarés obligatoires pour le lecteur d'écran : `AutomationProperties.IsRequiredForForm="True"` sur `TxtIdentifiant` et `PwdMotDePasse`. Rien n'est vérifié (voir plus haut) : c'est une propriété d'accessibilité seulement.

### 3.3 Se connecter et progression

Un clic sur **Se connecter** (souris, Entrée ou Espace sur le bouton) :

1. donne le focus à `TxtResultat`, **puis** désactive le bouton (dans cet ordre : désactiver un contrôle qui a le focus le lui fait perdre, et le lecteur d'écran ne saurait plus où il est) ;
2. remplace le texte de `TxtResultat` par « En cours de connexion, veuillez patienter ... » ;
3. lance la progression : `PrgProgression` passe de **0 à 100** (`Minimum="0"`, `Maximum="100"`) en **5 secondes**, par pas entiers d'1 %.

À 100 % :

1. le bouton est **réactivé** (il ne reprend pas le focus : celui-ci reste sur `TxtResultat`) ;
2. `TxtResultat` reçoit la liste des champs du formulaire, **un par ligne**, sous la forme `Nom : valeur` (voir 3.4). Le curseur est placé au début du texte.

- Le bouton étant désactivé pendant la progression, un second clic est impossible : la progression ne repart pas en cours de route.
- **Pourcentage en chiffres** : `TblPourcentage`, à droite de la barre (même ligne), affiche la valeur de la barre sous la forme « 42 % » (EN : « 42% »). Il vaut « 0 % » au démarrage, suit chaque pas d'1 % et reste à « 100 % » à la fin. Un `ProgressBar` WPF n'affiche aucun texte et son peer UIA n'expose que `RangeValue` ; JAWS ne lit pas cette valeur de lui-même (constat de l'utilisateur, 2026-11-19), d'où ce texte.
  - Item décoché (menu contextuel du bouton, 3.10) : ce n'est **pas** une région vivante et il ne lève aucune notification UIA, rien n'est annoncé à chaque pas (décision de l'utilisateur). L'item est **coché au démarrage**. Il se lit à la demande (curseur virtuel de JAWS, navigation par objets de NVDA). L'item *ProgressBar avec région vivante* du menu contextuel du bouton le change en région vivante (3.10).
  - Pas de nom accessible posé (le texte visible fait office de nom), pas d'arrêt de tabulation.
- La barre reste pleine à la fin. Aucun message n'est affiché ni annoncé à la fin, en dehors du nouveau contenu de `TxtResultat`.
- Pas de bouton par défaut (`IsDefault`) ni d'Annuler : Entrée dans un champ ne lance rien.

### 3.4 Contenu du résultat

Une ligne par champ, dans l'ordre du formulaire, avec le libellé de la langue en cours :

| Ligne | Nom | Valeur |
|---|---|---|
| 1 | Confirmation | Texte de l'entrée choisie (Aucun, SMS, Email) |
| 2 | Contact de confirmation | Texte du champ (vide si rien n'a été saisi, même si la ligne est désactivée) |
| 3 | Identifiant | Texte du champ |
| 4 | Mot de passe | Mot de passe **en clair** (application d'essai, aucun vrai compte) |
| 5 | Rester connecté | Oui / Non (« Yes » / « No » en EN) |

Un champ vide donne « Nom : » suivi de rien. Le résultat n'est pas retraduit si la langue change ensuite (il est recalculé à la prochaine connexion).

### 3.5 Au démarrage

`TxtResultat` est vide, le bouton est actif.

### 3.6 Menu contextuel du résultat

`TxtResultat` a son propre menu contextuel (`CtxResultat`, touche Applications, Maj+F10 ou clic droit) :

| Item | Comportement |
|---|---|
| **Rendre visible le point d'insertion** (`MnuResCurseur`) | Case à cocher, **cochée au démarrage** : `IsReadOnlyCaretVisible="True"`. Décochée : valeur par défaut de WPF (point d'insertion invisible, les flèches le déplacent quand même). |

- La case agit sur `Checked` / `Unchecked` et non sur `Click` (constat de `CLAUDE.md`).
- Ce menu **remplace** le menu contextuel natif du champ (Copier, Sélectionner tout…) ; ces actions restent dans le menu Édition et aux raccourcis clavier.
- Texte et touche d'accès  : voir la section « Textes » en fin de fichier.

### 3.7 Touches d'accès de l'identifiant et du mot de passe

Les libellés des lignes 3 et 4 sont des `Label` dont le texte porte une touche d'accès (`_Identifiant`, `_Mot de passe` ; EN `_User name`, `_Password`) et dont `Target` désigne le champ (`TxtIdentifiant`, `PwdMotDePasse`, par liaison `ElementName`).

- Rôle dans l'application : Alt+I et Alt+M (EN : Alt+U et Alt+P) donnent le focus directement au champ.
- Le nom accessible du champ reste posé par `AutomationProperties.Name`, sans le `_` (« Identifiant »). Pas de `AutomationProperties.AccessKey` sur le champ **pour l'instant** : voir « À vérifier » ci-dessous.
- Aucun conflit avec les touches d'accès du menu (F, É, C, A en FR ; F, E, F, H en EN).
- À vérifier :
  - si le raccourci fonctionne ;
  - si NVDA l'annonce sur le champ. On s'attendait à ce que non. **Constat** (utilisateur, 2026-11-18, réglage NVDA *Annoncer les raccourcis clavier des objets* coché) : **oui**, NVDA dit la lettre seule en fin d'annonce (« i » pour Identifiant, « m » pour Mot de passe), facile à manquer. WPF expose la propriété UIA `AccessKey` = « I » / « M » sur le champ (déduite du `Label`, relevé UIA du contrôle compilé), sans « Alt+ » (Windows écrit « Alt + L'espace » pour le menu système). `AutomationProperties.AccessKey` posée sur le champ est ignorée par WPF (.NET 10) : la propriété UIA devient vide (essai en console) ;
  - **corrigé** par *Correctif > Annoncer les touches d'accès avec Alt* (`Correctifs.md`), coché au démarrage : « Alt+I », « Alt+M » (EN : Alt+U, Alt+P) ;
  - autre constat : le focus sur la case « Rester connecté » (ou un autre contrôle qui ne prend pas de texte), la lettre **I seule, sans Alt**, ramène le focus sur Identifiant. C'est le comportement normal des touches d'accès (WPF comme les boîtes de dialogue Win32) : sans Alt, elles agissent quand le contrôle focalisé ne prend pas de saisie de texte ;
  - si NVDA lit le `Label` deux fois (avec et sans `_`, `Docs/Notes.md`, section 5) ou une seule fois, en navigation par objets.

### 3.8 Lien « Mot de passe oublié ? »

Un `Hyperlink` dans un `TextBlock`, sous le bouton (ligne 7), aligné à gauche, sans `NavigateUri`.

- Rôle dans l'application : activé (souris, Entrée), il remplace le texte de `TxtResultat` par « Lien activé » (EN : « Link activated »). Aucune navigation réelle, aucune boîte de dialogue. Il donne aussi le focus à `TxtResultat` (comme « Se connecter », 3.3), pour que le lecteur d'écran lise le message ; l'ordre est : texte, puis focus (décision de l'utilisateur, 2026-11-18 : le lien activé seul ne faisait rien entendre).
- Le lien reste actif pendant la progression ; si la progression est en cours, la liste des champs remplace « Lien activé » à 100 %.
- **Constats** (utilisateur, 2026-11-18, .NET 10 et 4.8) : rôle « lien » annoncé ; arrêt de tabulation entre « Se connecter » et « Résultat » ; Entrée l'active, Espace non ; clic à la souris OK ; le nom est **lu deux fois** (« Mot de passe oublié, mot de passe oublié, lien »), probablement à cause du `TextBlock` ancêtre qui porte le même nom. **Corrigé** par l'item *Lire les liens une seule fois* du menu contextuel du lien (3.9, `Correctifs.md`), coché au démarrage ; décoché, on retrouve le comportement natif du lien WPF.
- À vérifier (historique) :
  - le rôle « lien » annoncé par NVDA ;
  - s'il est un arrêt de tabulation (c'est un `ContentElement`, pas un contrôle) ;
  - si Entrée l'active ;
  - le motif UIA `Invoke`.

### 3.9 Menu contextuel du lien

Le lien a son propre menu contextuel (`CtxMotDePasseOublie`, posé sur le `Hyperlink` ; touche Applications, Maj+F10 ou clic droit, focus sur le lien) :

| Item | Comportement |
|---|---|
| **Lire les liens une seule fois** (`MnuLnkLienSeul`) | Case à cocher, **cochée au démarrage** : le correctif des liens seuls dans un `TextBlock` est actif (`Correctifs.md`). Décochée : comportement natif (nom du lien lu deux fois). |

- Demande de l'utilisateur (2026-11-18) : l'item est ici, près du lien qu'il concerne, et **plus** dans le menu principal *Correctif*. Le correctif reste global (tous les `TextBlock` du processus qui ne contiennent qu'un lien).
- Appliqué au démarrage par le constructeur de l'onglet ; échec d'application : `MessageBox` d'erreur et case décochée, comme les correctifs du menu principal.
- La case agit sur `Checked` / `Unchecked` et non sur `Click` (constat de `CLAUDE.md`), branchés dans le constructeur (l'item est coché dans le XAML).
- **Confirmé** (utilisateur, NVDA, .NET 10 et 4.8, 2026-11-18) : la touche Applications ouvre le menu quand le focus est sur le lien (un `Hyperlink` est un `ContentElement`, pas un contrôle), et l'item agit.

### 3.10 Menu contextuel du bouton « Se connecter »

Le bouton a son propre menu contextuel (`CtxSeConnecter` ; touche Applications, Maj+F10 ou clic droit, focus sur le bouton) :

| Item | Comportement |
|---|---|
| **ProgressBar avec région vivante** (`MnuBtnRegion`) | Case à cocher, **cochée au démarrage** (décision de l'utilisateur, 2026-11-19). Cochée : `TblPourcentage` (3.3) devient une région vivante, `AutomationProperties.LiveSetting` = `Assertive` (une annonce **coupe la précédente** ; en `Polite`, elles feraient la queue), et `AutomationEvents.LiveRegionChanged` est levé sur son peer **au plus toutes les 500 ms**, plus une fois à 100 % (même mécanisme que la sortie « Région vivante » de `Annonce.md`). Le lecteur lit le nom de l'élément, soit son texte (« 42 % »). Le texte affiché, lui, suit chaque pas d'1 %. Décochée : `LiveSetting` effacé (`Off`), texte seul. |
| **ProgressBar tabulable** (`MnuBtnTabulable`) | Case à cocher, **décochée au démarrage** (comportement natif). Cochée : `PrgProgression.Focusable` = `true`, la barre devient un **arrêt de tabulation** et reçoit le focus au clavier : le lecteur d'écran lit son nom et sa valeur (`RangeValue`). Décochée : valeur native de WPF rétablie (`ClearValue`), non focalisable. |

- Demande de l'utilisateur (2026-11-19) : comparer, avec JAWS et NVDA, la lecture du pourcentage à la demande et celle d'une région vivante. Demandes du même jour : libellé avec « région vivante » (et non « partie vivante »), une annonce toutes les 500 ms au lieu de chaque pas, une annonce coupée par la suivante (`Assertive`).
- Libellé exact demandé par l'utilisateur : « ProgressBar avec région vivante » (touche d'accès P).
- La case agit sur `Checked` / `Unchecked` (constat de `CLAUDE.md`), branchés dans le constructeur.
- Le bouton est désactivé pendant la progression : le menu ne s'ouvre qu'avant ou après. On coche l'item avant de cliquer sur *Se connecter*.
- Une annonce toutes les 500 ms environ, soit 10 pas d'1 % : « 0 % », « 10 % », « 20 % », ... « 100 % ». **À vérifier** : ce que JAWS et NVDA disent réellement, notamment si l'annonce suivante coupe bien la précédente (`Assertive`) ou si la lecture est tronquée ou perdue. Comme dans `Annonce.md`, une région vivante levée dans un onglet non affiché n'est pas lue : l'onglet Usuels doit être affiché.

## 4. Accessibilité

- Un `Label` avec `Target` ne donne pas de nom au champ (constat de `CLAUDE.md`) : chaque contrôle porte son nom accessible (`AutomationProperties.Name`), identique au libellé de sa ligne (sans le `_` des deux `Label`). Le `CheckBox`, le `Button` et le `Hyperlink` tirent leur nom de leur texte visible.
- Les libellés ne sont pas des arrêts de tabulation. Ordre de tabulation attendu : liste, contact (s'il est actif), identifiant, mot de passe, case à cocher, bouton, lien (à vérifier, 3.8), résultat. La barre de progression n'est pas focalisable par défaut : elle ne s'atteint pas avec Tab, sauf si *ProgressBar tabulable* est coché (3.10) ; elle se place alors entre le lien et le résultat (ordre du formulaire). Le pourcentage (`TblPourcentage`) n'est jamais un arrêt.
- Nom accessible de `PrgProgression` : « Progression ». Valeur lue par le motif UIA `RangeValue` (0 à 100).
- `TxtResultat` : nom accessible « Résultat » / « Result ». Lecture seule (`IsReadOnly="True"`) avec le point d'insertion **visible** par défaut (`IsReadOnlyCaretVisible="True"` : sans lui, WPF déplace le point d'insertion aux flèches mais ne l'affiche pas, et le texte paraît figé ; réglage décochable, voir 3.6) ; **focalisable** : il reçoit le focus à la connexion et reste un arrêt de tabulation (après le bouton). Multiligne (`AcceptsReturn="True"`, `TextWrapping="Wrap"`, `MinLines="6"`, `VerticalScrollBarVisibility="Auto"`). Il n'est pas une région vivante : **à vérifier avec NVDA** si le nouveau texte (« En cours… », puis la liste) est annoncé alors que le focus y est déjà.
- Le bouton est désactivé pendant 5 s : NVDA l'annonce « indisponible » s'il est relu, et il n'est plus atteignable par Tab pendant ce temps.
- Pas de `ControlTemplate` personnalisé (règle 6 de `Specs.md`).

## 5. Menu et barre d'état avec cet onglet

- **Édition** : s'applique au contrôle focalisé, sans traitement particulier. Couper et Copier restent indisponibles sur le mot de passe (comportement natif de `PasswordBox`).
- **Barre d'état** : le champ 1 (position) est vide ; le champ 2 donne le type WPF du contrôle focalisé : `ComboBox`, `TextBox`, `PasswordBox`, `CheckBox`, `Button`, `Hyperlink` (si le lien reçoit le focus, 3.8).

## 6. Lecture par un lecteur d'écran

- Types de contrôle UIA attendus : `ComboBox`, `Edit` (×2), `Edit` avec l'indicateur de mot de passe, `CheckBox`, `Button`, `Hyperlink`, `ProgressBar`, `Edit` en lecture seule (résultat) ; `Text` pour les libellés et le pourcentage.
- À vérifier avec JAWS (et NVDA) : la lecture du pourcentage (`TblPourcentage`) à la demande, et si la valeur de la barre elle-même est lue.
- Points de `ChecklistNVDA.md` qui concernent cet onglet : 6, 8, 15, 16, 48, 49.

## Textes

Textes FR/EN de l'onglet (`Textes.cs` les reprend ; le menu et la barre d'état sont dans `Fenetre.md`, section 5).

| Élément | FR | EN |
|---|---|---|
| Usuels : liste (libellé et nom accessible) | Confirmation | Confirmation |
| Usuels : entrées de la liste | Aucun / SMS / Email | None / SMS / Email |
| Usuels : contact (libellé et nom accessible) | Contact de confirmation | Confirmation contact |
| Usuels : identifiant (nom accessible) | Identifiant | User name |
| Usuels : identifiant (libellé, touche d'accès) | _Identifiant | _User name |
| Usuels : mot de passe (nom accessible) | Mot de passe | Password |
| Usuels : mot de passe (libellé, touche d'accès) | _Mot de passe | _Password |
| Usuels : case à cocher | Rester connecté | Stay signed in |
| Usuels : bouton | Se connecter | Sign in |
| Usuels : lien (texte et nom accessible) | Mot de passe oublié ? | Forgot your password? |
| Usuels : texte de résultat à l'activation du lien | Lien activé | Link activated |
| Usuels : menu contextuel du lien (à cocher) | Lire les liens une seule _fois | Read links _once |
| Usuels : barre de progression (libellé et nom accessible) | Progression | Progress |
| Usuels : menu contextuel du bouton (à cocher) | _ProgressBar avec région vivante | _ProgressBar with live region |
| Usuels : menu contextuel du bouton, barre tabulable (à cocher) | ProgressBar _tabulable | ProgressBar with _tab stop |
| Usuels : pourcentage de la progression (`{0}` = 0 à 100) | {0} % | {0}% |
| Usuels : résultat (libellé et nom accessible) | Résultat | Result |
| Usuels : menu contextuel du résultat (à cocher) | _Rendre visible le point d'insertion | _Show the caret |
| Usuels : texte pendant la connexion | En cours de connexion, veuillez patienter ... | Signing in, please wait ... |
| Usuels : valeurs de la case « Rester connecté » dans le résultat | Oui / Non | Yes / No |
