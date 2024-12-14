using System.Collections.Generic;
using AutoActMod.Actions;
using HarmonyLib;
using UnityEngine;

namespace AutoActMod.Patches;

[HarmonyPatch]
static class Misc
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AM_Adv), "_OnUpdateInput")]
    static void AM_Adv_OnUpdateInput_Patch()
    {
        if (EInput.leftMouse.down || EInput.rightMouse.down || EInput.middleMouse.down)
        {
            AutoActMod.lastHitPoint = Scene.HitPoint.Copy();
        }

    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CharaRenderer), "OnEnterScreen")]
    public static void CharaRenderer_OnEnterScreen_Patch()
    {
        if (AutoActMod.active && Settings.IgnoreEnemySpotted)
        {
            EClass.player.enemySpotted = false;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AIAct), "SetChild")]
    static void AIAct_SetChild_Patch(AIAct __instance, AIAct seq)
    {
        if (seq is AI_Goto go && (__instance is AI_Shear or TaskHarvest or TaskBuild))
        {
            go.ignoreConnection = true;
        }
    }

#if DEBUG
    [HarmonyPrefix]
    [HarmonyPatch(typeof(VirtualDate), "BuildSunMap")]
    static bool ShutUp(VirtualDate __instance)
    {
        __instance.sunMap = new HashSet<int>();
        foreach (Trait trait in EClass._map.props.installed.traits.suns.Values)
        {
            foreach (Point point in trait.ListPoints(null, false))
            {
                __instance.sunMap.Add(point.index);
            }
        }
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Chara), "SetAI")]
    static void Chara_SetAI_Patch(Chara __instance, AIAct g)
    {
        if (!__instance.IsPC)
        {
            return;
        }
        AIAct prev = __instance.ai;
        // if (prev is GoalIdle || prev is GoalManualMove || prev is NoGoal)
        // {
        //     return;
        // }
        Debug.Log($"===  Chara_SetAI_Prefix  ===");
        Debug.Log($"Prev: {prev}, {prev.status}, Next: {g}");
        Debug.Log($"==== Chara_SetAI_Prefix ====");
        // Utils.PrintStackTrace();
    }

    // [HarmonyPrefix]
    // [HarmonyPatch(typeof(AIAct), "Tick")]
    // static bool AIAct_Tick_Prefix(AIAct __instance, ref AIAct.Status __result)
    // {
    //     // return true;
    //     if (__instance is AutoAct)
    //     {
    //         Debug.Log(__instance);
    //         if (__instance.owner == null || (__instance.isFail != null && __instance.isFail()))
    //         {
    //             __result = __instance.Cancel();
    //             return false;
    //         }
    //         Debug.Log("Tick 1");
    //         if (__instance.IsChildRunning)
    //         {
    //             switch (__instance.child.Tick())
    //             {
    //                 case AIAct.Status.Running:
    //                     __result = AIAct.Status.Running;
    //                     Debug.Log("Child running");
    //                     return false;
    //                 case AIAct.Status.Fail:
    //                     Debug.Log("Child fail");
    //                     if (__instance.onChildFail != null)
    //                     {
    //                         __result = __instance.onChildFail();
    //                         return false;
    //                     }
    //                     __result = AIAct.Status.Fail;
    //                     return false;
    //                 case AIAct.Status.Success:
    //                     Debug.Log("Child success");
    //                     if (__instance.owner == null || (__instance.isFail != null && __instance.isFail()))
    //                     {
    //                         __result = __instance.Cancel();
    //                         return false;
    //                     }
    //                     break;
    //             }
    //         }
    //         Debug.Log("Tick 2");
    //         if (__instance.Enumerator == null)
    //         {
    //             __instance.Start();
    //             if (__instance.status != AIAct.Status.Running)
    //             {
    //                 __result = __instance.status;
    //                 return false;
    //             }
    //         }
    //         Debug.Log("Tick 3");
    //         if (!__instance.Enumerator.MoveNext())
    //         {
    //             __result = __instance.Success(null);
    //             return false;
    //         }
    //         Debug.Log("Tick 4");
    //         Debug.Log(__instance + " | " + __instance.status);
    //         __result = __instance.status;
    //         return false;
    //     }
    //     return true;
    // }
#endif
}