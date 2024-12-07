using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace AutoAct;

[HarmonyPatch]
static class Entrance
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AIAct), "Start")]
    static void AIAct_Start_Patch(AIAct __instance)
    {
        AIAct a = __instance;
        if (!a.owner.IsPC)
        {
            return;
        }

        if (a is TaskHarvest
            or TaskDig
            or TaskMine
            or TaskPlow
            or TaskDrawWater
            or AI_Read
            or AI_Shear)
        {
            AutoAct.UpdateState(a);
        }
        if (a is TaskPourWater tpw)
        {
            if (AutoAct.active && !AutoAct.IsTarget(tpw.pos.sourceFloor))
            {
                AutoAct.pourCount += 1;
                if (AutoAct.pourCount >= Settings.PourDepth)
                {
                    tpw.Success();
                }
            }
            else
            {
                AutoAct.UpdateState(tpw);
            }
        }

        if (a is TaskBuild tb)
        {
            AutoAct.UpdateStateTaskBuild(tb);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DynamicAct), "Perform")]
    static void DaynamicAct_Perform_Patch(DynamicAct __instance)
    {
        // Debug.Log($"DynamicAct: {__instance.id}");
        if (!AutoAct.IsSwitchOn) { return; }
        if (__instance.id == "actClean")
        {
            AutoAct.active = true;
            AutoAct.SayStart();
            OnActionComplete.ContinueClean();
        }
        else if (__instance.id == "actPickOne")
        {
            List<Thing> list = AutoAct.lastHitPoint.Things;
            Thing refThing = list.FindLast(t => t.placeState == PlaceState.roaming);
            if (refThing == null) { return; }
            AutoAct.active = true;
            AutoAct.SayStart();
            OnActionComplete.ContinuePick(refThing);
        }
        else if (__instance.id == "actHold")
        {
            List<Thing> list = AutoAct.lastHitPoint.Things;
            Thing refThing = list.LastOrDefault();
            if (refThing == null) { return; }
            AutoAct.active = true;
            AutoAct.SayStart();
            OnActionComplete.ContinuePick(refThing, refThing.placeState == PlaceState.installed);
        }
    }

    [HarmonyPatch(typeof(AIAct), "Success")]
    static class AIAct_Success_Patch
    {
        // To fix AutoAct being unable to be interrupted by attacks
        [HarmonyPrefix]
        static bool Prefix(AIAct __instance, ref AIAct.Status __result)
        {
            if (__instance.child != null && __instance.child.status == AIAct.Status.Fail)
            {
                if (__instance.onChildFail == null || __instance.onChildFail() == AIAct.Status.Fail)
                {
                    __result = AIAct.Status.Fail;
                    return false;
                }
            }
            return true;
        }

        [HarmonyPostfix]
        static void Postfix(AIAct __instance, AIAct.Status __result)
        {
            if (__instance is TaskBuild)
            {
                OnTaskBuildComplete.Run(__instance, __result);
            }
            else
            {
                OnActionComplete.Run(__instance, __result);
            }
        }
    }
}