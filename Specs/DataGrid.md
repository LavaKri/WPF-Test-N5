# Onglet DataGrid — AccessTest

Spécification de l'onglet **DataGrid** : une grille de commandes liée à une source de données, avec une colonne de chaque type courant, et un menu contextuel qui active deux corrections connues (tabulation, tri au clavier).
Voir `Fenetre.md` pour la zone d'onglets, le menu et la barre d'état, et `Specs.md` pour les règles d'accessibilité, `ChecklistNVDA.md` pour la checklist NVDA.

But : des tests **réalistes**. Les lignes viennent d'une source de données, chaque colonne couvre un cas qui compte pour un lecteur d'écran (texte, nombre, date, case, liste, cellule sans texte), et les deux corrections reprennent des défauts documentés par Microsoft (`Docs/Constats/Sources-Microsoft.md`).

## 1. Onglet

- En-tête (`x:Name="TabDataGrid"`) : « DataGrid », non traduit.
- Contenu : un `UserControl` à part, `OngletDataGrid` (`Source/Onglets/OngletDataGrid.xaml` et `.xaml.cs`), placé dans l'onglet sous le nom `ContenuDataGrid`. Les `x:Name` ci-dessous sont ceux de ce contrôle ; ils restent les `AutomationId` des éléments. La fenêtre applique la langue (`AppliquerLangue`) et lit la position de la cellule pour la barre d'état (`Position`, événement `PositionModifiee`).
- Placé **après** l'onglet TreeView (sixième onglet, suivi d'Annonce). Au démarrage, l'onglet sélectionné et le focus restent ceux de l'onglet TextBox.
- Pas d'autre contrôle que la grille et son libellé.

## 2. Source de données

Une classe `Commande` (fichier `Commande.cs`), une instance par ligne :

| Propriété | Type | Rôle |
|---|---|---|
| `Numero` | `int` | Numéro de commande, de 1001 à 1030. |
| `Client` | `string` | Nom du client (noms propres, non traduits). |
| `Date` | `DateTime` | Date de la commande. |
| `Montant` | `decimal` | Montant en euros. |
| `Payee` | `bool` | Commande payée ou non. |
| `Statut` | énumération `StatutCommande` | En attente, Expédiée, Livrée (textes traduits). |
| `Prioritaire` | `bool` | Commande prioritaire : signalée par une icône seule (5). |

- `Commande` implémente `INotifyPropertyChanged` ; son `ToString()` renvoie « Commande {Numero} » / « Order {Numero} » : c'est le nom accessible de la ligne (constat de `CLAUDE.md` sur `ListView` + `GridView`, à vérifier pour le `DataGrid`).
- **30 commandes**, toujours les mêmes (générées par une règle fixe, pas au hasard) : dix clients qui reviennent, dates réparties d'octobre à novembre 2026, montants variés (de quelques euros à plus de 1 000 €), payées une sur deux environ, statuts mélangés, quelques prioritaires. Trente lignes dépassent la hauteur de la fenêtre restaurée : la grille défile, la virtualisation joue.
- Une `ObservableCollection<Commande>` est la source de la grille. Une ligne ajoutée par la ligne vide (6) reçoit le numéro suivant.

## 3. Disposition

Comme l'onglet TreeView : une grille de deux lignes, le libellé (hauteur automatique) puis le `DataGrid`, qui occupe **toute la place restante** et défile seul.

| Élément | `x:Name` | Contenu |
|---|---|---|
| Libellé | `LblCommandes` | « Commandes » / « Orders » |
| Grille | `GrdCommandes` | `DataGrid` lié à la source, menu contextuel `CtxGrille` (7) |

## 4. Réglages de la grille

