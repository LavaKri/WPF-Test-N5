# Onglet RichText — AccessTest

Spécification de l'onglet **RichText**. Voir `Fenetre.md` pour la zone d'onglets, le menu et la barre d'état, et `Specs.md` pour les règles d'accessibilité, `ChecklistNVDA.md` pour la checklist NVDA.

## 1. Contrôle

- Une barre de mise en forme en haut de l'onglet (section 1b), puis un `RichTextBox` (`x:Name="RtbEditeur"`) qui occupe le reste de l'onglet :
  - `AcceptsTab` : **faux au démarrage** (Tab sort du contrôle) ; l'item *Tab insère une tabulation* du menu contextuel (`Fenetre.md`, 2.7) le passe à vrai : Tab insère alors une tabulation et ne permet plus de quitter le contrôle au clavier ;
  - `VerticalScrollBarVisibility="Auto"` : barre de défilement verticale quand elle est utile ;
  - retour à la ligne automatique **toujours actif** : comportement natif, le `RichTextBox` n'a pas de réglage pour le couper. L'item *Retour à la ligne automatique* du menu contextuel partagé n'apparaît pas ici (`Fenetre.md`, 2.7.3) ; à la place, *Largeur de page* (Fenêtre au démarrage, Étroite 300 pixels, Large 2 000 pixels) fixe la largeur à laquelle le texte revient à la ligne (`Fenetre.md`, 2.7.4).
- En-tête d'onglet (`x:Name="TabRichText"`) : « RichText », non traduit.
- Contenu : un `UserControl` à part, `OngletRichText` (`Source/Onglets/OngletRichText.xaml` et `.xaml.cs`), placé dans l'onglet sous le nom `ContenuRichText`. Les `x:Name` de cette spécification sont ceux de ce contrôle. Le menu contextuel est celui, partagé avec le TextBox, de `MenuEditeurs` (`Fenetre.md`, 2.7), qui règle la largeur de page et le document interactif par les méthodes `DefinirLargeur` et `DefinirDocumentInteractif` de l'onglet.
- Nom accessible (`AutomationProperties.Name`) : « Zone de texte enrichi » / « Rich text area ».

## 1b. Barre de mise en forme

Réponse à l'ancienne question Q6 (utilisateur, 2026-11-17). But : tester une barre d'outils standard (`ToolBarTray` + `ToolBar`) avec un lecteur d'écran : rôle annoncé, arrêts de tabulation, boutons dont le texte visible ne suffit pas à les nommer, état enfoncé suivant la sélection, groupe exclusif dans une barre, retour au texte.

- Un `ToolBarTray` (`TrayMiseEnForme`) au-dessus de l'éditeur, contenant une `ToolBar` (`BarMiseEnForme`), nom accessible « Mise en forme » / « Formatting ». Pas de `ControlTemplate` personnalisé : les boutons prennent le style que la `ToolBar` donne à ses enfants.
- Contenu, de gauche à droite :

| # | Contrôle | `x:Name` | Affiché (FR / EN) | Nom accessible (FR / EN) | Commande WPF | Raccourci |
|---|---|---|---|---|---|---|
| 1 | `ToggleButton` | `TglGras` | **G** / **B** (lettre en gras) | Gras / Bold | `EditingCommands.ToggleBold` | Ctrl+B |
| 2 | `ToggleButton` | `TglItalique` | *I* / *I* (lettre en italique) | Italique / Italic | `EditingCommands.ToggleItalic` | Ctrl+I |
| 3 | `ToggleButton` | `TglSouligne` | <u>S</u> / <u>U</u> (lettre soulignée) | Souligné / Underline | `EditingCommands.ToggleUnderline` | Ctrl+U |
| — | `Separator` | | | | | |
| 4 | `RadioButton` | `RdoGauche` | icône : quatre traits alignés à gauche | Aligner à gauche / Align left | `EditingCommands.AlignLeft` | Ctrl+L |
| 5 | `RadioButton` | `RdoCentre` | icône : traits centrés | Centrer / Center | `EditingCommands.AlignCenter` | Ctrl+E |
| 6 | `RadioButton` | `RdoDroite` | icône : traits alignés à droite | Aligner à droite / Align right | `EditingCommands.AlignRight` | Ctrl+R |

