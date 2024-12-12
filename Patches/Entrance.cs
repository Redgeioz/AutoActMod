using System;
using System.Collections.Generic;
using System.Linq;
using ElinAutoAct.Actions;
using HarmonyLib;
using UnityEngine;

namespace ElinAutoAct.Patches;

[HarmonyPatch]
static class Entrance
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AIAct), "Start")]
    static void AIAct_Start_Patch(AIAct __instance)
    {
        var a = __instance;
        if (!a.owner.IsPC)
        {
            return;
        }

        if (AutoActMod.IsSwitchOn)
        {
            AutoAct.TrySetAutoAct(a.owner, a);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Act), "Perform", new Type[] { typeof(Chara), typeof(Card), typeof(Point) })]
    static bool Act_Perform_Patch(Act __instance, ref bool __result)
    {
        if (!AutoActMod.IsSwitchOn) { return true; }
        if (AutoAct.TrySetAutoAct(Act.CC, __instance, AutoActMod.lastHitPoint).HasValue())
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
            if (__instance.parent is AutoAct aa)
            {
                aa.CancelRetry();
            }
            __result = AIAct.Status.Fail;
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