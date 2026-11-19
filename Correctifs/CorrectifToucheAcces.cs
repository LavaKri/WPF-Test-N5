// =====================================================================================================
// CorrectifToucheAcces.cs — correctif réutilisable : la touche d'accès d'un contrôle WPF est exposée aux
// lecteurs d'écran par la lettre seule (« I »), et AutomationProperties.AccessKey est ignorée.
//
// DÉPENDANCE OBLIGATOIRE : Harmony (bibliothèque de patch de méthodes .NET à l'exécution)
//   - paquet NuGet : Lib.Harmony, version 2.4.3 (testée)
//   - licence : MIT — https://github.com/pardeike/Harmony
//   Voir l'en-tête de CorrectifLigneVide.cs pour les détails.
//
// CIBLES : .NET 10 (net10.0-windows) et .NET Framework 4.8 (net48).
//   Fichier autonome : pas de using implicites, pas de syntaxe C# récente (compile en C# 7.3).
//
// DÉFAUTS CORRIGÉS (constatés avec NVDA et par relevé UIA, .NET 10, novembre 2026) :
//   1. Un champ désigné par un Label (« _Identifiant », Target) expose la propriété UIA AccessKey « I » :
//      NVDA dit « i » seul en fin d'annonce, facile à manquer. L'usage de Windows est « Alt+I ».
//   2. AutomationProperties.AccessKey posée par l'application est ignorée : la propriété UIA devient vide.
//      Comportement observé de UIElementAutomationPeer.GetAccessKeyCore : caractère de l'AccessKeyManager
//      si AutomationProperties.AccessKey est vide, chaîne vide sinon.
//
// MOYEN : un postfix Harmony de UIElementAutomationPeer.GetAccessKeyCore (méthode protégée documentée), sauf
//   pour un MenuItem (MenuItemAutomationPeer a sa propre règle et ajoute déjà « Alt+ » au premier niveau) :
//   - valeur posée par l'application (AutomationProperties.AccessKey) -> rendue telle quelle ;
//   - sinon, lettre seule rendue par WPF -> « Alt+ » et la lettre en majuscule.
//
// UTILISATION :
//   CorrectifsWpf.CorrectifToucheAcces.Activer();     // lève une exception si le code de WPF a changé
//   CorrectifsWpf.CorrectifToucheAcces.Desactiver();  // rétablit le comportement d'origine
//   Le patch agit sur tout le processus, en mémoire seulement. À retester à chaque version de .NET.
// =====================================================================================================

using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using HarmonyLib;

namespace CorrectifsWpf
{
    /// <summary>Touches d'accès exposées aux lecteurs d'écran sous la forme « Alt+I ».</summary>
    public static class CorrectifToucheAcces
    {
        private const string IdHarmony = "CorrectifsWpf.CorrectifToucheAcces";
        private static readonly Harmony harmony = new Harmony(IdHarmony);

        public static bool Actif { get; private set; }

        public static void Activer()
        {
            if (Actif)
            {
                return;
            }

            // La méthode doit être déclarée par UIElementAutomationPeer (sinon le patch toucherait une autre classe).
            MethodInfo methode = AccessTools.DeclaredMethod(typeof(UIElementAutomationPeer), "GetAccessKeyCore", Type.EmptyTypes);
            if (methode == null || methode.ReturnType != typeof(string))
            {
                throw new InvalidOperationException("UIElementAutomationPeer.GetAccessKeyCore introuvable (code de WPF différent).");
            }

            try
            {
                harmony.Patch(methode, postfix: new HarmonyMethod(typeof(CorrectifToucheAcces), nameof(Apres)));
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

        private static void Apres(UIElementAutomationPeer __instance, ref string __result)
        {
            if (__instance.Owner is MenuItem)
            {
                return;
            }

            string posee = AutomationProperties.GetAccessKey(__instance.Owner);
            if (!string.IsNullOrEmpty(posee))
            {
                __result = posee;
            }
            else if (__result != null && __result.Length == 1)
            {
                __result = "Alt+" + __result.ToUpper(CultureInfo.CurrentCulture);
            }
        }
    }
}
