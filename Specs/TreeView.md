# Onglet TreeView — AccessTest

Spécification de l'onglet **TreeView** : deux arbres (`TreeView`) côte à côte, alimentés par une même source de données de trois branches : « Dossiers », et « Dossiers à cocher », dont chaque nœud porte une case. Les deux ont un menu contextuel.
Voir `Fenetre.md` pour la zone d'onglets, le menu et la barre d'état, et `Specs.md` pour les règles d'accessibilité, `ChecklistNVDA.md` pour la checklist NVDA.

But : des tests **réalistes**. Les nœuds viennent d'une source de données (comme dans une vraie application), pas de `TreeViewItem` écrits à la main, et les trois branches couvrent les trois cas qui comptent pour un lecteur d'écran : plusieurs enfants, aucun enfant, une chaîne profonde.

## 1. Onglet

- En-tête (`x:Name="TabTreeView"`) : « TreeView », non traduit.
- Contenu : un `UserControl` à part, `OngletTreeView` (`Source/Onglets/OngletTreeView.xaml` et `.xaml.cs`), placé dans l'onglet sous le nom `ContenuTreeView`. Les `x:Name` ci-dessous sont ceux de ce contrôle ; ils restent les `AutomationId` des éléments. La fenêtre ne fait qu'appliquer la langue (`AppliquerLangue`).
- Placé **après** l'onglet Choix (cinquième onglet, suivi de DataGrid). Au démarrage, l'onglet sélectionné et le focus restent ceux de l'onglet TextBox.
- Pas d'autre contrôle que les deux arbres et leurs deux libellés.

## 2. Source de données

Une classe `Noeud` (fichier `Noeud.cs`), utilisée par les deux arbres :

| Propriété | Rôle |
|---|---|
| `Cle` | Clé du texte dans `Textes.cs` ; le nom affiché en dépend (traduction, voir 5). |
| `Nom` | Texte affiché, résolu à partir de `Cle` selon la langue. |
| `Enfants` | `ObservableCollection<Noeud>` : les enfants du nœud. |
| `EstDeveloppe` | État développé / réduit (`IsExpanded` du `TreeViewItem`, liaison à double sens). |
| `EstSelectionne` | État sélectionné (`IsSelected`, liaison à double sens). |
| `EstCoche` | État de la case à cocher (utilisé seulement par le second arbre). |

- `Noeud` implémente `INotifyPropertyChanged` et son `ToString()` renvoie `Nom` : **le nom accessible d'un nœud vient donc de `ToString()`**, comme pour les lignes du `ListView` (constat de `CLAUDE.md`). Aucun `AutomationProperties.Name` n'est posé sur les nœuds : c'est précisément ce que l'on veut observer.
- Les deux arbres ont **chacun leur propre copie** de la source : développer, sélectionner ou cocher dans l'un ne change rien dans l'autre.
- Contenu (trois nœuds racine, tous **réduits** au démarrage, aucun sélectionné, aucun coché) :

| Branche | Nœuds (FR / EN) | Cas testé |
|---|---|---|
| 1 | **Documents** / Documents, avec trois enfants : **Rapports** / Reports, **Factures** / Invoices, **Courriers** / Letters (aucun n'a d'enfant) | Plusieurs enfants : « 1 sur 3 », « 2 sur 3 »… |
| 2 | **Corbeille** / Recycle bin, sans enfant | Nœud racine sans enfant (pas de développement possible) |
| 3 | Chaîne de six niveaux, un seul enfant à chaque niveau : **Projets** / Projects, puis **AccessTest**, **Net10**, **Specs**, **Annexes** / Appendices, **Archives** / Archives (le dernier est une feuille) | Profondeur : le niveau est-il annoncé ? |

Les noms propres (AccessTest, Net10, Specs) ne sont pas traduits.

## 3. Disposition

Une grille de deux colonnes de largeur égale (`*` / `*`) et de deux lignes : le libellé (hauteur automatique), puis l'arbre, qui occupe **toute la hauteur restante**. Pas de `ScrollViewer` autour : chaque `TreeView` défile seul quand ses nœuds débordent.

| Colonne | Libellé | Contrôle | `x:Name` du contrôle |
|---|---|---|---|
| Gauche | Dossiers | `TreeView` lié à la source, avec un menu contextuel (4) | `ArbMenu`, menu `CtxArbre` |
| Droite | Dossiers à cocher | `CorrectifArbreCases` (`TreeView` dérivé, voir 5) lié à la source, une case à cocher par nœud, avec un menu contextuel (5) | `ArbCases`, menu `CtxCases` |

