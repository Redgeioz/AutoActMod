using System;
using System.Collections.Generic;
using AutoActMod.Actions;
using HarmonyLib;
using UnityEngine;

namespace AutoActMod.Patches;

[HarmonyPatch]
internal static class RangeSelect
{
    internal static Point FirstPoint;
    internal static Point StartPos = new();
    internal static List<Point> Range = [];
    internal static Thing LastHeld;
    internal static Action OnSelectComplete;
    internal static int Width;
    internal static int Height;

    internal static bool Active => Input.GetKey(Settings.RangeSelectKeyCode) || Range.Count > 0;

    static void SetRange(Point p1, Point p2)
    {
        Range.Clear();
        var xMin = Math.Min(p1.x, p2.x);
        var zMin = Math.Min(p1.z, p2.z);
        var xMax = Math.Max(p1.x, p2.x);
        var zMax = Math.Max(p1.z, p2.z);

        Width = xMax - xMin + 1;
        Height = zMax - zMin + 1;
        StartPos.Set(xMin, zMin);

        for (var x = xMin; x <= xMax; x++)
        {
            for (var z = zMin; z <= zMax; z++)
            {
                Range.Add(new Point(x, z));
            }
        }
    }

    static void Reset()
    {
        if (EClass.pc.ai is AutoAct)
        {
            return;
        }

        Range.Clear();
        FirstPoint = null;
        LastHeld = null;
        OnSelectComplete = null;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(AM_Adv), nameof(AM_Adv.PressedActionMove))]
    static bool AM_Adv_PressedActionMove_Patch() => !Active;

    [HarmonyPostfix, HarmonyPatch(typeof(AM_Adv), nameof(AM_Adv._OnUpdateInput))]
    static void AM_Adv_OnUpdateInput_Patch()
    {
        if (EClass.pc.held is not Thing t
            || (LastHeld.HasValue() && t != LastHeld)
            || EInput.middleMouse.down
            || EInput.rightMouse.down)
        {
            Reset();
            return;
        }

        if (HotItemHeld.taskBuild.HasValue() && (t.Num > 1 || t.trait is TraitSeed) && t.trait is not TraitFertilizer)
        {
            OnSelectComplete = SetAutoActBuild;
        }
        else if (t.trait is TraitTool && t.HasElement(230, 1))
        {
            OnSelectComplete = SetAutoActDig;
        }
        else if (t.trait is TraitTool && t.HasElement(286, 1))
        {
            OnSelectComplete = SetAutoActPlow;
        }
        else if (t.trait is TraitToolWaterPot)
        {
            OnSelectComplete = SetAutoActPourWater;
        }
        else if (t.trait is not (TraitToolShears or TraitToolWaterCan or TraitToolMusic or TraitFertilizer))
        {
            OnSelectComplete = SetAutoActHarvestMine;
        }
        else
        {
            Reset();
            return;
        }

        LastHeld = t;

        if (FirstPoint.HasValue())
        {
            SetRange(FirstPoint, EClass.scene.mouseTarget.pos);
        }
        else
        {
            Reset();
        }

        if (!EInput.leftMouse.down || !Active)
        {
            return;
        }

        if (FirstPoint.IsNull())
        {
            FirstPoint = Scene.HitPoint.Copy();
        }
        else
        {
            FirstPoint = null;
            OnSelectComplete?.Invoke();
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Player), nameof(Player.MarkMapHighlights))]
    static void MarkMapHighlights_Patch()
    {
        Range.ForEach(p => p.SetHighlight(8));
    }

    static Point FindNearestPoint()
    {
        return Range.FindMax(p => -Utils.Dist2(EClass.pc.pos, p));
    }

    static void SetStartPos(AutoAct autoAct)
    {
        autoAct.startPos = StartPos;
        autoAct.startDir = 2;
    }

    static AutoAct SetAutoAct(AutoAct a)
    {
        var autoAct = AutoAct.SetAutoAct(EClass.pc, a);
        autoAct.useOriginalPos = false;
        return autoAct;
    }

    static void SetAutoActBuild()
    {
        var autoAct = SetAutoAct(new AutoActBuild(HotItemHeld.taskBuild)
        {
            w = Width,
            h = Height,
            hasSowRange = true,
            onStart = SetStartPos,
        }) as AutoActBuild;
        autoAct.range = Range;
    }

    static void SetAutoActDig()
    {
        var dig = new TaskDig
        {
            pos = FindNearestPoint(),
            mode = TaskDig.Mode.RemoveFloor,
        };

        var autoAct = SetAutoAct(new AutoActDig(dig)
        {
            w = Width,
            h = Height,
            onStart = SetStartPos,
        }) as AutoActDig;

        autoAct.range = Range;
    }

    static void SetAutoActPlow()
    {
        var plow = new TaskPlow { pos = FindNearestPoint() };

        var autoAct = SetAutoAct(new AutoActPlow(plow)
        {
            w = Width,
            h = Height,
            onStart = SetStartPos,
        }) as AutoActPlow;

        autoAct.range = Range;
    }

    static void SetAutoActPourWater()
    {
        var subAct = new AutoActPourWater.SubActPourWater
        {
            pos = StartPos,
            pot = EClass.pc.held.trait as TraitToolWaterPot,
            targetCount = Settings.PourDepth,
        };

        SetAutoAct(new AutoActPourWater(subAct)
        {
            w = Width,
            h = Height,
            onStart = SetStartPos
        });
    }

    static void SetAutoActHarvestMine()
    {
        var taskHarvest = new TaskHarvest { pos = StartPos };
        var c = Range.RemoveAll(p => TaskHarvest.TryGetAct(EClass.pc, p).IsNull() && !TaskMine.CanMine(p, EClass.pc.held));
        if (Range.Count == 0)
        {
            return;
        }
        var autoAct = SetAutoAct(new AutoActHarvestMine(taskHarvest)) as AutoActHarvestMine;
        autoAct.SetRange(Range);
    }
}