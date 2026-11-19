// =====================================================================================================
// CorrectifLigneVide.cs — correctif réutilisable des lignes vides lues de travers par les lecteurs d'écran
// dans les contrôles texte WPF (TextBox, RichTextBox).
//
// DÉPENDANCE OBLIGATOIRE : Harmony (bibliothèque de patch de méthodes .NET à l'exécution)
//   - paquet NuGet : Lib.Harmony, version 2.4.3 (testée)
//       dotnet add package Lib.Harmony --version 2.4.3
//     ou, sans NuGet : référencer 0Harmony.dll (fournie dans le paquet) et livrer son fichier LICENSE avec.
//   - licence : MIT — https://github.com/pardeike/Harmony
//   - Harmony embarque MonoMod.Core (détour des méthodes en mémoire).
//
// CIBLES TESTÉES : .NET 10 (net10.0-windows) et .NET Framework 4.8 (net48).
//   Fichier autonome : pas de using implicites, pas de syntaxe C# récente (compile en C# 7.3).
//
// DÉFAUT CORRIGÉ : bug WPF https://github.com/dotnet/wpf/issues/7190 (non corrigé par Microsoft).
//   Sur une ligne vide, NVDA et Narrateur lisent la ligne du dessous, puis la relisent en arrivant dessus.
//   Cause : TextRangeAdaptor.ExpandToEnclosingUnit (unité Line), méthode interne de PresentationFramework :
//   la plage de la ligne vide déborde sur la ligne suivante. La condition d'origine devient :
//     if (lineRange.Contains(_end) || lineRange.Start.CompareTo(lineRange.End) == 0 || expansionLigne)
//   - la deuxième partie vient de la PR https://github.com/dotnet/wpf/pull/11509 (suffit pour TextBox) ;
//   - la troisième couvre le RichTextBox, où un paragraphe vide ne donne pas une plage vide : pendant un
//     appel UIA ExpandToEnclosingUnit(Line), WPF réduit d'abord la plage à un caractère après son début,
//     la bonne réponse est donc toujours la ligne qui contient ce début.
//   Validé avec NVDA (TextBox et RichTextBox) et Narrateur (TextBox), .NET 10, novembre 2026.
//
// UTILISATION :
//   CorrectifsWpf.CorrectifLigneVide.Activer();     // une fois, au démarrage (lève une exception si échec)
//   CorrectifsWpf.CorrectifLigneVide.Desactiver();  // facultatif : rétablit le code d'origine de WPF
//   Le patch agit sur tous les contrôles texte du processus, en mémoire seulement : aucun fichier de WPF
//   n'est modifié, les autres applications ne sont pas touchées.
//
// SI LE CODE INTERNE DE WPF CHANGE (nouvelle version de .NET) : Activer() lève InvalidOperationException
//   (ou HarmonyException) avec la raison, rien n'est patché, l'application garde le comportement natif.
//   À retester à chaque version majeure de .NET.
// =====================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Automation.Text;
using System.Windows.Controls;
using HarmonyLib;

namespace CorrectifsWpf
{
    /// <summary>Correctif des lignes vides lues de travers par les lecteurs d'écran (dotnet/wpf#7190).</summary>
    public static class CorrectifLigneVide
    {
        private const string IdHarmony = "CorrectifsWpf.CorrectifLigneVide";
        private static readonly Harmony harmony = new Harmony(IdHarmony);

        // Vrai pendant un appel UIA ExpandToEnclosingUnit(Line). WPF y réduit d'abord la plage à un caractère
        // après son début : la bonne réponse est donc toujours la ligne qui contient ce début.
        [ThreadStatic]
        private static bool expansionLigne;

        public static bool Actif { get; private set; }