- `AutoGenerateColumns="False"` : colonnes déclarées (5).
- Sélection : `SelectionMode="Extended"`, `SelectionUnit="FullRow"` (valeurs par défaut de WPF).
- `CanUserAddRows="True"` (ligne vide d'ajout en bas), `CanUserDeleteRows="True"`, `CanUserSortColumns="True"`, `CanUserResizeColumns` et `CanUserReorderColumns` par défaut (souris seulement).
- Virtualisation par défaut (`EnableRowVirtualization="True"`) : les lignes hors de l'écran n'existent pas dans l'arbre UIA. C'est le comportement à observer.
- En-têtes de ligne et de colonne visibles (`HeadersVisibility="All"`, par défaut).
- Pas de `ControlTemplate` personnalisé (règle 6 de `Specs.md`). Un `DataTemplate` pour la colonne Priorité seulement.

## 5. Colonnes

| # | En-tête (FR / EN) | Type de colonne | Liaison | Ce qu'elle teste |
|---|---|---|---|---|
| 1 | N° / No. | `DataGridTextColumn`, lecture seule | `Numero` | Lecture simple d'un nombre. |
| 2 | Client / Customer | `DataGridTextColumn`, modifiable | `Client` | Mode édition (F2 ou frappe, Entrée valide, Échap annule), annonce de la zone de saisie. |
| 3 | Date / Date | `DataGridTextColumn`, lecture seule | `Date`, date courte dans la langue de l'application | Lecture d'une date, FR et EN. |
| 4 | Montant / Amount | `DataGridTextColumn`, lecture seule, aligné à droite | `Montant`, format `{0:N2} €` dans la langue de l'application | Lecture d'un nombre formaté. |
| 5 | Payée / Paid | `DataGridCheckBoxColumn` | `Payee` | Case dans une cellule. Défaut connu : il faut d'abord entrer en édition pour cocher. Est-elle annoncée, et Espace la coche-t-il ? |
| 6 | Statut / Status | `DataGridComboBoxColumn` | `Statut` | Liste dans une cellule : ouverture (F2, Alt+Bas, F4) et choix au clavier. |
| 7 | Priorité / Priority | `DataGridTemplateColumn` : un triangle (`Polygon`) visible si `Prioritaire`, rien sinon ; aucun texte | `Prioritaire` | Cellule sans texte : WPF lui fabrique un nom technique (« Élément : Commande 1007, index d'affichage de la colonne : 6 », constaté, section 10). Corrigé par le troisième item du menu (7) : nom « Prioritaire » ou « Normale ». |

Colonnes (`x:Name`) : `ColNumero`, `ColClient`, `ColDate`, `ColMontant`, `ColPayee`, `ColStatut`, `ColPriorite`. Les en-têtes, les textes des statuts et le format des dates et des montants suivent la langue de l'application (`Fenetre.md`, 2.6) ; au changement de langue, une édition en cours est validée et les lignes sont redessinées (`Items.Refresh`).

## 6. Comportement natif à observer

- **Tab** passe de cellule en cellule (piège de tabulation, `Docs/Constats/Sources-Microsoft.md`, contrôle `CustomDataGrid` de Microsoft) ; noter si et quand il sort de la grille.
- Flèches : cellule à cellule ; Début / Fin : première et dernière cellule de la ligne ; Ctrl+Début / Ctrl+Fin : première et dernière cellule de la grille ; PageHaut / PageBas : par page.
- **Tri** : à la souris seulement, les en-têtes de colonne n'étant pas focalisables ; l'ordre de tri n'est pas exposé en UIA.
- **Ligne vide d'ajout** : dernière ligne ; taper dans une cellule crée une commande, Entrée la valide. Noter ce que NVDA annonce dessus (nom de ligne attendu : un texte technique, `{NewItemPlaceholder}` ou équivalent, à vérifier).
- Suppr sur une ligne sélectionnée (hors édition) supprime la commande (`CanUserDeleteRows`).

## 7. Menu contextuel

`CtxGrille`, posé sur le `DataGrid` : clic droit, touche Applications ou Maj+F10. Quatre items à cocher, **cochés au démarrage** (comme les correctifs : décocher pour voir le comportement natif). Ils agissent sur `Checked` / `Unchecked`.

| Item (FR / EN) | `x:Name` | Coché | Décoché |
|---|---|---|---|
| Un seul arrêt de _tabulation / Single _tab stop | `MnuGrlUnArret` | Les cellules sont **un seul arrêt** : Tab depuis une cellule sort de la grille vers l'élément suivant, Maj+Tab vers l'élément précédent (ou vers les en-têtes, si l'item suivant est coché). Les flèches restent dans la grille. | Natif : Tab de cellule en cellule. |
| En-têtes _focalisables (tri au clavier) / _Focusable headers (keyboard sort) | `MnuGrlEntetes` | Les en-têtes de colonne deviennent focalisables et forment **un arrêt** placé avant les cellules : Gauche / Droite passent d'un en-tête à l'autre, Espace ou Entrée trie la colonne (croissant, puis décroissant). L'ordre de tri est exposé par `AutomationProperties.ItemStatus` de l'en-tête (« tri croissant » / « tri décroissant ») et signalé par un événement UIA de changement de propriété. | Natif : en-têtes non focalisables, tri à la souris, ordre non exposé. |
| _Nommer les cellules sans texte / _Name cells without text | `MnuGrlNoms` | Les cellules qui n'ont pas de texte affiché propre reçoivent un nom et une valeur utiles : **Priorité** : nom « Prioritaire » (triangle affiché) ou « Normale » ; **Statut** : nom = texte du statut (« En attente »…), valeur vide (lu une fois) ; **Payée** : nom d'une seule espace (NVDA ne le lit pas, et WPF ne remplace pas un nom non vide par son nom technique) et valeur « coché » / « non coché » : NVDA ne dit que l'état, et le changement par Espace reste annoncé (il passe par la valeur). Moyens : style de cellule (`CellStyle`, `AutomationProperties.Name` lié aux données) et `ClipboardContentBinding` des colonnes Statut et Payée (vide pour Statut, état traduit pour Payée), méthode de Guy Barker et `Docs/Constats/Sources-Microsoft.md`. Effet de bord : Ctrl+C copie « coché » / « non coché » et un statut vide. Code dans l'application (le nom dépend des données). **À vérifier** : NVDA ne lit-il vraiment rien pour le nom d'une espace ? | Natif : nom technique fabriqué par WPF (« Élément : Commande …, index d'affichage de la colonne : … »), puis la valeur (statut, « True » / « False »). |
| Une seule _lecture par cellule / Read each cell _once | `MnuGrlUneLecture` | Les cellules des colonnes de texte (`DataGridTextColumn` : N°, Client, Date, Montant) ont une **valeur UIA vide** : NVDA ne lit plus leur contenu qu'une fois (le nom). Moyen : leur `ClipboardContentBinding`, d'où WPF tire cette valeur, est remplacé par une liaison qui donne une chaîne vide ; le contenu copié (Ctrl+C) est rétabli par l'événement `CopyingCellClipboardContent` (`Docs/Constats/Arbre-Grille-Calendrier.md`). Les autres colonnes (case, liste, modèle) ne changent pas. Effet de bord : un client UIA ne peut plus lire ni écrire la valeur de ces cellules par le motif `Value` (NVDA ne s'en sert pas). | Natif : contenu lu deux fois (nom puis valeur). |

- Seuls les vrais en-têtes de colonne deviennent focalisables : l'en-tête de remplissage (case vide à droite de la dernière colonne, sans colonne associée, placé en premier dans le modèle de WPF) reste non focalisable. **Constaté** (utilisateur, 2026-11-16, première version) : il recevait le focus au premier Tab, NVDA disait « DataGrid sélectionné » et la barre d'état « DataGridColumnHeader ».
- Ordre de tabulation, les deux items cochés : en-têtes d'onglet, en-têtes de colonne (un arrêt), cellules (un arrêt, sur la cellule courante), sortie.
- Code : correctif réutilisable `Correctifs/CorrectifGrille.cs` (espace de noms `CorrectifsWpf`, sans dépendance, inclus par lien), branché sur `GrdCommandes`, avec un réglage par item du correctif (`UnArret`, `EntetesFocalisables`, `UneLecture` ; l’item « Nommer les cellules sans texte » est dans l’application) et les deux textes d'ordre de tri fournis par l'application (`TexteCroissant`, `TexteDecroissant`).
  - Un seul arrêt : `TabNavigation="Once"` sur la grille ; Tab et Maj+Tab (`PreviewKeyDown`) valident une édition en cours, puis déplacent le focus hors de la grille, la grille étant rendue non tabulable le temps du déplacement (même moyen que `CorrectifCalendrier`, pour ne pas s'arrêter sur la grille elle-même).
  - En-têtes : style d'en-tête de colonne dérivé du style en place, avec `Focusable` et `IsTabStop` vrais, sauf pour l'en-tête sans colonne (déclencheur sur `Column` nul) ; Tab depuis un en-tête va à la cellule courante (la première, à défaut) ; après chaque tri (`Sorting`), `ItemStatus` de chaque en-tête est mis à jour et l'événement de propriété levé depuis son peer (ou la source de ses événements). Méthode de tabulation d'après Microsoft (`CustomDataGrid`, Accessibility Insights), tri annoncé d'après Guy Barker (`Docs/Constats/Sources-Microsoft.md`).
- **À vérifier** : NVDA annonce-t-il un changement d'`ItemStatus` sur l'en-tête qui a le focus ? (NVDA le reçoit comme `UIA_itemStatus` ; annonce non vérifiée.)

## 8. Accessibilité

- Nom accessible : `AutomationProperties.Name` sur la grille (« Commandes » / « Orders »). Les lignes tirent leur nom de `ToString()`, les cellules de leur contenu, les en-têtes de colonne de leur texte.
- Types UIA attendus : `DataGrid` (motifs `Grid`, `Table`, `Selection`), lignes `DataItem`, cellules avec motifs `GridItem` et `TableItem` (en-tête de colonne associé), en-têtes `HeaderItem`. En .NET Framework 4.7.1 et suivants, les cellules sont annoncées comme cellules de grille (et non « personnalisé »).
- **À relever avec NVDA** : rôle « tableau » ; en-tête de colonne et position annoncés avec chaque cellule ; navigation de tableau de NVDA (Ctrl+Alt+flèches) ; annonce de la ligne sélectionnée ; nombre de lignes annoncé malgré la virtualisation ; cellule Priorité vide ; ligne d'ajout.
- **Lecture des en-têtes à chaque déplacement** : c'est un réglage de NVDA, pas de l'application : *Paramètres > Mise en forme des documents > Tableaux*, lecture des en-têtes de ligne et de colonne (intitulé exact selon la version de NVDA, non vérifié ici). Le nom de la ligne (« Commande … »), annoncé en changeant de ligne, ne dépend pas de ce réglage : NVDA annonce le nouveau conteneur du focus. L'application ne retire pas les en-têtes de l'UIA : d'autres utilisateurs en ont besoin, et la barre d'état les donne à la demande (section 9).

## 9. Menu et barre d'état avec cet onglet

- **Barre d'état** : le champ 1 (position) donne la ligne et la colonne en cours : « {nom de la ligne}, {en-tête de colonne} » (« Commande 1003, Montant », « Nouvelle ligne, Client » sur la ligne d'ajout) ; sur un en-tête de colonne, son texte seul. Mis à jour à chaque changement de focus et de cellule courante (`CurrentCellChanged`). Le champ 2 donne le type WPF de l'élément focalisé : `DataGridCell`, `DataGridColumnHeader` (en-têtes focalisables), `TextBox` (édition d'une cellule texte), `CheckBox`, `ComboBox`…
- Points de `ChecklistNVDA.md` qui concernent cet onglet : 6, 25, 26, 27.

