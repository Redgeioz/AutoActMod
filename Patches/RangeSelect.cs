using System;
using System.Collections.Generic;
using System.Linq;
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
    internal static List<Chara> CharaRange = [];
    internal static Thing LastHeld;
    internal static Action OnSelectComplete;
    internal static int Width;
    internal static int Height;

    internal static bool Active => Input.GetKey(Settings.RangeSelectKeyCode) || Range.Count > 0 || CharaRange.Count > 0;

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
        CharaRange.Clear();
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

        if (HotItemHeld.taskBuild.HasValue()
            && (t.Num > 1 || t.trait is TraitSeed)
            && t.trait is TraitSeed or TraitFloor or TraitPlatform or TraitBlock or TraitFertilizer)
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
            OnSelectComplete = SetAutoActPourWaterOrDrawWater;
        }
        else if (t.trait is TraitToolButcher)
        {
            OnSelectComplete = SetAutoActSlaughter;
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
        if (CharaRange.Count > 0)
        {
            CharaRange.ForEach(c => c.pos.SetHighlight(4));
        }
        else
        {
            Range.ForEach(p => p.SetHighlight(8));
        }
    }

    static Point FindNearestPoint()
    {
        return Range.FindMax(p => -EClass.pc.pos.Dist2(p));
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
        var autoAct = new AutoActBuild(HotItemHeld.taskBuild)
        {
            w = Width,
            h = Height,
            hasSowRange = true,
            onStart = SetStartPos,
            range = Range,
        };

        var plantChecker = (Point p) => true;
        if (EClass.pc.held.trait is TraitSeed seed)
        {
            if (seed.row.id == 88 && Range.Find(p => p.IsWater).HasValue())
            {
                plantChecker = p => p.IsWater;
            }
            else if (Range.Find(p => p.IsFarmField).HasValue())
            {
                plantChecker = p => p.IsFarmField;
            }
        }
        Range.RemoveAll(p => !autoAct.PointChecker(p) || !plantChecker(p));
        if (Range.Count > 0)
        {
            SetAutoAct(autoAct);
        }
    }

    static void SetAutoActDig()
    {
        var dig = new TaskDig
        {
            pos = FindNearestPoint(),
            mode = TaskDig.Mode.RemoveFloor,
        };

        var autoAct = new AutoActDig(dig)
        {
            w = Width,
            h = Height,
            onStart = SetStartPos,
            range = Range
        };

        Range.RemoveAll(p => !autoAct.Filter(p.cell));
        if (Range.Count > 0)
        {
            SetAutoAct(autoAct);
        }
    }

    static void SetAutoActPlow()
    {
        var plow = new TaskPlow { pos = FindNearestPoint() };

        var autoAct = new AutoActPlow(plow)
        {
            w = Width,
            h = Height,
            onStart = SetStartPos,
            range = Range
        };

        Range.RemoveAll(p => !autoAct.Filter(p.cell));
        if (Range.Count > 0)
        {
            SetAutoAct(autoAct);
        }
    }

    static void SetAutoActPourWaterOrDrawWater()
    {
        var pot = EClass.pc.held.trait as TraitToolWaterPot;
        if (pot.owner.c_charges == 0 || Range.Count(p => AutoActDrawWater.CanDrawWaterSimple(p.cell)) > Range.Count / 2)
        {
            SetAutoActDrawWater(pot);
        }
        else
        {
            SetAutoActPourWater(pot);
        }
    }

    static void SetAutoActDrawWater(TraitToolWaterPot pot)
    {
        Range.RemoveAll(p => !AutoActDrawWater.CanDrawWaterSimple(p.cell));
        if (Range.Count == 0)
        {
            return;
        }

        var source = new TaskDrawWater
        {
            pos = StartPos,
            pot = pot,
        };

        SetAutoAct(new AutoActDrawWater(source)
        {
            onStart = SetStartPos,
            simpleIdentify = 1,
            range = Range,
        });
    }

    static void SetAutoActPourWater(TraitToolWaterPot pot)
    {
        Range.RemoveAll(p => !AutoActPourWater.CanPourWater(p.cell));
        if (Range.Count == 0)
        {
            return;
        }

        var subAct = new AutoActPourWater.SubActPourWater
        {
            pos = StartPos,
            pot = pot,
            targetCount = Settings.PourDepth,
        };

        SetAutoAct(new AutoActPourWater(subAct)
        {
            w = Width,
            h = Height,
            onStart = SetStartPos,
            range = Range,
        });
    }

    static void SetAutoActHarvestMine()
    {
        var taskHarvest = new TaskHarvest { pos = StartPos };
        Range.RemoveAll(p =>
        {
            SourceData.BaseRow row;
            if (p.HasObj)
            {
                row = p.sourceObj;
            }
            else if (p.HasBlock)
            {
                row = p.sourceBlock;
            }
            else
            {
                return true;
            }

            if (TaskMine.CanMine(p, EClass.pc.held))
            {
                return false;
            }

            if (AutoAct.RowCheckCache.TryGetValue(row, out var result))
            {
                return result;
            }

            result = !AutoActHarvestMine.CanHarvest(EClass.pc, p);
            AutoAct.RowCheckCache.Add(row, result);
            return result;
        });
        AutoAct.RowCheckCache.Clear();

        if (Range.Count == 0)
        {
            return;
        }

        SetAutoAct(new AutoActHarvestMine(taskHarvest).SetRange(Range));
    }

    static void SetAutoActSlaughter()
    {
        Range.ForEach(p =>
        {
            p.Charas.ForEach(chara =>
            {
                if (AutoActSlaughter.CanBeSlaughtered(chara))
                {
                    CharaRange.Add(chara);
                }
            });
        });
        Range.Clear();
        SetAutoAct(new AutoActSlaughter(new AI_Slaughter())
        {
            range = CharaRange
        });
    }
}