        /// <summary>Applique le patch. Lève une exception avec la raison si le code interne de WPF ne correspond pas.</summary>
        public static void Activer()
        {
            if (Actif)
            {
                return;
            }

            Type type = typeof(TextBox).Assembly.GetTypes().FirstOrDefault(t => t.Name == "TextRangeAdaptor");
            if (type == null)
            {
                throw new InvalidOperationException("classe TextRangeAdaptor introuvable");
            }
            MethodInfo interne = AccessTools.Method(type, "ExpandToEnclosingUnit",
                                                    new[] { typeof(TextUnit), typeof(bool), typeof(bool) });
            MethodInfo publique = AccessTools.GetDeclaredMethods(type)
                                             .FirstOrDefault(m => m.Name.EndsWith("ITextRangeProvider.ExpandToEnclosingUnit", StringComparison.Ordinal));
            if (interne == null || publique == null)
            {
                throw new InvalidOperationException("méthode TextRangeAdaptor.ExpandToEnclosingUnit introuvable");
            }

            try
            {
                harmony.Patch(interne, transpiler: new HarmonyMethod(typeof(CorrectifLigneVide), nameof(Transpiler)));
                harmony.Patch(publique, prefix: new HarmonyMethod(typeof(CorrectifLigneVide), nameof(AvantExpansion)),
                                        finalizer: new HarmonyMethod(typeof(CorrectifLigneVide), nameof(ApresExpansion)));
            }
            catch
            {
                harmony.UnpatchAll(IdHarmony);
                throw;
            }
            Actif = true;
        }

        /// <summary>Retire le patch : le code d'origine de WPF est rétabli.</summary>
        public static void Desactiver()
        {
            if (!Actif)
            {
                return;
            }
            harmony.UnpatchAll(IdHarmony);
            Actif = false;
        }

        private static void AvantExpansion(TextUnit __0)
        {
            expansionLigne = __0 == TextUnit.Line;
        }

        // Finalizer : exécuté même si l'original lève une exception (qui est alors relancée telle quelle).
        private static void ApresExpansion()
        {
            expansionLigne = false;
        }

        // Cherche l'appel « lineRange.Contains(_end) » (TextSegment.Contains) et insère juste après :
        //   || lineRange.Start.CompareTo(lineRange.End) == 0 || expansionLigne
        // CA1859 (renvoyer List) écartée : IEnumerable<CodeInstruction> est la signature documentée d'un transpileur Harmony.
#pragma warning disable CA1859
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
#pragma warning restore CA1859
        {
            List<CodeInstruction> code = instructions.ToList();

            int appel = code.FindIndex(c => c.opcode == OpCodes.Call && c.operand is MethodInfo m
                                            && m.Name == "Contains" && m.DeclaringType.Name == "TextSegment");
            if (appel < 0)
            {
                throw new InvalidOperationException("appel TextSegment.Contains introuvable");
            }

            // Variable locale lineRange : le dernier « ldloca » avant l'appel.
            int chargement = code.FindLastIndex(appel, c => c.opcode == OpCodes.Ldloca || c.opcode == OpCodes.Ldloca_S);
            if (chargement < 0)
            {
                throw new InvalidOperationException("variable lineRange introuvable");
            }
            CodeInstruction lineRange = code[chargement];

            Type segment = ((MethodInfo)code[appel].operand).DeclaringType;
            MethodInfo debut = AccessTools.PropertyGetter(segment, "Start");
            MethodInfo fin = AccessTools.PropertyGetter(segment, "End");
            MethodInfo comparer = debut == null ? null : AccessTools.Method(debut.ReturnType, "CompareTo", new[] { debut.ReturnType });
            if (fin == null || comparer == null)
            {
                throw new InvalidOperationException("TextSegment.Start, End ou ITextPointer.CompareTo introuvable");
            }

            code.InsertRange(appel + 1, new[]
            {
                new CodeInstruction(lineRange.opcode, lineRange.operand),
                new CodeInstruction(OpCodes.Call, debut),
                new CodeInstruction(lineRange.opcode, lineRange.operand),
                new CodeInstruction(OpCodes.Call, fin),
                new CodeInstruction(OpCodes.Callvirt, comparer),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ceq),
                new CodeInstruction(OpCodes.Or),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(CorrectifLigneVide), nameof(expansionLigne))),
                new CodeInstruction(OpCodes.Or),
            });
            return code;
        }
    }
}