## 10. Constats

Utilisateur avec NVDA, 2026-11-16, .NET 10, les deux items du menu cochés :

- **Tabulation** : Maj+Tab depuis une cellule va sur l'en-tête, Maj+Tab depuis l'en-tête sort de la grille ; Tab depuis une cellule (y compris de la dernière ligne) sort aussi. Conforme à la section 7.
- **Arrivée dans la grille** : depuis l'en-tête d'onglet, Tab, puis NVDA+Tab (relire le focus), donne « DataGrid sélectionné » ; il faut encore Tab ou Bas pour arriver sur une cellule. Élément focalisé après le premier Tab non établi (l'en-tête d'onglet « DataGrid », resté focalisé ? la grille elle-même ?).
- **Flèches Gauche / Droite** : le contenu de la cellule est lu **deux fois**, puis son en-tête de colonne. **Haut / Bas** : le nom de la ligne (« Commande … »), puis le contenu de la cellule deux fois. Cause probable (code de WPF, `DataGridCellItemAutomationPeer`) : la cellule expose son texte à la fois comme **nom** et comme **valeur** (motif `Value`, `GetCellAutomationValue`), et NVDA lit les deux. Non vérifié côté NVDA.
- **Cellule sans texte (Priorité)** : NVDA dit « élément commande… ». Explication (même code) : faute de texte, WPF fabrique un nom technique, « Élément : {ligne}, index d'affichage de la colonne : {n} » (format `DataGridCellItemAutomationPeer_NameCoreFormat`), d'où l'écart de ponctuation entre .NET 10 et 4.8 déjà relevé dans `CLAUDE.md`. Corrigé par l'item « Nommer les cellules sans texte » (section 7).
- **Case Payée** : Espace coche et décoche directement (sans passer par l'édition), le changement est annoncé.
- **Après correction** (utilisateur, 2026-11-16) : Tab depuis l'onglet arrive sur le premier vrai en-tête (en-tête de remplissage exclu) ; cellule Priorité lue « Normale », **une seule fois**. Indice pour la double lecture : la valeur UIA de cette cellule est vide (colonne modèle, sans texte), alors que les cellules de texte ont un nom et une valeur identiques ; la double lecture viendrait donc bien du nom et de la valeur lus l'un après l'autre.
- **Une seule lecture par cellule** (utilisateur, 2026-11-16, item coché) : les cellules de texte ne sont plus lues en double ; F2 et l'édition fonctionnent ; Ctrl+C copie toute la ligne (normal : sélection par ligne, `SelectionUnit="FullRow"`).
- **Liste des statuts** : les trois choix étaient lus « AccessTest » : leur nom venait du `ToString()` de `OptionStatut`, non défini (nom de la classe). Corrigé : `ToString()` renvoie le texte du statut.
- **Cellules Statut et Payée** : NVDA dit d'abord le nom technique (« Élément : Commande …, index d'affichage de la colonne : … »), puis la valeur : le statut (« En attente ») ou « True » / « False ». Ces cellules n'ont pas de texte affiché propre (elles contiennent une liste ou une case) : WPF leur fabrique le même nom technique qu'à Priorité. Leur valeur vient de `ClipboardContentBinding` (liaison de la colonne), d'où « True » / « False » pour la case. Corrigé par l'item « Nommer les cellules sans texte » (section 7).

