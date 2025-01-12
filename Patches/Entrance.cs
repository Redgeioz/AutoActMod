using AutoActMod.Actions;
using HarmonyLib;

namespace AutoActMod.Patches;

[HarmonyPatch]
static class Entrance
{
    [HarmonyPrefix, HarmonyPatch(typeof(Chara), nameof(Chara.SetAI))]
    static bool Chara_SetAI_Patch(Chara __instance, AIAct g)
    {
#if DEBUG
        if (!__instance.IsPC) { return true; }
        if (!__instance.IsPCParty) { return true; }
        AIAct prev = __instance.ai;
        AutoActMod.Log($"===   Chara_SetAI_Prefix   ===");
        AutoActMod.Log($"Chara: {__instance.Name}");
        AutoActMod.Log($"Prev: {prev}, {prev.status} | Next: {g}");
        AutoActMod.Log($"=== Chara_SetAI_Prefix End ===");
#endif
        if (!__instance.IsPC) { return true; }
        if (AutoActMod.IsSwitchOn)
        {
            return AutoAct.TrySetAutoAct(__instance, g).IsNull();
        }
        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Act), nameof(Act.Perform), [typeof(Chara), typeof(Card), typeof(Point)])]
    static bool Act_Perform_Patch(Act __instance, Chara _cc, Card _tc, Point _tp, ref bool __result)
    {
        if (!_cc.IsPC || !AutoActMod.IsSwitchOn) { return true; }
#if DEBUG
        AutoActMod.Log($"===   Act_Perform_Patch   ===");
        AutoActMod.Log($"Perform Action: {__instance}");
        AutoActMod.Log($"=== Act_Perform_Patch End ===");
#endif
        if (AutoAct.TrySetAutoAct(_cc, __instance, _tc, _tp).HasValue())
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ActPlan), nameof(ActPlan.ShowContextMenu))]
    public static void ActPlan_ShowContextMenu_Patch(ActPlan __instance)
    {
        if (!__instance.pos.Equals(EClass.pc.pos))
        {
            return;
        }

        Settings.SetupSettings(__instance);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ActPlan.Item), nameof(ActPlan.Item.Perform))]
    static bool ActPlan_Item_Perform_Patch(ActPlan.Item __instance)
    {
        if (__instance.act is DynamicAct a && a.id == AALang.GetText("settings"))
        {
            a.Perform();
            return false;
        }
        return true;
    }
}