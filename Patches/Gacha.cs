using System;
using HarmonyLib;
using UnityEngine;

namespace AutoActMod.Patches;

[HarmonyPatch]
public static class Gacha
{
    public static InvOwner InvOwner;
    public static ButtonGrid Coin;
    public static TraitGachaBall GachaBall;
    public static long LastUpdate = 0;

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

    [HarmonyPostfix, HarmonyPatch(typeof(TraitGachaBall), nameof(TraitGachaBall.OnUse))]
    static void TraitGachaBall_OnUse_Patch(TraitGachaBall __instance)
    {
        if (!AutoActMod.IsSwitchOn || GachaBall.HasValue())
        {
            return;
        }

        AutoActMod.Say(AALang.GetText("start"));
        GachaBall = __instance;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(AutoActMod), nameof(AutoActMod.Update))]
    internal static void Update()
    {

        if (Input.GetMouseButtonDown(0)
         || Input.GetMouseButtonDown(1))
        {
            Reset();
            return;
        }

        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        if (now - LastUpdate < 40)
        {
            return;
        }

        LastUpdate = now;

        AutoFeed();
        AutoOpen();
    }

    internal static void AutoFeed()
    {
        if (InvOwner.IsNull())
        {
            return;
        }

        if (InvOwner.destInvOwner is not InvOwnerGacha invOwnerGacha
            || Coin.card is not Thing t
            || t.isDestroyed)
        {
            Reset();
            return;
        }

        new InvOwner.Transaction(Coin, t.Num).Process();

        invOwnerGacha.dragGrid.RefreshCurrentGrid();
    }

    internal static void AutoOpen()
    {
        if (GachaBall.IsNull())
        {
            return;
        }

        GachaBall.OnUse(EClass.pc);
        if (GachaBall.owner.isDestroyed)
        {
            GachaBall = null;
        }
    }

    internal static void Reset()
    {
        InvOwner = null;
        Coin = null;
        GachaBall = null;
    }
}