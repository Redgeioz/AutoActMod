using HarmonyLib;
using UnityEngine;

namespace AutoActMod.Patches;

[HarmonyPatch]
public static class Gacha
{
    public static InvOwner InvOwner;
    public static ButtonGrid Coin;

    [HarmonyPostfix, HarmonyPatch(typeof(InvOwner), nameof(InvOwner.OnRightClick))]
    static void InvOwner_OnRightClick_Patch(InvOwner __instance, ButtonGrid button)
    {
        if (!AutoActMod.IsSwitchOn
            || __instance.destInvOwner is not InvOwnerGacha
            || button.card is not Thing t
            || t.isDestroyed)
        {
            return;
        }

        AutoActMod.Say(AALang.GetText("start"));
        InvOwner = __instance;
        Coin = button;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(AutoActMod), nameof(AutoActMod.Update))]
    internal static void AutoFeed()
    {
        if (InvOwner.IsNull())
        {
            return;
        }

        if (InvOwner.destInvOwner is not InvOwnerGacha invOwnerGacha
            || Input.GetMouseButtonDown(0)
            || Input.GetMouseButtonDown(1)
            || Coin.card is not Thing t
            || t.isDestroyed)
        {
            InvOwner = null;
            Coin = null;
            return;
        }

        new InvOwner.Transaction(Coin, t.Num).Process();

        invOwnerGacha.dragGrid.RefreshCurrentGrid();
    }
}