- Les lettres sont celles de Word dans chaque langue (G, I, S en français ; B, I, U en anglais) ; l'icône d'alignement est dessinée (`Path`), sans texte, de la couleur du texte du bouton (lisible en Contraste élevé). Aucun bouton ne tire son nom de son contenu : chacun porte `AutomationProperties.Name`.
- Raccourcis : ceux des commandes de WPF, qui marchent déjà dans le `RichTextBox` sans la barre (Ctrl+B, Ctrl+I, Ctrl+U, Ctrl+L, Ctrl+E, Ctrl+R). WPF ne les range **pas** dans la commande (`InputGestures` des `EditingCommands` vide : ils sont posés comme raccourcis de classe de l'éditeur, illisibles par l'application) : l'application les **déclare elle-même**, un `KeyGesture` par bouton, affiché dans la langue de l'interface (`GetDisplayStringForCulture`), en infobulle (« Gras (Ctrl+B) ») et en `AutomationProperties.AcceleratorKey` (« Ctrl+B »). Sans `AcceleratorKey`, WPF expose à la place le **texte de la commande** (`ButtonBaseAutomationPeer.GetAcceleratorKeyCore`), en anglais : NVDA disait « Gras Bascule non enfoncée Toggle bold » (constat ci-dessous).
- Les trois boutons d'alignement forment un groupe exclusif (`GroupName="Alignement"`).
- Chaque bouton agit sur l'éditeur (`CommandTarget="RtbEditeur"`), où que soit le focus : sur la sélection, ou sans sélection sur le texte tapé ensuite (gras, italique, souligné) et sur le paragraphe du curseur (alignement).
- **État des boutons** : il suit le texte, pas seulement les clics. À chaque déplacement du curseur ou de la sélection (`SelectionChanged`), et après chaque commande d'édition exécutée sur l'éditeur (clic dans la barre ou raccourci tapé dans le texte), l'application relit la mise en forme :
  - Gras, Italique, Souligné : enfoncés si toute la sélection a cette mise en forme (sans sélection : celle du curseur, y compris une mise en forme demandée et pas encore tapée) ; relâchés sinon, y compris pour une sélection mélangée ;
  - alignement : le bouton de l'alignement du paragraphe ; aucun bouton coché si la sélection mélange les alignements ou si l'alignement est « justifié ».
  - Moyen : `TextSelection.GetPropertyValue`, et pour la mise en forme demandée et pas encore tapée la méthode interne `TextSelection.GetCurrentValue` (celle des commandes de WPF), appelée par réflexion ; introuvable : `GetPropertyValue` seul.
