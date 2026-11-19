# Constats — Sources Microsoft : Accessibility Insights et Guy Barker

Constats d'essai (UIA, lecteur d'écran) repris de `Docs/Notes.md`, section 5, sans changement de contenu. Retour à l'index : [`Notes.md`](../Notes.md).

## Autres contrôles corrigés par Microsoft (Accessibility Insights, Guy Barker)

Recherche du 2026-11-15. Dossier des contrôles dérivés d'Accessibility Insights for Windows : [`SharedUx/Controls/CustomControls`](https://github.com/microsoft/accessibility-insights-windows/tree/main/src/AccessibilityInsights.SharedUx/Controls/CustomControls). Hors du `TreeView` à cocher ([`Arbre-Grille-Calendrier.md`](Arbre-Grille-Calendrier.md)), il contient :

| Classe | Défaut corrigé | Intérêt pour AccessTest |
|---|---|---|
| `CustomDataGrid : DataGrid` | Tab parcourt les cellules une à une : piège de tabulation. Le `DataGrid` devient un seul arrêt (`TabNavigation="Once"`) et Tab / Maj+Tab sortent du contrôle (`MoveFocus` répété dans `PreviewKeyDown`). | Si un onglet `DataGrid` est ajouté. Même famille que le piège Maj+Tab du `Calendar` (onglet Choix). |
| `CustomExpander : Expander` | En-tête de groupe d'un `ListView` groupé (`Expander` dans un `GroupItem`) : le focus n'est pas signalé en .NET 4.8. Le peer renvoie l'état de focus du `GroupItem` parent (`HasKeyboardFocusCore`). | Pour `Net48`, si une liste groupée est testée. |
| `ToggleMenuItem` + peer | Item de menu dont l'en-tête est un `RadioButton` : ajout du motif `Toggle` au `MenuItem`. | Aucun : nos items à cocher (`IsCheckable`) ont déjà le motif `Toggle` natif. |
| `CustomToggleButton` | Peer supprimé (`OnCreateAutomationPeer` renvoie `null`) : bouton retiré de l'arbre UIA (élément redondant dans leur interface). | Aucun. |
| `CustomListView` | Molette de la souris transmise au conteneur parent (défilement d'une liste imbriquée). Pas un sujet de lecteur d'écran. | Aucun. |
| `CustomGridViewColumnHeader` | Rien pour l'instant : peer préparé « au cas où » (retrait du motif `Invoke` laissé en commentaire). | Aucun. |

Pas d'autre article de ce blog sur un contrôle WPF trouvé (le `TreeView` à cocher est le seul). Microsoft ne fournit pas ces classes comme bibliothèque : elles sont internes à l'outil (licence MIT, réutilisables en citant la source).

Article de référence plus général, de Guy Barker (équipe UIA de Microsoft, 2017) : [Common approaches for enhancing the programmatic accessibility of your Win32, WinForms and WPF apps: Part 4 – WPF](https://learn.microsoft.com/en-us/archive/blogs/winuiautomation/common-approaches-for-enhancing-the-programmatic-accessibility-of-your-win32-winforms-and-wpf-apps-part-4-wpf). Points utiles :
- Ajouter un motif à un contrôle : peer dérivé, `GetPattern`, implémentation, et `RaisePropertyChangedEvent` à chaque changement (même méthode que `CorrectifArbreCases`). Exemples : motif `Value` sur un bouton, motif `Invoke` sur les items d'un `ListView` (`CreateItemAutomationPeer`).
- Nom d'un `ListViewItem` : passer par `ToString()` du modèle (rejoint notre constat sur `ListView` + `GridView`). Selon lui, lier `AutomationProperties.Name` dans un `ItemContainerStyle` n'est pas pris en charge pour ces items.
- Tri d'une colonne de `DataGrid` / `GridView` : rendre l'en-tête focalisable (`ColumnHeaderStyle`, `Focusable="True"`), et donner l'ordre de tri par `AutomationProperties.ItemStatus` + `RaisePropertyChangedEvent(ItemStatusProperty)`.
- Cellule qui ne montre qu'une image : nom par un `CellStyle` (`AutomationProperties.Name` lié, avec convertisseur).
- `LocalizedControlType` d'un contrôle personnalisé, retrait d'un conteneur inutile de l'arbre (`IsControlElementCore`).
- Mise à jour de 2018 : régions vivantes (`LiveSetting`) disponibles en WPF depuis .NET Framework 4.7.1, et événements de notification UIA. À garder en tête pour la barre d'état, qui n'est pas une région vivante (choix de `Fenetre.md`, section 4).
