# Onglet Choix — AccessTest

Spécification de l'onglet **Choix** : les contrôles de choix simple (boutons radio, calendrier, sélecteur de date) et de choix multiple (liste à sélection multiple, case à trois états, bouton à bascule), puis deux contrôles de réglage d'une valeur (curseur, barre de défilement).
Voir `Fenetre.md` pour la zone d'onglets, le menu et la barre d'état, et `Specs.md` pour les règles d'accessibilité, `ChecklistNVDA.md` pour la checklist NVDA.

## 1. Onglet

- En-tête (`x:Name="TabChoix"`) : « Choix », non traduit.
- Contenu : un `UserControl` à part, `OngletChoix` (`Source/Onglets/OngletChoix.xaml` et `.xaml.cs`), placé dans l'onglet sous le nom `ContenuChoix`. Les `x:Name` ci-dessous sont ceux de ce contrôle ; ils restent les `AutomationId` des éléments. La fenêtre ne fait qu'appliquer la langue (`AppliquerLangue`).
- Placé **après** l'onglet Usuels (quatrième onglet, suivi de TreeView). Au démarrage, l'onglet sélectionné et le focus restent ceux de l'onglet TextBox.
- Un seul comportement : le menu contextuel du calendrier (voir 3). Les autres contrôles, bouton à bascule compris, ne déclenchent rien : ils servent à tester ce que le lecteur d'écran annonce.
- Le contenu est dans un `ScrollViewer` (barre verticale `Auto`) : la fenêtre restaurée (900 × 600) est plus basse que le contenu.

## 2. Disposition

Même grille que l'onglet Usuels (`Usuels.md`, section 2) : deux colonnes de largeur égale (`*` / `*`), le libellé à gauche (texte aligné à droite), le contrôle à droite. Les contrôles qui portent leur propre texte (groupe, case à cocher, bouton) n'ont pas de libellé : ils sont dans la colonne de droite, alignés à gauche, à taille naturelle.

| # | Libellé (colonne de gauche) | Contrôle (colonne de droite) | `x:Name` du contrôle |
|---|---|---|---|
| 1 | *(vide)* | `GroupBox` « Couleur » contenant trois `RadioButton` : Rouge, Vert, Bleu | `GrpCouleur`, `RdoRouge`, `RdoVert`, `RdoBleu` ; conteneur des boutons `PnlCouleur` ; menu contextuel `CtxCouleur` |
| 2 | QCM multiple | `ListBox` à sélection multiple : Nord, Sud, Est, Ouest | `LstQcm` (items `ItmNord`, `ItmSud`, `ItmEst`, `ItmOuest`) |
| 3 | *(vide)* | `CheckBox` à trois états « Case à trois états » | `ChkTroisEtats` |
| 4 | *(vide)* | `ToggleButton` « Bouton à bascule » | `TglBascule` |
| 5 | Calendrier | `Calendar`, un seul jour sélectionnable, avec un menu contextuel | `CalCalendrier`, `CtxCalendrier` |
| 6 | Date | `DatePicker` | `DatDate` |
| 7 | Curseur | `Slider` horizontal, de 0 à 100, avec un menu contextuel | `SldCurseur`, `CtxCurseur` |
| 8 | Barre de défilement | `ScrollBar` horizontale, de 0 à 100, valeur synchronisée avec le curseur | `ScbDefilement` |

Libellés : `LblQcm`, `LblCalendrier`, `LblDate`, `LblCurseur`, `LblDefilement`. Les lignes 7 et 8 sont **sous** le sélecteur de date, une ligne chacune ; le contrôle occupe toute la largeur de la colonne de droite. Textes FR/EN dans la section « Textes » en fin de fichier.

## 3. Comportement