Libellés : `LblArbMenu`, `LblArbCases`. Les deux arbres sont **liés** par `ItemsSource` à leur copie de la source (`Enfants` des racines) et utilisent un `HierarchicalDataTemplate` (`ItemsSource="{Binding Enfants}"`). Un `DataTemplate` n'est pas un `ControlTemplate` : la règle 6 de `Specs.md` reste respectée. Le style des `TreeViewItem` (`ItemContainerStyle`) ne fait que lier `IsExpanded` et `IsSelected` à `EstDeveloppe` et `EstSelectionne`.

## 4. Premier arbre : menu contextuel

`CtxArbre`, posé sur le `TreeView` : clic droit, touche Applications ou Maj+F10. Quatre items, qui agissent sur **cet arbre** :

| Item | `x:Name` | Comportement |
|---|---|---|
| **Tout développer** | `MnuArbDevelopper` | `EstDeveloppe = true` sur tous les nœuds (récursif). |
| **Tout réduire** | `MnuArbReduire` | `EstDeveloppe = false` sur tous les nœuds. Si un nœud sélectionné se retrouve caché, **ne rien faire de plus** : c'est le comportement natif à observer (où est le focus ?). |
| **Ajouter un nœud** | `MnuArbAjouter` | Ajoute un enfant « Nouveau nœud *n* » (*n* = 1, 2, 3… en ordre d'ajout) au nœud sélectionné ; s'il n'y en a pas, un nouveau nœud racine. Le parent est développé, le nouveau nœud est sélectionné et reçoit le focus. Sur « Corbeille », le nœud devient donc un parent. |
| **Sélectionner le plus profond** | `MnuArbProfond` | Développe les ancêtres du premier nœud du niveau le plus profond (« Archives » tant qu'on n'a rien ajouté), le sélectionne et lui donne le focus clavier. Teste la sélection par code. |

- Le menu agit sur l'arbre entier, sauf « Ajouter un nœud », qui vise le nœud sélectionné. Le clic droit ne change pas la sélection (comportement natif de WPF, à vérifier).
- Pas de case à cocher dans ce menu : aucun item n'a d'état. Les items sont des commandes ordinaires (`Click`).
- Les gestionnaires sont **communs aux deux arbres** : l'arbre visé est celui dont le menu contient l'item. Le second menu (5) reprend donc les mêmes quatre items.
- Touches d'accès (voir la section « Textes » en fin de fichier) : D, R, A, S en FR ; A, C, N, S en EN.

## 5. Second arbre : cases à cocher

- Le modèle de chaque nœud (`HierarchicalDataTemplate`) est **une seule `CheckBox`** dont le texte est `Nom` et dont `IsChecked` est lié à `EstCoche` (`bool?`, double sens). `IsThreeState="False"` : l'utilisateur ne choisit que coché / non coché ; l'état indéterminé ne vient que de la propagation (5.1).
- **Propagation** : désactivée au démarrage (cases indépendantes) ; item **Propager aux enfants** du menu contextuel, voir 5.1.
- **Menu contextuel** `CtxCases`, posé sur le `TreeView` : les quatre items du premier arbre (`MnuCasDevelopper`, `MnuCasReduire`, `MnuCasAjouter`, `MnuCasProfond`, mêmes gestionnaires, voir 4), un séparateur, puis deux items à cocher : **Corriger les cases** (`MnuCasCorriger`), **coché au démarrage**, et **Propager aux enfants** (`MnuCasPropager`), **décoché au démarrage**. Touches d'accès : C et P en FR, F et P en EN.
- **Constat (comportement natif)**, fait par l'utilisateur avec NVDA le 2026-11-15, case décochée dans le menu : la `CheckBox` de chaque nœud est un arrêt de focus à part. Tous nœuds réduits, sur « Documents », il faut **quatre** flèches Bas pour arriver en bas, et NVDA dit « Corbeille », « Corbeille, case à cocher », « Projets », « Projets, case à cocher » ; Espace ne fait entendre aucun changement. La lecture n'est pas utilisable.
- **Corriger les cases** (cochée) : correctif réutilisable `Correctifs/CorrectifArbreCases.cs` (espace de noms `CorrectifsWpf`, sans dépendance), d'après la solution publiée par Microsoft pour Accessibility Insights (recherche et sources : `Docs/Constats/Arbre-Grille-Calendrier.md`). Le second arbre est un `CorrectifArbreCases`, `TreeView` dérivé dont les nœuds sont des `NoeudACocher` (`TreeViewItem` dérivé), quel que soit l'état de l'item ; seul le comportement dépend de `CorrectifArbreCases.Actif`, lu à chaque fois.
  - La `CheckBox` n'est plus focalisable (`Focusable` et `IsTabStop` à faux, par liaison à l'objet commun `CorrectionCases` de l'application) : un seul arrêt par nœud.
  - Le nœud a une propriété `IsChecked`, liée à `EstCoche` (style des nœuds) comme la case : les deux restent synchronisés.
  - **Motif UIA `Toggle`** sur le nœud : son peer de donnée (`TreeViewDataItemAutomationPeer` dérivé, à tous les niveaux, racines comprises) implémente `IToggleProvider` (`ToggleState` lu sur `IsChecked`, `Toggle()` l'inverse). NVDA ajoute alors l'état « coché » / « non coché » à n'importe quel rôle (code source de NVDA, `_get_states`).
  - **Événement** `ToggleStateProperty` levé à **chaque** changement de `IsChecked` (Espace, clic sur la case, code), traduit par NVDA en `stateChange` : le changement devrait être annoncé. La version de Microsoft ne le levait que par `Toggle()`.
  - **Espace** sur le nœud qui a le focus inverse `IsChecked` (`OnKeyDown` du nœud).
  - Le **nom du nœud reste `ToString()`** : l'état n'est pas ajouté au nom.
  - Décochée : pas de motif `Toggle`, pas d'événement, Espace non géré, case focalisable : comportement natif.
- **Historique des essais** (utilisateur, NVDA, 2026-11-15) : 1) case non focalisable, Espace géré par l'application et état ajouté au nom du nœud (« Nom, coché ») : déplacements bons, mais **Espace muet** (NVDA n'annonce pas un changement de nom du focus) ; retiré à la demande de l'utilisateur. 2) Sans le nom d'état : **NVDA ne dit pas qu'il y a une case** (le `TreeViewItem` n'a pas de motif `Toggle`, la `CheckBox` enfant n'est pas lue). D'où le correctif ci-dessus.
- **Constat de l'utilisateur avec NVDA** (2026-11-15), correctif actif : **la case est lue en se déplaçant** d'un nœud à l'autre et **le changement d'état est annoncé après Espace**. Le second arbre est utilisable. Reste à relever : le changement fait à la souris (clic sur la case).

