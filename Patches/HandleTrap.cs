using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using AutoActMod.Actions;
using HarmonyLib;

namespace AutoActMod.Patches;

[HarmonyPatch]
static class HandleTrap
{
    [HarmonyPatch]
    static class ActWait_Search_Patch
    {
        static MethodInfo TargetMethod()
        {
            return AccessTools.Method(
                AccessTools.FirstInner(typeof(ActWait), t => t.Name.Contains("DisplayClass8_0")),
                "<Search>b__0");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldsfld),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CoreConfig.GameConfig), nameof(CoreConfig.GameConfig.haltOnSpotTrap))))
                .RemoveInstructions(4)
                .InsertAndAdvance(
                    new CodeInstruction(Transpilers.EmitDelegate(() => EClass.pc.ai is not AutoAct && EClass.core.config.game.haltOnSpotTrap)))
                .InstructionEnumeration();
        }
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(AI_Goto), nameof(AI_Goto.TryGoTo))]
    static IEnumerable<CodeInstruction> AI_Goto_TryGoTo_Patch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Ldloc_3),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Card), nameof(Card.TryMove))))
            .RemoveInstructions(5)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(Transpilers.EmitDelegate(CheckTrap)))
            .InstructionEnumeration();
    }

    static Card.MoveResult CheckTrap(AI_Goto move)
    {
        var chara = move.owner;
        if (!chara.IsPC || chara.ai is not AutoAct)
        {
            return chara.TryMove(Point.shared);
        }

        var trap = Point.shared.Things.Find(t => !t.isHidden && t.trait is TraitTrap)?.trait as TraitTrap;
        if (trap.IsNull())
        {
            return chara.TryMove(Point.shared);
        }

        move.SetChild(new AutoActDisarm(trap));
        return Card.MoveResult.Success;
    }
}