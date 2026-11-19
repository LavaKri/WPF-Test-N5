# Onglet Annonce — AccessTest

Spécification de l'onglet **Annonce** : envoyer un texte au lecteur d'écran par trois sorties différentes (événement de notification UIA, région vivante, focus), avec leurs paramètres, une fois ou toutes les 5 secondes, pour comparer ce que chaque lecteur annonce.
Voir `Fenetre.md` pour la zone d'onglets, le menu et la barre d'état, et `Specs.md` pour les règles d'accessibilité, `ChecklistNVDA.md` pour la checklist NVDA.

## 1. Onglet

- En-tête (`x:Name="TabAnnonce"`) : « Annonce », non traduit.
- Contenu : un `UserControl` à part, `OngletAnnonce` (`Source/Onglets/OngletAnnonce.xaml` et `.xaml.cs`), placé dans l'onglet sous le nom `ContenuAnnonce`. Les `x:Name` ci-dessous sont ceux de ce contrôle ; ils restent les `AutomationId` des éléments. La fenêtre ne fait qu'appliquer la langue (`AppliquerLangue`).
- Placé **après** l'onglet DataGrid (septième et dernier onglet). Au démarrage, l'onglet sélectionné et le focus restent ceux de l'onglet TextBox.

## 2. Disposition

Même grille que l'onglet Usuels (`Usuels.md`, section 2) : deux colonnes de largeur égale, le libellé à gauche (texte aligné à droite), le contrôle à droite. Les boutons n'ont pas de libellé : ils sont dans la colonne de droite, alignés à gauche, à taille naturelle.

| # | Libellé (colonne de gauche) | Contrôle (colonne de droite) | `x:Name` du contrôle |
|---|---|---|---|
| 1 | Sortie | `ComboBox` non éditable : Aucune, Événement de notification UIA, Région vivante, Focus | `CmbSortie` |
| 2 | Type de notification | `ComboBox` : `AutomationNotificationKind` | `CmbType` |
| 3 | Traitement | `ComboBox` : `AutomationNotificationProcessing` | `CmbTraitement` |
| 4 | Politesse | `ComboBox` : `AutomationLiveSetting` | `CmbPolitesse` |
| 5 | Message | `TextBox` d'une ligne | `TxtMessage` |
| 6 | *(vide)* | `Button` « Annoncer » | `BtnAnnoncer` |
| 7 | *(vide)* | `ToggleButton` « Répéter toutes les 5 secondes » | `TglRepeter` |
| 8 | *(vide)* | `Button` « Ouvrir une boîte de dialogue » | `BtnDialogue` |
| 9 | Dernière annonce | `TextBlock` (la région vivante, voir 3.3) | `TxtDerniere` |

