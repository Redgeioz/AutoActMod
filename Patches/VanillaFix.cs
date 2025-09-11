using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace AutoActMod.Patches;

[HarmonyPatch]
static class VanillaFix
{
    [HarmonyPostfix, HarmonyPatch(typeof(AIAct), nameof(AIAct.SetChild))]
    static void AIAct_SetChild_Patch(AIAct __instance, AIAct seq)
    {
        if (seq is AI_Goto move
            && __instance is AI_Shear or AI_Fuck or AI_Slaughter or (TaskPoint and not TaskPlow))
        {
            move.ignoreConnection = true;
        }
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(Recipe), nameof(Recipe.Build), [typeof(Chara), typeof(Card), typeof(Point), typeof(int), typeof(int), typeof(int), typeof(int)])]
    static IEnumerable<CodeInstruction> Recipe_Build_Patch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_3),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Point), nameof(Point.ListCharas)))
            )
            .RemoveInstruction()
            .Insert(Transpilers.EmitDelegate((Point p) => p.ListCharas().Copy()))
            .InstructionEnumeration();
    }
}