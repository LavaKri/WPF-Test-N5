# Constats — Fenêtre et annonces : barre d'état, région vivante, ProgressBar

Constats d'essai (UIA, lecteur d'écran) repris de `Docs/Notes.md`, section 5, sans changement de contenu. Retour à l'index : [`Notes.md`](../Notes.md).

## Barre d'état introuvable par NVDA (NVDA+Fin)

Constat de l'utilisateur (2026-11-13, .NET 10) : NVDA ne trouve pas la barre d'état.

- **Algorithme de NVDA** ([`source/api.py`](https://github.com/nvaccess/nvda/blob/master/source/api.py), `getStatusBar`) : 1) `appModule.statusBar` du module d'application, s'il existe ; 2) sinon, objet situé au point `(gauche, haut + hauteur - 1)` du rectangle (`location`) de la fenêtre au premier plan ; 3) remontée des parents jusqu'à un rôle `STATUSBAR`. Si rien n'est trouvé, `_getStatusBarText` (`globalCommands.py`) lit la dernière ligne de la revue à plat. NVDA **ne parcourt pas** l'arbre UIA pour chercher la barre d'état. Texte lu (`getStatusBarText`) : nom de la barre, puis nom et valeur de chaque enfant.
- **Relevé UIA** (`AutomationElement.FromPoint` au point de NVDA, écran 1024 × 768) : le rectangle UIA de la fenêtre WPF est celui de la fenêtre entière, cadre compris.
  - Maximisée : fenêtre `-8,-8,1040,736`, point `(-8, 727)`, hors de l'écran → la fenêtre, puis le bureau. La `StatusBar` est en `0,698,1024,22`.
  - Normale : fenêtre `50,50,800,500`, point `(50, 549)`, dans la bordure invisible de 8 px → la fenêtre. La `StatusBar` est en `58,520,784,22`. Si la fenêtre chevauche la barre des tâches, le point tombe même sur la barre des tâches (`Shell_TrayWnd`).
- Même cas que le Bloc-notes de Windows 11, que NVDA règle par un module d'application ([`appModules/notepad.py`](https://github.com/nvaccess/nvda/blob/master/source/appModules/notepad.py), `_get_statusBar` qui cherche la barre dans l'arbre UIA).
- **Correctif retenu et fait** (menu Correctif, `Correctifs/CorrectifBarreEtat.cs`) : peer de fenêtre dérivé de `WindowAutomationPeer` dont `GetBoundingRectangleCore` renvoie la zone cliente (`GetClientRect` + `ClientToScreen`). Le rectangle fourni par le peer WPF remplace bien celui de la fenêtre côté UIA. Relevé avec le correctif : maximisée `0,23,1024,697`, point `(0, 719)` ; normale `58,81,784,461`, point `(58, 541)` ; dans les deux cas le point tombe sur la `StatusBar`, dont les enfants donnent « Position du curseur : Ligne 1, colonne 1 » et « Type de contrôle : TextBox ». Le correctif s'active et se retire pendant l'exécution (`Actif` lu à chaque demande).
- **Test NVDA de l'utilisateur** (2026-11-13) : correctif efficace en fenêtre maximisée, sans effet en fenêtre restaurée. Cause, reproduite par relevé UIA : la position par défaut d'une fenêtre restaurée avance de 26 px à chaque lancement (`26,26`, `52,52`… `130,130`) ; sur cet écran (zone de travail 1024 × 720), une fenêtre de 900 × 600 placée à partir de `y = 130` déborde sous la barre des tâches. Le point visé `(138, 721)` tombe sur la barre des tâches, qui cache les deux dernières lignes de la `StatusBar` (`138,700,884,22`). NVDA relit le rectangle à chaque fois (`getCurrentPropertyValueEx`, `BoundingRectangle` absent de son cache de base) : pas de problème de cache.
- Correction : le rectangle annoncé est limité à la zone de travail (`MonitorFromWindow` + `GetMonitorInfo`, `rcWork`). Relevés : restaurée en `130,130` → point `(138, 719)`, barre trouvée ; débord à gauche en `-200,100` → point `(0, 691)`, barre trouvée ; en `-200,150`, la barre est entièrement sous la barre des tâches → non trouvée (normal, elle est invisible).
- Effet de bord : quand le correctif est actif, le rectangle UIA de la fenêtre exclut barre de titre, bordures et partie hors de la zone de travail (cadre de surbrillance de Narrateur, par exemple). La barre de titre reste exposée par Windows.
- Piège rencontré : le `TogglePattern` UIA d'un `MenuItem` à cocher change `IsChecked` **sans** déclencher `Click`. Le menu du correctif est donc branché sur `Checked` / `Unchecked`.
- Autre voie, non retenue : un module d'application NVDA `accesstest.py` (dossier `scratchpad\appModules`, avec le *scratchpad* activé dans les paramètres avancés de NVDA) qui fournit `statusBar`. Fiable, mais la correction serait du côté de NVDA et non de l'application.
- Confirmé avec NVDA en fenêtre maximisée. **À confirmer** en fenêtre restaurée après la limite à la zone de travail (point 14 de la checklist). Non testé en .NET Framework 4.8.

## Région vivante dans un onglet non affiché

Constat de l'utilisateur (NVDA, .NET 10, 2026-11-16), onglet Annonce, sortie Région vivante en répétition : dans l'onglet Annonce, le message est lu ; dans un autre onglet, NVDA dit régulièrement « Annonce » (le nom de l'onglet) au lieu du message.

- Cause probable, non vérifiée dans le code de WPF : le `TabControl` n'affiche que le contenu de l'onglet sélectionné. Le `TextBlock` de la région vivante n'est plus dans l'arbre visuel, donc plus dans l'arbre UIA ; l'événement `LiveRegionChanged` levé sur son peer semble attribué à l'ancêtre encore présent, le `TabItem`. NVDA lit le nom de l'élément de l'événement.
- À retenir pour une vraie application : ne lever une région vivante que sur un élément affiché ; pour une annonce qui doit être entendue partout, l'événement de notification UIA (levé sur la fenêtre) fonctionne dans tous les onglets, même menu ouvert (point 31).
- Choix d'AccessTest : comportement conservé et noté, pas de correction (`Specs/Annonce.md`, 3.4).

## `ProgressBar` : pas de texte, valeur non lue par JAWS

Constat de l'utilisateur (2026-11-19) : JAWS ne lit pas la progression de la `ProgressBar` de l'onglet Usuels. Un `ProgressBar` WPF n'affiche aucun texte ; son peer (`ProgressBarAutomationPeer`) n'expose que le motif `RangeValue` (valeur, minimum, maximum, lecture seule). L'application ajoute donc le pourcentage en chiffres dans un `TextBlock` voisin (`TblPourcentage`), sans annonce forcée (`Specs/Usuels.md`, 3.3). Lecture de ce texte par JAWS et NVDA : à vérifier.
