using System.Collections.Generic;
using System.Reflection.Emit;
using AutoActMod.Actions;
using HarmonyLib;

namespace AutoActMod.Patches;

[HarmonyPatch]
static class HandleEnemy
{
    [HarmonyTranspiler, HarmonyPatch(typeof(CharaRenderer), nameof(CharaRenderer.OnEnterScreen))]
    static IEnumerable<CodeInstruction> CharaRenderer_OnEnterScreen_Patch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldc_R4),
                new CodeMatch(OpCodes.Stfld))
            .Advance(1)
            .InsertAndAdvance(
                new CodeMatch(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((CharaRenderer thiz) =>
                {
                    if (thiz.owner.ExistsOnMap && !EClass._zone.IsRegion && thiz.owner.IsHostile() && EClass.pc.CanSeeLos(thiz.owner, -1))
                    {
                        OnSpotEnemy();
                    }
                }),
                new CodeInstruction(OpCodes.Ret))
            .InstructionEnumeration();
    }

    // Make AutoAct able to stop when spotting an enemy
    [HarmonyTranspiler, HarmonyPatch(typeof(AI_Goto), nameof(AI_Goto.Run), MethodType.Enumerator)]
    static IEnumerable<CodeInstruction> AI_Goto_Run_Patch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .Start()
            .MatchStartForward(
                new CodeMatch(OpCodes.Stfld),
                new CodeMatch(OpCodes.Ldstr))
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_1),
                Transpilers.EmitDelegate((AI_Goto thiz) =>
                {
                    if (thiz.parent is AutoAct autoAct)
                    {
                        autoAct.restartCount = (byte)autoAct.MaxRestart;
                    }
                })
            )
            .InstructionEnumeration();
    }

    static void OnSpotEnemy()
    {
        if (EClass.pc.ai is GoalCombat)
        {
            return;
        }

        if (!AutoActMod.Active || Settings.EnemyEncounterResponse == 0)
        {
            if (EClass.core.config.game.haltOnSpotEnemy)
            {
                EClass.player.enemySpotted = true;
            }

            return;
        }

        if (Settings.EnemyEncounterResponse == 2)
        {
            EClass.pc.FindNewEnemy();
            EClass.pc.SetAIAggro();
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Chara), nameof(Chara.SetAIAggro))]
    static bool Chara_SetAIAggro_Patch(Chara __instance)
    {
        if (__instance.ai.Current is GoalCombat)
        {
            return false;
        }

        var goal = __instance.IsPC ? new GoalAutoCombat(__instance.enemy) : new GoalCombat();

        if (__instance.ai is AutoAct autoAct && autoAct.IsRunning)
        {
            autoAct.InsertAction(goal);
            return false;
        }

        __instance.SetAI(goal);

        return false;
    }
}