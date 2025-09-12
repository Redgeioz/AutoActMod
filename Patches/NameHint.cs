using System;
using System.Collections.Generic;
using System.Reflection;
using AutoActMod.Actions;
using HarmonyLib;

namespace AutoActMod.Patches;

[HarmonyPatch]
static class NameHint
{
    public static void EditText(ref string str) => str += $"({AALang.GetText("autoact")})";

    [HarmonyPostfix, HarmonyPatch(typeof(Act), nameof(Act.GetText))]
    static void Act_GetText_Patch(Act __instance, ref string __result)
    {
        if (!AutoActMod.IsSwitchOn)
        {
            return;
        }

        var target = EClass.scene.mouseTarget.card;
        if (__instance is ActDrawWater or AI_TendAnimal or TaskMine or TaskHarvest or TaskDrawWater
            || (__instance is TaskWater taskWater && taskWater.dest.cell.HasFire)
            || (__instance is TaskDig
                && (EClass._zone.IsRegion
                    || !(Scene.HitPoint.cell.sourceSurface.tag.Contains("grass") || Scene.HitPoint.HasBridge)))
            || (__instance is AI_OpenLock && target is Thing t && AutoActUnlock.NeedUnlock(t))
            || (__instance is ActThrow && target is Chara chara && AutoActThrowMilk.NeedMilk(chara)))
        {
            EditText(ref __result);
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(DynamicAct), nameof(Act.GetText))]
    static void DynamicAct_GetText_Patch(DynamicAct __instance, ref string __result)
    {
        if (!AutoActMod.IsSwitchOn)
        {
            return;
        }

        var target = EClass.scene.mouseTarget.card;
        var actions = new string[] {
            "actMilk",
            "actPickOne",
            "actHold",
        };
        if (actions.Contains(__instance.id)
            || (__instance.id == "AI_Slaughter" && target is Chara chara && AutoActSlaughter.CanBeSlaughtered(chara))
            || (__result == Element.Get(6011).GetName()) // steal
            || (__result == AutoActSmash.GetActMeleeLang() && AutoActSmash.CanSmash(target)))
        {
            EditText(ref __result);
        }
    }

    [HarmonyPatch]
    static class Rename
    {
        static IEnumerable<MethodInfo> TargetMethods() => [
            AccessTools.Method(typeof(AI_Shear), nameof(AI_Shear.GetText)),
            AccessTools.Method(typeof(TaskClean), nameof(TaskClean.GetText)),
        ];

        static void Postfix(ref string __result)
        {
            if (AutoActMod.IsSwitchOn)
            {
                EditText(ref __result);
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(InvOwner), nameof(InvOwner.GetAutoUseLang))]
    static void InvOwner_GetAutoUseLang_Patch(InvOwner __instance, ButtonGrid button, ref string __result)
    {
        if (!AutoActMod.IsSwitchOn || button.card is not Thing t)
        {
            return;
        }

        var list = __instance.ListInteractions(button, false);
        if (list.Count == 0)
        {
            return;
        }

        if (AutoActRead.CanRead(t) || t.trait is TraitBookSkill or TraitGachaBall)
        {
            EditText(ref __result);
        }
    }

    // [HarmonyPostfix, HarmonyPatch(typeof(ActPlan), nameof(ActPlan.GetText))]
    // static void ActPlan_GetText_Patch(ActPlan __instance, ref string __result)
    // {
    //     if (!AutoActMod.IsSwitchOn)
    //     {
    //         return;
    //     }

    //     AutoActMod.Log($"ActPlan {__result}");
    // }
}