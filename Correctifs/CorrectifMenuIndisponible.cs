// =====================================================================================================
// CorrectifMenuIndisponible.cs — correctif réutilisable : un item de menu WPF grisé reste atteignable au clavier.
//
// DÉPENDANCE OBLIGATOIRE : Harmony (bibliothèque de patch de méthodes .NET à l'exécution)
//   - paquet NuGet : Lib.Harmony, version 2.4.3 (testée)
//   - licence : MIT — https://github.com/pardeike/Harmony
//   Voir l'en-tête de CorrectifLigneVide.cs pour les détails.
//
// CIBLES : .NET 10 (net10.0-windows) et .NET Framework 4.8 (net48).
//   Fichier autonome : pas de using implicites, pas de syntaxe C# récente (compile en C# 7.3).
//
// DÉFAUT CORRIGÉ : un MenuItem désactivé (IsEnabled = false, ou commande dont CanExecute est faux) est grisé ET
//   non focalisable : les flèches le sautent et un lecteur d'écran ne l'annonce jamais. L'utilisateur ne sait
//   pas que la commande existe. Constaté avec NVDA, .NET Framework 4.8 et .NET 10, novembre 2026.
//   Ailleurs : un menu Win32 (Bloc-notes) s'arrête sur un item grisé, annoncé « indisponible » ; Microsoft a
//   corrigé le même défaut dans WinForms (.NET Framework 4.7.1, ToolStripMenuItem) ; le W3C (ARIA Authoring
//   Practices) demande que les items de menu désactivés restent focalisables.
//
// MOYEN : le code de WPF sépare l'état désactivé (IsEnabled) du refus du focus, fait par trois tests internes.
//   Le correctif ne change que ces tests, et seulement pour un MenuItem désactivé, visible et focalisable :
//   - Keyboard.IsFocusable (PresentationCore) : le focus clavier est accepté (KeyboardDevice ne le refuse plus,
//     et ne le retire plus quand l'item devient désactivé) ;
//   - KeyboardNavigation.IsTabStop et KeyboardNavigation.IsFocusableInternal (PresentationFramework) : l'item
//     redevient candidat pour la navigation aux flèches dans le menu ;
//   - MenuItem.ClickItem : ignoré tant que l'item est désactivé (Entrée, touche d'accès) ; le menu reste ouvert.
//   Tout le reste est le comportement de WPF : IsEnabled reste faux à la lecture, le grisé vient du thème, le
//   peer UIA déclare IsEnabled faux (« indisponible ») et refuse Invoke, la commande n'est pas exécutée.
//   Une première version (classe dérivée de MenuItem) marchait aussi, mais imposait de changer le type de
//   chaque item et de ne plus utiliser IsEnabled.
//
// UTILISATION :
//   CorrectifsWpf.CorrectifMenuIndisponible.Activer();     // lève une exception si le code de WPF a changé
//   CorrectifsWpf.CorrectifMenuIndisponible.Desactiver();  // rétablit le comportement d'origine
//   Le patch agit sur tous les MenuItem du processus, en mémoire seulement. À retester à chaque version de .NET.
// =====================================================================================================

using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HarmonyLib;

namespace CorrectifsWpf
{
    /// <summary>Les items de menu désactivés restent atteignables au clavier et annoncés « indisponible ».</summary>
    public static class CorrectifMenuIndisponible
    {
        private const string IdHarmony = "CorrectifsWpf.CorrectifMenuIndisponible";
        private static readonly Harmony harmony = new Harmony(IdHarmony);

        public static bool Actif { get; private set; }

        public static void Activer()
        {
            if (Actif)
            {
                return;
            }

            MethodInfo focalisable = Trouver(typeof(Keyboard), "IsFocusable", typeof(DependencyObject));
            MethodInfo arret = Trouver(typeof(KeyboardNavigation), "IsTabStop", typeof(DependencyObject));
            MethodInfo focalisableInterne = Trouver(typeof(KeyboardNavigation), "IsFocusableInternal", typeof(DependencyObject));
            MethodInfo clic = Trouver(typeof(MenuItem), "ClickItem", typeof(bool));

            try
            {
                HarmonyMethod apresFocalisable = new HarmonyMethod(typeof(CorrectifMenuIndisponible), nameof(ApresFocalisable));
                harmony.Patch(focalisable, postfix: apresFocalisable);
                harmony.Patch(focalisableInterne, postfix: apresFocalisable);
                harmony.Patch(arret, postfix: new HarmonyMethod(typeof(CorrectifMenuIndisponible), nameof(ApresArret)));
                harmony.Patch(clic, prefix: new HarmonyMethod(typeof(CorrectifMenuIndisponible), nameof(AvantClic)));
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

        private static MethodInfo Trouver(Type type, string nom, Type parametre)
        {
            MethodInfo methode = AccessTools.Method(type, nom, new[] { parametre });
            if (methode == null || methode.ReturnType != (nom == "ClickItem" ? typeof(void) : typeof(bool)))
            {
                throw new InvalidOperationException(type.Name + "." + nom + " introuvable (code interne de WPF différent).");
            }
            return methode;
        }

        // Un MenuItem désactivé qui serait focalisable s'il était activé.
        private static bool Atteignable(DependencyObject element)
        {
            MenuItem item = element as MenuItem;
            return item != null && !item.IsEnabled && item.Focusable && item.IsVisible;
        }

        // Keyboard.IsFocusable et KeyboardNavigation.IsFocusableInternal : __0 = l'élément testé.
        private static void ApresFocalisable(DependencyObject __0, ref bool __result)
        {
            if (!__result && Atteignable(__0))
            {
                __result = true;
            }
        }

        // KeyboardNavigation.IsTabStop : même chose, si l'item est aussi un arrêt de tabulation (IsTabStop).
        private static void ApresArret(DependencyObject __0, ref bool __result)
        {
            if (!__result && Atteignable(__0) && ((MenuItem)__0).IsTabStop)
            {
                __result = true;
            }
        }

        // MenuItem.ClickItem : rien ne se passe sur un item désactivé, et le menu reste ouvert.
        private static bool AvantClic(MenuItem __instance)
        {
            return __instance.IsEnabled;
        }
    }
}
