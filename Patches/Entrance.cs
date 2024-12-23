using System;
using System.Collections.Generic;
using System.Linq;
using AutoActMod.Actions;
using HarmonyLib;
using UnityEngine;

namespace AutoActMod.Patches;

[HarmonyPatch]
static class Entrance
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Chara), "SetAI")]
    static bool Chara_SetAI_Patch(Chara __instance, AIAct g)
    {
        if (!__instance.IsPC) { return true; }
#if DEBUG
        AIAct prev = __instance.ai;
        Debug.Log($"===   Chara_SetAI_Prefix   ===");
        Debug.Log($"Prev: {prev}, {prev.status} | Next: {g}");
        Debug.Log($"=== Chara_SetAI_Prefix End ===");
#endif
        if (AutoActMod.IsSwitchOn)
        {
            return AutoAct.TrySetAutoAct(__instance, g).IsNull();
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Act), "Perform", new Type[] { typeof(Chara), typeof(Card), typeof(Point) })]
    static bool Act_Perform_Patch(Act __instance, Chara _cc, Point _tp, ref bool __result)
    {
        if (!_cc.IsPC || !AutoActMod.IsSwitchOn) { return true; }
        if (AutoAct.TrySetAutoAct(_cc, __instance, _tp).HasValue())
        {
            __result = true;
            return false;
        }
        return true;
    }

    // To fix AutoAct being unable to be interrupted by attacks
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AIAct), "Success")]
    static bool AIAct_Success_Patch(AIAct __instance, ref AIAct.Status __result)
    {
        if (__instance.child.HasValue()
            && __instance.child.status == AIAct.Status.Fail
            && (__instance.onChildFail.IsNull() || __instance.onChildFail() == AIAct.Status.Fail))
        {
            if (__instance is AutoAct aa)
            {
                aa.CancelRetry();
            }
            if (__instance.parent is AutoAct aa2)
            {
                aa2.CancelRetry();
            }
            __result = __instance.Cancel();
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ActPlan), "ShowContextMenu")]
    public static void ActPlan_ShowContextMenu_Patch(ActPlan __instance)
    {
        if (!__instance.pos.Equals(EClass.pc.pos))
        {
            return;
        }

        Settings.ShowSettings(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ActPlan.Item), "Perform")]
    static bool ActPlan_Item_Perform_Patch(ActPlan.Item __instance)
    {
        if (__instance.act is DynamicAct a && a.id == ALang.GetText("settings"))
        {
            a.Perform();
            return false;
        }
        return true;
    }
}