## Textes

Textes FR/EN de l'onglet (`Textes.cs` les reprend ; le menu et la barre d'état sont dans `Fenetre.md`, section 5).

| Élément | FR | EN |
|---|---|---|
| DataGrid : grille (libellé et nom accessible) | Commandes | Orders |
| DataGrid : nom d'une ligne (`ToString()`) | Commande {0} | Order {0} |
| DataGrid : en-têtes de colonne | N°, Client, Date, Montant, Payée, Statut, Priorité | No., Customer, Date, Amount, Paid, Status, Priority |
| DataGrid : statuts | En attente, Expédiée, Livrée | Pending, Shipped, Delivered |
| DataGrid : menu contextuel, item 1 (à cocher) | Un seul arrêt de _tabulation | Single _tab stop |
| DataGrid : menu contextuel, item 2 (à cocher) | En-têtes _focalisables (tri au clavier) | _Focusable headers (keyboard sort) |
| DataGrid : menu contextuel, item 3 (à cocher) | _Nommer les cellules sans texte | _Name cells without text |
| DataGrid : menu contextuel, item 4 (à cocher) | Une seule _lecture par cellule | Read each cell _once |
| DataGrid : nom d'une cellule Priorité (item 3 coché) | Prioritaire / Normale | High priority / Normal |
| DataGrid : ordre de tri d'un en-tête (`ItemStatus`) | tri croissant / tri décroissant | sorted ascending / sorted descending |
| DataGrid : état d'une cellule Payée (item 3 coché) | coché / non coché | checked / unchecked |
| DataGrid : barre d'état, position dans la grille | {0}, {1} (ligne, colonne) | {0}, {1} (row, column) |
| DataGrid : nom de la ligne d'ajout (barre d'état) | Nouvelle ligne | New row |