- **Couleur** : choix exclusif entre les trois boutons radio (même `GroupName`, `Couleur`). **« Vert » est sélectionné au démarrage** (un bouton est donc toujours choisi). Le groupe est **un seul arrêt de tabulation** : Tab et Maj+Tab ne passent jamais d'un bouton à un autre (`KeyboardNavigation.TabNavigation="Once"` sur `PnlCouleur`, le conteneur des boutons). **Limite constatée** (utilisateur, 2026-11-14) : avec `Once`, WPF envoie le focus au dernier bouton focalisé ou, à défaut, au premier : à la première arrivée, le focus est sur « Rouge », non coché, alors que « Vert » est coché. **Conclusion des essais** : sans `Once` (gestion native des groupes de boutons radio), le focus arrive bien sur le bouton coché, mais Tab passe ensuite d'un bouton à l'autre ; avec `Once`, il n'y a qu'un arrêt, mais pas sur le bouton coché. Obtenir les deux demande du code (redirection du focus vers le bouton coché à l'arrivée depuis l'extérieur), non fait pour l'instant. Seules les flèches passent d'un bouton à l'autre et déplacent la sélection, **sans sortir du groupe** : sur le premier ou le dernier bouton, la flèche ne fait rien (`KeyboardNavigation.DirectionalNavigation="Contained"`, ni retour au début ni sortie). Pour quitter le groupe, on utilise Tab ou Maj+Tab depuis n'importe quel bouton.
- **Menu contextuel du groupe de couleurs** (`CtxCouleur`, posé sur le `GroupBox` : clic droit, touche Applications ou Maj+F10 depuis n'importe lequel des trois boutons) : un item à cocher, **Limiter les déplacements** (`MnuCouleurLimiter`), **coché au démarrage**. Coché : un seul arrêt de tabulation (`TabNavigation="Once"`) et flèches contenues dans le groupe (`DirectionalNavigation="Contained"`). Décoché : comportement natif de WPF (les deux réglages remis à leur valeur par défaut) : le focus arrive sur le bouton coché, Tab passe d'un bouton à l'autre et sort après le dernier, les flèches peuvent sortir du groupe. Le réglage est posé par code sur `PnlCouleur` (le conteneur des boutons) ; l'item agit sur `Checked` / `Unchecked` (le `TogglePattern` UIA ne déclenche pas `Click`).
- **QCM multiple** : `SelectionMode="Multiple"` (un clic, ou Espace, ajoute ou retire l'item de la sélection ; pas besoin de Ctrl). Aucun item sélectionné au démarrage.
- **Case à trois états** : `IsThreeState="True"`, décochée au démarrage. Cycle : décochée, cochée, indéterminée.
- **Bouton à bascule** : `ToggleButton`, non enfoncé au démarrage. Il ne fait rien d'autre que changer d'état (il ne cache plus le calendrier ; c'est le menu contextuel qui agit dessus).
- **Calendrier** : `SelectionMode="SingleDate"`, aucune date sélectionnée au démarrage (le mois affiché est le mois courant). Comportement de WPF (code source, `Calendar.ProcessSelection`) : **la flèche sélectionne le jour atteint**, la sélection suit le focus ; Espace et Entrée ne font rien dans la vue des jours (ils ouvrent un mois ou une année dans les autres vues). Au clavier, le jour où l'on est est donc toujours la date choisie. **Confirmé avec NVDA** (utilisateur, 2026-11-16) : un jour atteint par les flèches, relu, est annoncé « bouton sélectionné » ; après « Sélectionner Demain » (menu contextuel), le focus reste sur ce jour, qui n'est plus annoncé que « bouton » : demain est sélectionné, mais le focus n'y va pas.
- **Menu contextuel du calendrier** (`CtxCalendrier`, ouvert par clic droit, touche Applications ou Maj+F10) : deux items.
  - **Sélectionner Demain** (`MnuCalDemain`) : sélectionne la date de demain (`DateTime.Today.AddDays(1)`) et affiche son mois.
  - **Masquer le Calendar Day Button** (`MnuCalMasquerJours`) : item à cocher, **décoché au démarrage** (il était coché tant que le piège Maj+Tab n'était pas corrigé ; depuis le 2026-11-15, le menu Correctif le corrige, voir plus bas). Coché : tous les `CalendarDayButton` (les jours) sont masqués (`Visibility="Hidden"`, la taille du calendrier est conservée) ; le calendrier reste affiché avec ses boutons de navigation et ses noms de jours. Décoché : les jours sont de nouveau visibles. Les boutons sont retrouvés dans l'arbre visuel après l'affichage (le modèle du `Calendar` n'est pas modifié) et le choix est réappliqué à chaque affichage de l'onglet et quand le mois, l'année ou la vue changent. L'item agit sur `Checked` / `Unchecked` (le `TogglePattern` UIA ne déclenche pas `Click`).
  - Jours masqués, « Sélectionner Demain » est la seule façon de choisir une date dans le calendrier.
  - **Constaté avec NVDA** (utilisateur, 2026-11-14) : jours masqués, Tab depuis le bouton à bascule annonce « Calendrier » et le menu contextuel (« Sélectionner Demain »…) peut être invoqué ; le piège Maj+Tab a disparu, faute de jour focalisable. Les deux éléments tabulables du contrôle sont donc le jour du calendrier (ou le `Calendar` lui-même quand les jours sont masqués) et le champ de saisie du `DatePicker`.
  - **Constaté** : sur le champ de saisie du `DatePicker`, le menu contextuel est le menu d'édition natif (Couper, Copier, Coller…), pas celui du calendrier : c'est normal, le menu n'est posé que sur le `Calendar`.
- **Date** : `DatePicker` qui contient **la date du jour au démarrage** (`SelectedDate = DateTime.Today`, au format de la langue de l'application), saisie libre ou choix dans le calendrier déroulant (Alt+Bas ou F4). Le calendrier, lui, n'a pas de date sélectionnée : il n'est pas synchronisé avec le sélecteur.
- **Constat (Maj+Tab)** : sur le `Calendar` natif, Maj+Tab laisse le focus sur le jour (piège clavier, Tab vers l'avant fonctionne). Constaté par l'utilisateur ; le piège disparaissait quand le calendrier était caché (version précédente, où le bouton à bascule le cachait), ce qui le situe dans le calendrier lui-même. **Corrigé** (2026-11-15) par *Correctif > Corriger le calendrier*, coché au démarrage (`Correctifs.md`) : Maj+Tab depuis un jour va à l'élément d'avant (le bouton à bascule). Cause probable : le `Calendar`, arrêt de tabulation lui-même, renvoie le focus au jour dès qu'il le reçoit (`Docs/Constats/Arbre-Grille-Calendrier.md`). Pour reproduire le piège : décocher l'item du menu Correctif (jours visibles, réglage par défaut).
- **Constat (lecture des jours)**, utilisateur avec NVDA, 2026-11-15, jours visibles : chaque jour est annoncé avec sa date complète, aux flèches comme à l'arrivée par Tab ; sélection, « Sélectionner Demain », PageHaut / PageBas, Début / Fin, vue des mois et passage en EN corrects. Espace ne fait rien entendre : ce n'est pas un défaut d'annonce, Espace ne change rien (la flèche a déjà sélectionné le jour, voir plus haut). La sélection par la flèche n'est pas annoncée par NVDA ; la faire annoncer (essai, `Correctifs.md`) donnait « bouton sélectionné » à chaque flèche, sans intérêt : retiré.
- **Curseur** : `Slider` horizontal, `Minimum="0"`, `Maximum="100"`, valeur **50** au démarrage, graduations tous les 10 (`TickFrequency="10"`, `TickPlacement="BottomRight"`), pas libre (pas d'aimantation aux graduations). Flèches : ±1 (`SmallChange`) ; PageHaut / PageBas : ±10 (`LargeChange`) ; Début / Fin : minimum / maximum.
  - **Menu contextuel du curseur** (`CtxCurseur`, posé sur le `Slider` : clic droit, touche Applications ou Maj+F10) : un item à cocher, **Rendre visible la scrollbar** (`MnuCurseurScrollbar`), **coché au démarrage**. Coché : la barre de défilement et son libellé sont visibles. Décoché : ils sont masqués (`Visibility="Collapsed"` : hors mise en page, hors ordre de tabulation et hors arbre UIA). La synchronisation des valeurs (ci-dessous) continue pendant qu'ils sont masqués. L'item agit sur `Checked` / `Unchecked` (branchés dans le constructeur).
- **Barre de défilement** : `ScrollBar` horizontale, `Minimum="0"`, `Maximum="100"`, valeur **synchronisée avec celle du curseur** (donc 50 au démarrage : liaison de données à double sens `Value` du `Slider`, sans code ; déplacer l'un déplace l'autre), `ViewportSize="10"` (taille du curseur de la barre), `SmallChange="1"`, `LargeChange="10"`. Elle ne défile rien : elle sert à tester la lecture de sa valeur. **Constaté** (utilisateur, 2026-11-14) : la `ScrollBar` n'est pas atteignable au clavier (hors de l'ordre de tabulation, `Focusable="False"` par défaut dans WPF). On ne le change pas : c'est le comportement natif, à relever. Un utilisateur du clavier ne peut donc pas régler une valeur avec une `ScrollBar` isolée ; le contrôle adapté est le `Slider`, focalisable. Rendre la barre focalisable (`Focusable="True"`) ne suffirait sans doute pas : à ma connaissance elle ne gère pas les flèches elle-même (non vérifié), il faudrait du code.
- Les noms des mois et des jours suivent la langue de l'application (propriété `Language` de la fenêtre, voir `Fenetre.md`, 2.6).

## 4. Accessibilité

- Noms accessibles : `AutomationProperties.Name` sur `LstQcm` (« QCM multiple »), `CalCalendrier` (« Calendrier »), `DatDate` (« Date »), `SldCurseur` (« Curseur ») et `ScbDefilement` (« Barre de défilement »). Le `GroupBox`, les boutons radio, la case à cocher et le bouton à bascule tirent leur nom de leur texte visible ; le nom du groupe est celui de son en-tête.
- Le sélecteur de date porte aussi un texte d'aide (`AutomationProperties.HelpText`) qui indique comment choisir une date : « Alt+Bas pour choisir une date » / « Alt+Down to pick a date ». Le nom accessible reste « Date ». **Constaté avec NVDA** (utilisateur, 2026-11-14) : l'aide est bien lue quand le champ de saisie du `DatePicker` prend le focus (« Date, édition, Alt+Bas pour choisir une date »), bien qu'elle soit posée sur le `DatePicker` et non sur son champ interne.
- Ordre de tabulation : groupe de boutons radio (un seul arrêt), liste, case à trois états, bouton à bascule, calendrier (Tab arrive sur le jour sélectionné ou le jour affiché ; jours masqués, sur le `Calendar` lui-même, annoncé « Calendrier » (constaté avec NVDA)), sélecteur de date, curseur. En arrière, Maj+Tab sort du calendrier si *Corriger le calendrier* est coché. La barre de défilement n'est pas dans l'ordre si elle n'est pas focalisable (voir plus haut).
- **Constat (point d'arrivée)** : le calendrier et le sélecteur de date sont deux arrêts de tabulation distincts, parcourus dans un sens par Tab et dans l'autre par Maj+Tab. C'est normal.
  - **Constaté avec NVDA** (utilisateur, 2026-11-15, jours visibles, *Corriger le calendrier* coché) : Tab depuis le bouton à bascule annonce « Calendrier. Dimanche 15 novembre 2026 » (le jour affiché, faute de date sélectionnée), puis « Date. 15 novembre 2026 » (champ du `DatePicker`, date du jour). Maj+Tab : l'inverse, sans piège.
  - Les deux montrent la même date au démarrage sans être liés : « Sélectionner Demain » ne change que le calendrier.
  - Le menu contextuel diffère selon l'arrêt : celui de l'application sur le calendrier, le menu d'édition natif sur le champ du `DatePicker`.
- Pas de `ControlTemplate` personnalisé (règle 6 de `Specs.md`).

## 5. Menu et barre d'état avec cet onglet

- **Barre d'état** : le champ 1 (position) est vide ; le champ 2 donne le type WPF du contrôle focalisé : `RadioButton`, `ListBoxItem`, `CheckBox`, `ToggleButton`, `CalendarDayButton`, `DatePickerTextBox`, `Slider`…

## 6. Lecture par un lecteur d'écran

- Types UIA attendus : `Group` (le `GroupBox`), `RadioButton`, `List` et `ListItem`, `CheckBox`, `Button` avec le motif `Toggle`, `Calendar`, `ComboBox`-like pour le `DatePicker` (à relever), `Slider` et `ScrollBar` (motif `RangeValue`).
- Points de `ChecklistNVDA.md` qui concernent cet onglet : 6, 8, 17, 18, 19, 24.

## Textes

Textes FR/EN de l'onglet (`Textes.cs` les reprend ; le menu et la barre d'état sont dans `Fenetre.md`, section 5).

| Élément | FR | EN |
|---|---|---|
| Choix : groupe de boutons radio | Couleur | Color |
| Choix : boutons radio | Rouge / Vert / Bleu | Red / Green / Blue |
| Choix : menu contextuel du groupe de couleurs (à cocher) | _Limiter les déplacements | _Limit movement |
| Choix : liste multiple (libellé et nom accessible) | QCM multiple | Multiple choice |
| Choix : entrées de la liste | Nord / Sud / Est / Ouest | North / South / East / West |
| Choix : case à trois états | Case à trois états | Three-state check box |
| Choix : bouton à bascule | Bouton à bascule | Toggle button |
| Choix : calendrier (libellé et nom accessible) | Calendrier | Calendar |
| Choix : menu contextuel du calendrier, item 1 | _Sélectionner Demain | _Select Tomorrow |
| Choix : menu contextuel du calendrier, item 2 (à cocher) | _Masquer le Calendar Day Button | _Hide the Calendar Day Button |
| Choix : sélecteur de date (libellé et nom accessible) | Date | Date |
| Choix : sélecteur de date (texte d'aide) | Alt+Bas pour choisir une date | Alt+Down to pick a date |
| Choix : curseur (libellé et nom accessible) | Curseur | Slider |
| Choix : barre de défilement (libellé et nom accessible) | Barre de défilement | Scroll bar |
| Choix : menu contextuel du curseur (à cocher) | _Rendre visible la scrollbar | _Show the scroll bar |
