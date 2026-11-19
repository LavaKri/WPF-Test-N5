// =====================================================================================================
// CorrectifRaccourciMenu.cs — correctif réutilisable : le raccourci d'un item de menu WPF n'est jamais annoncé,
// et un bouton lié à une commande annonce le texte anglais de la commande comme raccourci.
//
// DÉPENDANCE OBLIGATOIRE : Harmony (bibliothèque de patch de méthodes .NET à l'exécution)
//   - paquet NuGet : Lib.Harmony, version 2.4.3 (testée)
//   - licence : MIT — https://github.com/pardeike/Harmony
//   Voir l'en-tête de CorrectifLigneVide.cs pour les détails.
//
// CIBLES : .NET 10 (net10.0-windows) et .NET Framework 4.8 (net48).
//   Fichier autonome : pas de using implicites, pas de syntaxe C# récente (compile en C# 7.3).
//
// DÉFAUTS CORRIGÉS (constatés avec NVDA, .NET 10 et .NET Framework 4.8, novembre 2026) :
//   1. Le raccourci affiché à droite d'un MenuItem (InputGestureText, « Ctrl+O ») n'est jamais annoncé.
//      Code de WPF : MenuItemAutomationPeer ne redéfinit pas GetAcceleratorKeyCore ; la propriété UIA
//      AcceleratorKey reste vide. NVDA lit pourtant AcceleratorKey. Microsoft a corrigé le même défaut dans
//      WinForms (.NET Framework 4.7.2, ToolStripMenuItem) ; pas dans WPF.
//   2. Un bouton (Button, ToggleButton, RadioButton, CheckBox) lié à une RoutedUICommand, sans AcceleratorKey,
//      expose le TEXTE de la commande (« Toggle bold », non traduit) comme raccourci
//      (ButtonBaseAutomationPeer.GetAcceleratorKeyCore) ; NVDA le lit comme un raccourci.
//
// MOYEN : deux postfix Harmony, sur des méthodes protégées documentées de WPF :
//   - UIElementAutomationPeer.GetAcceleratorKeyCore : résultat vide et propriétaire MenuItem -> son
//     InputGestureText (le texte affiché) ;
//   - ButtonBaseAutomationPeer.GetAcceleratorKeyCore : résultat égal au texte de la commande (repli de WPF) ->
//     premier raccourci de la commande (InputGestures), ou rien.
//   Aucune propriété des éléments n'est modifiée ; une valeur posée par l'application
//   (AutomationProperties.AcceleratorKey) garde la priorité.
//   Une première version liait AutomationProperties.AcceleratorKey de chaque item à son InputGestureText
//   (gestionnaires de classe) : elle marchait, mais modifiait une propriété publique de l'item.
//
// UTILISATION :
//   CorrectifsWpf.CorrectifRaccourciMenu.Activer();     // lève une exception si le code de WPF a changé
//   CorrectifsWpf.CorrectifRaccourciMenu.Desactiver();  // rétablit le comportement d'origine
//   Le patch agit sur tout le processus, en mémoire seulement. À retester à chaque version de .NET.
// =====================================================================================================

using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using HarmonyLib;

namespace CorrectifsWpf
{
    /// <summary>Raccourcis exposés aux lecteurs d'écran : items de menu, et boutons liés à une commande.</summary>
    public static class CorrectifRaccourciMenu
    {
        private const string IdHarmony = "CorrectifsWpf.CorrectifRaccourciMenu";
        private static readonly Harmony harmony = new Harmony(IdHarmony);

        public static bool Actif { get; private set; }

        public static void Activer()
        {
            if (Actif)
            {
                return;
            }

            MethodInfo element = Trouver(typeof(UIElementAutomationPeer));
            MethodInfo bouton = Trouver(typeof(ButtonBaseAutomationPeer));

            try
            {
                harmony.Patch(element, postfix: new HarmonyMethod(typeof(CorrectifRaccourciMenu), nameof(ApresElement)));
                harmony.Patch(bouton, postfix: new HarmonyMethod(typeof(CorrectifRaccourciMenu), nameof(ApresBouton)));
            }
            catch
            {
                harmony.UnpatchAll(IdHarmony);
                throw;
            }
            Actif = true;
        }

        public static void Desactiver()
        {
            if (!Actif)
            {
                return;
            }
            harmony.UnpatchAll(IdHarmony);
            Actif = false;
        }

        // La méthode doit être déclarée par la classe elle-même (sinon le patch toucherait une autre classe).
        private static MethodInfo Trouver(Type type)
        {
            MethodInfo methode = AccessTools.DeclaredMethod(type, "GetAcceleratorKeyCore", Type.EmptyTypes);
            if (methode == null || methode.ReturnType != typeof(string))
            {
                throw new InvalidOperationException(type.Name + ".GetAcceleratorKeyCore introuvable (code de WPF différent).");
            }
            return methode;
        }

        // Item de menu : le raccourci affiché, si ni l'application ni WPF n'en donnent un.
        private static void ApresElement(UIElementAutomationPeer __instance, ref string __result)
        {
            if (string.IsNullOrEmpty(__result))
            {
                MenuItem item = __instance.Owner as MenuItem;
                if (item != null)
                {
                    __result = item.InputGestureText ?? string.Empty;
                }
            }
        }

        // Bouton : WPF met le texte de la commande à la place d'un raccourci absent ; on met le vrai raccourci.
        private static void ApresBouton(ButtonBaseAutomationPeer __instance, ref string __result)
        {
            ButtonBase bouton = (ButtonBase)__instance.Owner;
            RoutedUICommand commande = bouton.Command as RoutedUICommand;
            if (commande == null || string.IsNullOrEmpty(__result) || __result != commande.Text
                || !string.IsNullOrEmpty(AutomationProperties.GetAcceleratorKey(bouton)))
            {
                return;
            }
            string raccourci = string.Empty;
            foreach (InputGesture geste in commande.InputGestures)
            {
                KeyGesture touche = geste as KeyGesture;
                if (touche != null)
                {
                    raccourci = touche.GetDisplayStringForCulture(CultureInfo.CurrentUICulture);
                    break;
                }
            }
            __result = raccourci;
        }
    }
}