### 5.1 Propager aux enfants

Réponse à Q11 (utilisateur, 2026-11-16) : propagation essayée comme un réglage décochable, pour tester l'état intermédiaire avec un lecteur d'écran.

- **Décoché** (démarrage) : cases indépendantes. Les états déjà posés restent tels quels quand on décoche (un parent indéterminé le reste).
- **Coché** :
  - cocher ou décocher un nœud fait de même pour **tous ses descendants** ;
  - chaque parent prend l'état de ses enfants : **coché** s'ils le sont tous, **non coché** si aucun ne l'est, **indéterminé** (`null`) sinon, et ainsi de suite jusqu'à la racine ;
  - en cochant l'item, les parents sont recalculés une fois à partir des feuilles (les états deviennent cohérents) ;
  - un nœud ajouté (« Ajouter un nœud », non coché) recalcule son parent.
  - un parent qui n'a **qu'un enfant** prend toujours son état : sur la branche **Projets** (un seul enfant par niveau), il n'y a jamais d'état partiel. Pour le tester : **Documents** (trois enfants), en cochant un seul d'entre eux.
- La logique est dans le modèle (`Noeud.Propager`, statique : seul le second arbre utilise `EstCoche`), pas dans le correctif : `CorrectifArbreCases` expose déjà l'état indéterminé (`ToggleState.Indeterminate`) et lève l'événement à chaque changement de `IsChecked`, y compris pour les nœuds changés par propagation (s'ils sont générés, c'est-à-dire si leur parent est développé).
- **Constaté** (utilisateur, NVDA, 2026-11-16) : sur un parent partiel, NVDA annonce « semi-coché » (le code source de NVDA traduit `ToggleState_Indeterminate` en état `HALFCHECKED`). seul le changement du nœud qui a le focus est annoncé ; celui des enfants et des parents changés par propagation ne l'est pas. Jugé normal par l'utilisateur (NVDA n'annonce un changement d'état que pour le focus) : pas de correction.
- **Comportement natif à noter** : sur un nœud indéterminé, Espace (correctif actif) le coche ; un clic sur la case (`ToggleButton` sans trois états) le **décoche**.