- Au démarrage (document vide) : Gras, Italique et Souligné relâchés, « Aligner à gauche » coché.
- **Clavier** : avec le correctif de la barre d'outils (`Correctifs.md`), la barre est un seul arrêt de tabulation. Sinon, comportement natif à observer (rien d'autre n'est réglé par l'application) : ordre de tabulation (en-tête d'onglet, barre, éditeur ?) ; la barre est-elle un seul arrêt ou un arrêt par bouton, les flèches passent-elles d'un bouton à l'autre ; où est le focus après un clic ou Espace sur un bouton (reste-t-il dans la barre ?) ; la `ToolBar` étant une portée de focus (`FocusManager.IsFocusScope`), comme le menu.
- Enregistrer (`Fenetre.md`, 2.1.2) enregistre le texte brut : la mise en forme est perdue.

### Constats (utilisateur, NVDA, .NET 10, 2026-11-17)

- La barre est visible et atteinte par Tab depuis l'en-tête d'onglet. Chaque bouton est lu avec son nom accessible (« Gras », pas « G »).
- **Piège clavier** : dans la barre, Tab, Maj+Tab et les flèches tournent d'un bouton à l'autre ; Tab ne sort pas de la barre. Cause (code source de WPF, `ToolBar.cs`) : la `ToolBar` règle elle-même `KeyboardNavigation.TabNavigation` sur `Cycle`. Corrigé par *Correctif > Corriger la barre d'outils* (`Correctifs.md`), coché au démarrage : décocher pour retrouver le piège.
- **Sorties** : Échap, ou Espace sur un bouton (l'action est faite), font sortir de la barre et rendent le focus **à l'élément d'où l'on venait** (le texte, ou l'en-tête d'onglet). Comportement natif de la `ToolBar`, portée de focus comme le menu. Ce sont les seules sorties au clavier.
- Gras activé dans la barre : le texte tapé ensuite est bien en gras.
- **Raccourci annoncé « Toggle bold »** : NVDA dit « Gras Bascule non enfoncée Toggle bold » ; ni « Ctrl+B » ni l'infobulle. Causes : (1) l'application cherchait le raccourci dans `InputGestures` de la commande, vide pour les `EditingCommands` : ni infobulle avec raccourci, ni `AcceleratorKey` ; (2) code de WPF (`ButtonBaseAutomationPeer.GetAcceleratorKeyCore`) : sans `AcceleratorKey`, un bouton lié à une `RoutedUICommand` expose le **texte de la commande** comme raccourci, et NVDA le lit comme tel. Corrigé dans l'application (raccourcis déclarés, voir plus haut).
- **Raccourci lu deux fois** après cette correction : « Gras, bascule, non enfoncée, Ctrl+B, Ctrl+B ». Cause : WPF expose l'infobulle comme texte d'aide (`HelpText`) quand aucune aide n'est posée, et NVDA lit l'aide après le raccourci ; l'infobulle (« Gras (Ctrl+B) ») contient le raccourci. Avant, l'infobulle valait « Gras », égale au nom, et NVDA ne la répétait pas. Correction : l'infobulle reste complète à l'écran, et chaque bouton reçoit un texte d'aide d'**une seule espace** (`AutomationProperties.HelpText=" "`), que NVDA ne lit pas et qui empêche WPF de reprendre l'infobulle (même moyen que pour la cellule Payée, `DataGrid.md` 7). À revérifier : attendu « Gras, bascule, non enfoncée, Ctrl+B ».
- **Ctrl+B** met bien en gras (confirmé). Une première hypothèse (raccourcis traduits en français, Ctrl+G) est donc **fausse** : le « Ctrl+G » des ressources françaises de WPF désigne une autre commande.

## 2. Chargement d'un fichier

Au démarrage, le `RichTextBox` contient le document mis en forme (2c). *Fichier > Ouvrir...* (`Fenetre.md`, 2.1.1) charge un fichier au choix ; *Fichier > Enregistrer...* enregistre le texte brut, sans mise en forme (`Fenetre.md`, 2.1.2). Chargement :

- le texte est inséré comme **paragraphes simples**, un par ligne, sans marge (le Markdown n'est **pas** interprété) ;
- le document existant est **vidé puis rempli** ; il ne doit **pas** être remplacé (`Document = new FlowDocument`) : le peer UI Automation reste lié au document d'origine et le lecteur d'écran ne verrait plus le texte (constaté avec NVDA, voir `Docs/Constats/Texte.md`) ;
- le curseur est placé au début du document, et le `RichTextBox` reçoit le focus.

## 2b. Menu contextuel

Le `RichTextBox` a le même menu contextuel que le `TextBox` (`Fenetre.md`, 2.7, une seule instance partagée) : sous-menu **Correctif** (ici seulement, `Fenetre.md`, 2.7.5 : *Améliorer la lecture du texte enrichi*, *Document interactif*) et **Correcteur orthographique** (case à cocher, décochée au démarrage, avec un item Suggestions en tête du menu sur un mot mal orthographié). Il remplace le menu contextuel natif.

## 2c. Document mis en forme

Contenu du `RichTextBox` **au démarrage** (depuis le 2026-11-18 ; avant, *Charger > Document mis en forme* du menu contextuel, supprimé) : un document construit par l'application, pour tester ce que NVDA annonce des éléments structurés d'un `FlowDocument` (proposition 2 du 2026-11-17, contenu choisi par l'utilisateur).

- Le document existant est **vidé puis rempli** (jamais remplacé), curseur au début, **sans prendre le focus** (`Fenetre.md`, 2.5). Textes dans la langue de démarrage, non retraduits au changement de langue.
- Contenu, dans l'ordre (textes FR/EN : la section « Textes » en fin de fichier). Une **ligne vide** (paragraphe vide) sépare chaque bloc : titre, paragraphe, liste à puces (avec son titre de niveau 2 et son libellé), liste numérotée (avec son libellé), tableau (avec son titre de niveau 2 et son libellé), paragraphe du lien (avec son titre de niveau 2), fin (demande de l'utilisateur, 2026-11-17). Un titre de niveau 2 est directement suivi de son bloc, sans ligne vide (titres de niveau 2 ajoutés le 2026-11-18, demande de l'utilisateur) :

| # | Élément WPF | Contenu | Ce qu'on observe |
|---|---|---|---|
| 1 | `Paragraph`, police plus grande (20) et grasse, marqué titre de niveau 1 (`CorrectifLectureRiche.NiveauTitre`) | Titre « Document d'essai » | Natif : pas de titre (WPF n'en a pas la notion). Avec le correctif de lecture du texte enrichi (`Correctifs.md`) : « titre de niveau 1 ». |
| 2 | `Paragraph` avec `Bold`, `Italic`, `Underline` | « Ce paragraphe contient du texte en gras, en italique et souligné. » | Mise en forme lue par NVDA+F, et à la lecture si l'annonce de la mise en forme est activée dans NVDA. |
| 2a | `Paragraph`, police 16 et grasse, marqué titre de niveau 2 | Titre « Essais de listes » | Avec le correctif : « titre de niveau 2 » (**confirmé**, utilisateur, 2026-11-18). Les titres 5a et 6a sont faits de même. |
| 3 | `Paragraph` puis `List` à puces (`MarkerStyle="Disc"`) | « Une liste de fruits : » puis Pommes, Poires, Cerises | Annonce d'une liste, de la puce, du nombre d'éléments. Natif : rien. Avec le correctif de lecture du texte enrichi : « liste » et « hors liste », ni puce ni nombre d'éléments. |
| 4 | `Paragraph` puis `List` numérotée (`MarkerStyle="Decimal"`) | « Étapes numérotées de procédure : » puis Ouvrir le fichier, Lire le texte, Fermer le fichier | Annonce des numéros. Natif : rien. Avec le correctif : « liste » et « hors liste », sans les numéros. |
| 5a | `Paragraph`, titre de niveau 2 | Titre « Essais de tableau simple » | « titre de niveau 2 ». |
| 5 | `Paragraph` puis `Table` de 3 colonnes, ligne d'en-tête en gras et marquée comme telle (`CorrectifLectureRiche.LigneEntete`), bordures de la couleur du texte | « Tableau : » puis Produit, Quantité, Prix ; Pommes, 3, 2,50 € ; Poires, 2, 1,80 € ; Cerises, 1, 4,00 € | Annonce d'un tableau, des lignes et colonnes, de l'en-tête ; navigation de tableau de NVDA (Ctrl+Alt+flèches). |
| 6a | `Paragraph`, titre de niveau 2 | Titre « Test de liens » | « titre de niveau 2 ». |
| 6 | `Paragraph` avec `Hyperlink` | « Pour en savoir plus, voir le site de NVDA. » ; lien « site de NVDA » vers `https://www.nvaccess.org/` | Natif : lien non annoncé (constat). Avec le correctif de lecture : « lien ». Activation (ci-dessous). |
| 7 | `Paragraph` | « Fin du document. » | Fin repérable. |

- **Activation du lien** : menu contextuel > *Correctif* > **Document interactif** (`MnuDocInteractif`, `Fenetre.md`, 2.7.5, case à cocher, **cochée au démarrage**, seulement depuis le `RichTextBox`) règle `IsDocumentEnabled`. Décochée (natif) : le lien est inerte. Cochée : un clic sur le lien (ou une activation UIA, motif `Invoke`) lève `RequestNavigate`, que l'application intercepte : une `MessageBox` d'information « Lien activé » affiche l'adresse ; **aucun navigateur n'est ouvert**.
- **Entrée sur le lien** (demande de l'utilisateur, 2026-11-18 ; code de l'application, pas un correctif) : *Document interactif* coché, sans sélection, Entrée sans modificateur quand le curseur est dans un lien **active ce lien** comme un clic : même `MessageBox` « Lien activé ». L'activation passe par le peer UIA du lien (`HyperlinkAutomationPeer`, motif `Invoke`), qui fait exactement ce que fait le clic de WPF (`Click`, `RequestNavigate`, commande). Entrée est alors consommée : pas de saut de paragraphe dans le lien. Hors d'un lien, ou *Document interactif* décoché : Entrée garde son comportement natif. **Confirmé** par l'utilisateur (2026-11-18, .NET 10 et 4.8) : Entrée sur le lien ouvre la boîte « Lien activé ». Curseur juste avant ou juste après le lien : WPF le rattache au lien ou au texte voisin selon le sens du curseur (comportement natif, à observer).
- **Constat, *Document interactif* (alors *Activer les liens*) coché, avant l'activation par Entrée** (utilisateur, NVDA, .NET 10, 2026-11-18) : le lien n'est plus annoncé « indisponible » ; sur le lien, toute touche (Espace compris) écrit dans le texte, comme ailleurs dans l'éditeur ; **Entrée ne fait rien** : ni activation du lien, ni saut de paragraphe. Le lien n'est donc **pas activable au clavier** (seulement à la souris, ou par un client UIA qui appelle `Invoke`).
- **Enregistrer** (`Fenetre.md`, 2.1.2) écrit le texte brut : puces, numéros et tableau deviennent du texte (à observer).
- **Constat** (utilisateur, NVDA, .NET 10, 2026-11-17, première version sans lignes vides) : NVDA ne signale que le **tableau** ; titre, listes et lien ne sont pas annoncés comme tels. Dans le tableau, les commandes de navigation de tableau de NVDA (Ctrl+Alt+flèches) **ne fonctionnent pas**. Cause non recherchée (exposition UIA des éléments d'un `FlowDocument` par WPF, ou prise en charge par NVDA du motif `Text` de WPF).

## 3. Position dans la barre d'état

Le champ 1 de la barre d'état donne `Ligne {0}, colonne {1}`, numérotée à partir de 1 :

- ligne : nombre de lignes affichées entre le début du document et le curseur, plus un (`GetLineStartPosition`) ;
- colonne : nombre de caractères entre le début de la ligne affichée et le curseur, plus un.

## 4. Lecture par un lecteur d'écran

- Type de contrôle UIA : `Document` (et non `Edit` comme le `TextBox`), avec les motifs `Text` et `Scroll`.
- Les lignes vides sont lues de travers sans correctif, comme dans le `TextBox`, et le correctif de la PR dotnet/wpf#11509 seule ne suffit pas ici : un paragraphe vide ne donne pas une plage vide. Le correctif de l'application est élargi pour ce cas (voir `Correctifs.md`, et `Docs/Constats/Texte.md`).
- Barre de mise en forme (1b), types UIA attendus : `ToolBar` (nommée « Mise en forme »), `Button` avec le motif `Toggle` (Gras, Italique, Souligné), `RadioButton` avec le motif `SelectionItem` (alignement), `Separator`. À relever avec NVDA : nom, rôle et état (« enfoncé », « coché ») de chaque bouton, raccourci annoncé ou non (`AcceleratorKey`), mise en forme lue dans le texte (NVDA+F).
- Points de `ChecklistNVDA.md` qui concernent cet onglet : 4, 6, 7, 8, 13, 39, 40, 43, 44, 45, 46.

## Textes

Textes FR/EN de l'onglet (`Textes.cs` les reprend ; le menu et la barre d'état sont dans `Fenetre.md`, section 5).

| Élément | FR | EN |
|---|---|---|
| Correctif (sous-menu du menu contextuel, RichText seulement) | _Correctif | _Fixes |
| Améliorer la lecture du texte enrichi (dans ce sous-menu, à cocher) | Améliorer la lecture du texte _enrichi | Improve rich te_xt reading |
| Document interactif (dans ce sous-menu, à cocher) | _Document interactif | _Interactive document |
| Lien activé (titre / texte du message) | Lien activé / Le lien mène à :\n{0} | Link activated / The link leads to:\n{0} |
| Document mis en forme : titre | Document d'essai | Test document |
| Document mis en forme : paragraphe (gras, italique, souligné) | Ce paragraphe contient du texte en **gras**, en *italique* et souligné. | This paragraph contains **bold**, *italic* and underlined text. |
| Document mis en forme : titres de niveau 2 | Essais de listes / Essais de tableau simple / Test de liens | List tests / Simple table tests / Link test |
| Document mis en forme : listes | Une liste de fruits : Pommes, Poires, Cerises / Étapes numérotées de procédure : Ouvrir le fichier, Lire le texte, Fermer le fichier | A list of fruits: Apples, Pears, Cherries / Numbered procedure steps: Open the file, Read the text, Close the file |
| Document mis en forme : tableau | Tableau : Produit, Quantité, Prix ; Pommes, 3, 2,50 € ; Poires, 2, 1,80 € ; Cerises, 1, 4,00 € | Table: Product, Quantity, Price; Apples, 3, €2.50; Pears, 2, €1.80; Cherries, 1, €4.00 |
| Document mis en forme : lien et fin | Pour en savoir plus, voir le site de NVDA. / Fin du document. | To learn more, see the NVDA website. / End of document. |
| Menu contextuel du `RichTextBox` seulement : largeur de page (sous-menu) | _Largeur de page | Page widt_h |
| Largeur de page : choix | _Fenêtre / É_troite (300 pixels) / _Large (2 000 pixels) | _Window / _Narrow (300 pixels) / W_ide (2,000 pixels) |
| Barre de mise en forme (onglet RichText) : nom | Mise en forme | Formatting |
| Gras / Italique / Souligné : lettre affichée | G / I / S | B / I / U |
| Gras / Italique / Souligné : nom accessible | Gras / Italique / Souligné | Bold / Italic / Underline |
| Alignement : nom accessible | Aligner à gauche / Centrer / Aligner à droite | Align left / Center / Align right |
| Infobulle d'un bouton de la barre | {nom} ({raccourci}) | {name} ({shortcut}) |
| Nom accessible du `RichTextBox` | Zone de texte enrichi | Rich text area |
