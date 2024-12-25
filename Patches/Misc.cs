using HarmonyLib;

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
        if (seq is AI_Goto go && __instance is AI_Shear or TaskPoint && __instance is not TaskPlow)
        {
            go.ignoreConnection = true;
        }
    }

#if DEBUG
    // [HarmonyPrefix]
    // [HarmonyPatch(typeof(AIAct), "Tick")]
    // static bool AIAct_Tick_Prefix(AIAct __instance, ref AIAct.Status __result)
    // {
    //     // return true;
    //     if (__instance is AutoAct)
    //     {
    //         Debug.Log(__instance + " Start Tick | " + __instance.status);
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
    //             Debug.Log("Suc result" + __result);
    //             return false;
    //         }
    //         Debug.Log("Tick 4");
    //         Debug.Log(__instance + " End Tick | " + __instance.status);
    //         __result = __instance.status;
    //         return false;
    //     }
    //     return true;
    // }
#endif
}