## 6. Comportement commun

- Clavier natif de `TreeView` : Haut / Bas passent d'un nœud visible à l'autre ; Droite développe, puis descend au premier enfant ; Gauche replie, puis remonte au parent ; Début / Fin vont au premier et au dernier nœud visible. Pavé numérique `+` et `-` : développer et replier. `*` : **à vérifier** (non sûr que WPF l'implémente).
- Tab : un seul arrêt par arbre (`TabNavigation="Once"` natif du `TreeView`). Ordre : en-têtes d'onglet, `ArbMenu`, `ArbCases`.
- Au démarrage, aucun nœud n'est sélectionné : Tab arrive sur le premier nœud sans le sélectionner **(à relever)**.
- Frappe d'une lettre : saut au nœud dont le nom commence par cette lettre **(à vérifier)** ; `TextSearch` n'est pas configuré, c'est le comportement natif qui compte.
- Pas de virtualisation configurée (réglage par défaut du `TreeView`), les arbres étant petits.

## 7. Accessibilité

- Noms accessibles : `AutomationProperties.Name` sur chaque arbre (« Dossiers » et « Dossiers à cocher »). Les nœuds tirent leur nom de `ToString()` (voir 2), dans les deux arbres.
- Pas de `ControlTemplate` personnalisé (règle 6 de `Specs.md`). Exception assumée : le second arbre est un contrôle dérivé (`CorrectifArbreCases`), mais son comportement natif reste disponible en décochant « Corriger les cases ».
- Second arbre, correctif actif : nœuds avec motif `Toggle` (voir 5).
- Ordre de tabulation : voir 6.

## 8. Menu et barre d'état avec cet onglet

- **Barre d'état** : le champ 1 (position) est vide ; le champ 2 donne le type WPF de l'élément focalisé : `TreeViewItem` ou, si la case a pris le focus (comportement natif, « Corriger les cases » décoché), `CheckBox`.

## 9. Lecture par un lecteur d'écran

- Types UIA attendus : `Tree` pour le `TreeView`, `TreeItem` pour chaque `TreeViewItem` (motifs `ExpandCollapse` et `SelectionItem`) ; dans le second arbre, une `CheckBox` (motif `Toggle`) en enfant du nœud.
- `ExpandCollapseState` attendu : `Collapsed` / `Expanded` pour un nœud qui a des enfants, `LeafNode` pour les autres. Les enfants d'un nœud réduit ne sont pas dans l'arbre UIA (comme les sous-menus fermés).
- **À relever** : niveau et position (« 2 sur 3 ») annoncés ou non, nom d'un nœud sans `AutomationProperties.Name`, annonce d'un nœud devenu parent (« Corbeille » après « Ajouter un nœud »), focus après « Tout réduire ».
- Points de `ChecklistNVDA.md` qui concernent cet onglet : 6, 21, 22, 23.

## Textes

Textes FR/EN de l'onglet (`Textes.cs` les reprend ; le menu et la barre d'état sont dans `Fenetre.md`, section 5).

| Élément | FR | EN |
|---|---|---|
| TreeView : premier arbre (libellé et nom accessible) | Dossiers | Folders |
| TreeView : second arbre (libellé et nom accessible) | Dossiers à cocher | Folders with check boxes |
| TreeView : menu contextuel du second arbre, item 5 (à cocher) | _Corriger les cases | _Fix the check boxes |
| TreeView : second arbre, menu contextuel (à cocher) | _Propager aux enfants | _Propagate to children |
| TreeView : menu contextuel, item 1 | Tout _développer | Expand _all |
| TreeView : menu contextuel, item 2 | Tout _réduire | _Collapse all |
| TreeView : menu contextuel, item 3 | _Ajouter un nœud | Add a _node |
| TreeView : menu contextuel, item 4 | _Sélectionner le plus profond | _Select the deepest |
| TreeView : nœuds de la branche 1 | Documents (Rapports, Factures, Courriers) | Documents (Reports, Invoices, Letters) |
| TreeView : nœud de la branche 2 | Corbeille | Recycle bin |
| TreeView : nœuds de la branche 3 (un enfant à chaque niveau) | Projets, AccessTest, Net10, Specs, Annexes, Archives | Projects, AccessTest, Net10, Specs, Appendices, Archives |
| TreeView : nœud ajouté | Nouveau nœud {0} | New node {0} |
