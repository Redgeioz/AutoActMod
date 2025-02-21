using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using AutoActMod.Actions;
using HarmonyLib;

namespace AutoActMod.Patches;

[HarmonyPatch]
static class HandleEnemy
{
    static readonly Dictionary<Chara, (AutoAct, Thing)> Paused = [];

    [HarmonyTranspiler, HarmonyPatch(typeof(CharaRenderer), nameof(CharaRenderer.OnEnterScreen))]
    static IEnumerable<CodeInstruction> CharaRenderer_OnEnterScreen_Patch(IEnumerable<CodeInstruction> instructions)
    {
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

    [HarmonyPrefix, HarmonyPatch(typeof(Chara), nameof(Chara.SetAIAggro))]
    static bool Chara_SetAIAggro_Patch(Chara __instance)
    {
        var goal = __instance.IsPC ? new GoalAutoCombat(__instance.enemy) : new GoalCombat();

        if (__instance.ai is AutoAct autoAct)
        {
            Paused.Remove(__instance);
            Paused.Add(__instance, (autoAct, __instance.held as Thing));
#if DEBUG
            AutoActMod.Log($"Pause Auto Act: {__instance.Name} | {__instance.ai}");
#endif
            __instance.ai = Chara._NoGoalPC;
        }

        __instance.SetAI(goal);

        return false;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(AIAct), nameof(AIAct.OnReset))]
    static void AIAct_OnReset_Patch(AIAct __instance)
    {
        if (__instance is not GoalCombat g)
        {
            return;
        }

        var chara = g.owner;
        if (Paused.TryGetValue(chara, out var pair))
        {
            var (autoAct, thing) = pair;

            if (chara.IsPC || autoAct is not AutoActBuild)
            {
                chara.HoldCard(thing);
            }
            else
            {
                chara.held = thing;
            }

            chara.ai = autoAct;

            autoAct.Retry();
            Paused.Remove(chara);
            g.Reset();
#if DEBUG
            AutoActMod.Log($"Restore Auto Act: {chara.Name} | {chara.ai}");
#endif
        }
    }
}