- Libellés : `LblSortie`, `LblType`, `LblTraitement`, `LblPolitesse`, `LblMessage`, `LblDerniere`.
- Entrées de `CmbSortie` (`ComboBoxItem`) : `ItmSortieAucune`, `ItmSortieNotification`, `ItmSortieRegion`, `ItmSortieFocus`.
- Valeurs des listes de paramètres, **non traduites** (noms des énumérations de WPF, comme les types de la barre d'état) :

| Liste | Valeurs, dans cet ordre | Valeur au démarrage |
|---|---|---|
| `CmbType` | ItemAdded, ItemRemoved, ActionCompleted, ActionAborted, Other | Other |
| `CmbTraitement` | ImportantAll, ImportantMostRecent, All, MostRecent, CurrentThenMostRecent | ImportantAll |
| `CmbPolitesse` | Polite, Assertive | Polite |

Textes FR/EN : voir la section « Textes » en fin de fichier.

## 3. Comportement

### 3.1 Au démarrage

- Sortie : **Aucune**. Les trois listes de paramètres, « Annoncer » et « Répéter toutes les 5 secondes » sont **désactivés** (`IsEnabled="False"`), le bouton à bascule relâché.
- `TxtMessage` contient « Annonce de test ». Ce texte n'est pas retraduit au changement de langue : c'est une saisie.
- `TxtDerniere` est vide et n'est pas une région vivante.

### 3.2 Choix de la sortie

Seules les listes de la sortie choisie sont actives ; les autres sont **désactivées** (et non masquées : la mise en page ne bouge pas, NVDA les annonce « indisponible »). Leur valeur est conservée.

| Sortie | Listes actives | « Annoncer » et « Répéter » |
|---|---|---|
| Aucune | aucune | désactivés ; « Répéter » est relâché |
| Événement de notification UIA | Type de notification, Traitement | actifs |
| Région vivante | Politesse | actifs |
| Focus | aucune | actifs |

Le changement de sortie **désactive vraiment la sortie précédente** :

- en quittant **Région vivante**, `TxtDerniere` n'est plus une région vivante (`AutomationProperties.LiveSetting` effacé, valeur `Off`) ;
- en quittant **Notification** ou **Focus**, rien ne reste actif (ces sorties n'agissent qu'au moment de l'annonce) ;
- une répétition en cours continue avec la **nouvelle** sortie (sauf Aucune, qui l'arrête) : elle ne rejoue jamais l'ancienne.

Le gestionnaire `SelectionChanged` de `CmbSortie` est branché dans le constructeur (règle de `CLAUDE.md` : la sélection initiale le déclencherait pendant `InitializeComponent`).

### 3.3 Annoncer

« Annoncer » (souris, Entrée ou Espace) lit **à ce moment** la sortie, le texte de `TxtMessage` et les paramètres, puis :

1. recopie le message dans `TxtDerniere` (pour toutes les sorties sauf Aucune) ;
2. l'envoie par la sortie choisie :

| Sortie | Envoi |
|---|---|
| Événement de notification UIA | `RaiseNotificationEvent(type, traitement, message, "AccessTest.Annonce")`, **appelé par réflexion** (voir 6), sur le peer de la **fenêtre** : elle est toujours dans l'arbre UIA, même quand l'onglet Annonce n'est pas affiché. L'identifiant d'activité est fixe (utile pour les traitements `…MostRecent`, qui remplacent l'annonce précédente de même identifiant). |
| Région vivante | `AutomationProperties.LiveSetting` = la politesse choisie sur `TxtDerniere`, puis `RaiseAutomationEvent(AutomationEvents.LiveRegionChanged)` sur son peer. Le lecteur lit le **nom** de l'élément : pour un `TextBlock`, c'est son texte (aucun `AutomationProperties.Name` n'y est posé). |
| Focus | Focus clavier sur `TxtMessage`, curseur à la fin du texte. Le lecteur annonce le champ (nom « Message », rôle, contenu). |

- Message vide : il est envoyé tel quel (cas à observer), sauf pour la région vivante où un nom vide n'a rien à lire.
- Le même message envoyé deux fois est **renvoyé tel quel** : on observe si le lecteur le relit (en particulier pour la région vivante, dont le texte ne change pas).

### 3.4 Répéter toutes les 5 secondes

- Bouton à bascule : enfoncé, il annonce **tout de suite** (comme « Annoncer »), puis toutes les **5 secondes** (`DispatcherTimer`), jusqu'à ce qu'on le relâche ou qu'on choisisse la sortie Aucune.
- Chaque répétition relit la sortie, le texte et les paramètres en cours : on peut les changer pendant qu'elle tourne.
- La répétition **continue quand on change d'onglet** (pour observer si une annonce interrompt la lecture ailleurs), **sauf pour la sortie Focus** : celle-ci n'agit que si l'onglet Annonce est l'onglet sélectionné ; dans un autre onglet, les répétitions Focus sont ignorées (on ne vole pas le focus aux autres onglets).
- **Limites attendues**, à constater avec NVDA :
  - **Focus** : quand `TxtMessage` a déjà le focus, le lui redonner ne déclenche aucun événement ; les répétitions suivantes ne disent probablement rien.
  - **Région vivante** hors de l'onglet : le `TabControl` retire le contenu des onglets non affichés de l'arbre visuel, donc `TxtDerniere` n'est plus dans l'arbre UIA ; l'événement est levé quand même. **Constaté** (utilisateur, NVDA, 2026-11-16) : NVDA dit régulièrement « Annonce », le nom de l'onglet, au lieu du message. Comportement **conservé tel quel** et seulement noté (application de test, pas de correction) ; voir `Docs/Constats/Fenetre-Annonces.md`.

### 3.5 Ouvrir une boîte de dialogue

- « Ouvrir une boîte de dialogue » ouvre une boîte de dialogue **modale** standard (`MessageBox`, bouton OK, propriétaire : la fenêtre principale), pour observer les annonces pendant qu'elle est ouverte. Titre et texte : la section « Textes » en fin de fichier.
- Le bouton est **toujours actif**, quelle que soit la sortie (on lance d'abord la répétition, puis on ouvre la boîte).
- La répétition continue pendant que la boîte est ouverte : le `DispatcherTimer` tourne aussi pendant sa boucle modale. Rien n'est bloqué ni adapté pour la boîte : l'application de test note seulement le comportement.
- **Constaté** (utilisateur, NVDA, 2026-11-16) : boîte ouverte, la notification (levée sur la fenêtre principale, qui n'est plus la fenêtre active) et la région vivante (élément affiché derrière la boîte) sont lues à chaque répétition. À observer encore : Avec la sortie Focus, le focus est-il pris dans la fenêtre principale, désactivée par la boîte modale, et que devient la boîte ?
- À la fermeture (OK, Entrée ou Échap), le focus revient au bouton « Ouvrir une boîte de dialogue ».

## 4. Accessibilité

- Chaque liste et `TxtMessage` portent leur nom accessible (`AutomationProperties.Name`), identique au libellé de leur ligne. Les deux `Button` et le `ToggleButton` tirent leur nom de leur texte visible.
- `TxtDerniere` n'a **pas** de nom accessible posé : son nom est son texte (c'est ce que lit la région vivante). Son libellé « Dernière annonce » est un `TextBlock` voisin, non lié.
- Ordre de tabulation : Sortie, Type de notification, Traitement, Politesse (seulement les listes actives), Message, Annoncer, Répéter, Ouvrir une boîte de dialogue. `TxtDerniere` n'est pas focalisable.
- Pas de `ControlTemplate` personnalisé (règle 6 de `Specs.md`).

## 5. Menu et barre d'état avec cet onglet

- `TxtMessage` garde le menu contextuel natif.
- **Édition** : s'applique au contrôle focalisé, sans traitement particulier.
- **Barre d'état** : le champ 1 (position) est vide ; le champ 2 donne le type WPF du contrôle focalisé : `ComboBox`, `TextBox`, `Button`, `ToggleButton`.

## 6. Lecture par un lecteur d'écran

- Types de contrôle UIA attendus : `ComboBox` (×4), `Edit`, `Button` (×2), `Button` avec le motif `Toggle` (bouton à bascule), `Text` (dernière annonce).
- Points de `ChecklistNVDA.md` qui concernent cet onglet : 6, 30 à 34.
- **Compatibilité .NET Framework 4.8** (Q15, réponse de l'utilisateur, 2026-11-16) : `RaiseNotificationEvent` et les énumérations `AutomationNotificationKind` / `AutomationNotificationProcessing` sont dans le runtime 4.8 installé, mais **absents de ses assemblys de référence** (la compilation `net48` les refuse, essai du 2026-11-16). Pour garder les mêmes sources, la méthode est **appelée par réflexion** (types cherchés dans l'assembly `UIAutomationTypes`, celui de `ToggleState`) ; les listes Type et Traitement contiennent les noms des valeurs (texte), convertis par `Enum.Parse` à l'annonce. Si la méthode ou les types sont introuvables, l'entrée « Événement de notification UIA » de la liste Sortie est **désactivée**.

## Textes

Textes FR/EN de l'onglet (`Textes.cs` les reprend ; le menu et la barre d'état sont dans `Fenetre.md`, section 5).

| Élément | FR | EN |
|---|---|---|
| Annonce : sortie (libellé et nom accessible) | Sortie | Output |
| Annonce : entrées de la sortie | Aucune / Événement de notification UIA / Région vivante / Focus | None / UIA notification event / Live region / Focus |
| Annonce : type de notification (libellé et nom accessible) | Type de notification | Notification kind |
| Annonce : traitement (libellé et nom accessible) | Traitement | Processing |
| Annonce : politesse (libellé et nom accessible) | Politesse | Politeness |
| Annonce : valeurs des trois listes de paramètres | noms des énumérations de WPF, non traduits (`Annonce.md`, 2) | idem |
| Annonce : message (libellé et nom accessible) | Message | Message |
| Annonce : texte initial du message (non retraduit) | Annonce de test | Annonce de test |
| Annonce : bouton | Annoncer | Announce |
| Annonce : bouton à bascule | Répéter toutes les 5 secondes | Repeat every 5 seconds |
| Annonce : libellé de la dernière annonce | Dernière annonce | Last announcement |
| Annonce : bouton de la boîte de dialogue | Ouvrir une boîte de dialogue | Open a dialog box |
| Annonce : titre de la boîte de dialogue | Boîte de dialogue | Dialog box |
| Annonce : texte de la boîte de dialogue | Boîte de dialogue de test : les annonces répétées continuent pendant qu'elle est ouverte. | Test dialog box: repeated announcements continue while it is open. |
