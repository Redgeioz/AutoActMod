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
    internal static Point LeftClickPoint;
    internal static Point RightClickPoint;
    internal static Point StartPos = new();
    internal static HashSet<Point> Range = [];
    internal static List<Point> Selected = [];
    internal static List<Chara> CharaRange = [];
    internal static Thing LastHeld;
    internal static Action OnSelectComplete;
    internal static bool UseCenter = false;
    internal static int Width;
    internal static int Height;

    internal static bool Active => Input.GetKey(Settings.RangeSelectKeyCode) || Selected.Count > 0;

    static void AddRange()
    {
        Func<Chara, bool> check;
        if (OnSelectComplete == SetAutoActSlaughter)
        {
            check = AutoActSlaughter.CanBeSlaughtered;
        }
        else if (OnSelectComplete == SetAutoActBrush)
        {
            check = AutoActBrush.CanBeBrushed;
        }
        else
        {
            Selected.ForEach(p => Range.Add(p));
            UpdateRangeInfo();
            return;
        }

        foreach (var p in Selected)
        {
            p.Charas.ForEach(chara =>
            {
                if (check(chara) && !CharaRange.Contains(chara))
                {
                    CharaRange.Add(chara);
                }
            });
        }
    }

    static void RemoveRange()
    {
        Selected.ForEach(p =>
        {
            if (Range.Contains(p))
            {
                Range.Remove(p);
            }
        });

        UpdateRangeInfo();
    }

    static void UpdateRangeInfo()
    {
        if (Range.Count == 0)
        {
            return;
        }

        var min = Range.First().Copy();
        var max = min.Copy();
        foreach (var p in Range)
        {
            min.x = Math.Min(min.x, p.x);
            min.z = Math.Min(min.z, p.z);
            max.x = Math.Max(max.x, p.x);
            max.z = Math.Max(max.z, p.z);
        }

        Width = max.x - min.x + 1;
        Height = max.z - min.z + 1;
        StartPos.Set(min);
    }

    static void SetSelected(Point p1, Point p2, bool edgeOnly = false)
    {
        Selected.Clear();
        var xMin = Math.Min(p1.x, p2.x);
        var zMin = Math.Min(p1.z, p2.z);
        var xMax = Math.Max(p1.x, p2.x);
        var zMax = Math.Max(p1.z, p2.z);
        for (var x = xMin; x <= xMax; x++)
        {
            for (var z = zMin; z <= zMax; z++)
            {
                if (edgeOnly && x != xMin && x != xMax && z != zMin && z != zMax) { continue; }
                Selected.Add(new Point(x, z));
            }
        }
    }

    static internal void Reset()
    {
        if (EClass.pc.ai is AutoAct && EClass.pc.ai.IsRunning)
        {
            return;
        }

        Range.Clear();
        Selected.Clear();
        CharaRange.Clear();
        LeftClickPoint = null;
        LastHeld = null;
        OnSelectComplete = null;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(AM_Adv), nameof(AM_Adv.SetPressedAction))]
    static bool SetPressedAction_Patch() => !Active;

    [HarmonyPostfix, HarmonyPatch(typeof(AM_Adv), nameof(AM_Adv._OnUpdateInput))]
    static void AM_Adv_OnUpdateInput_Patch()
    {
        if (EClass.pc.held is not Thing t
            || (LastHeld.HasValue() && t != LastHeld)
            || EInput.middleMouse.down)
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
        else if (t.trait is TraitToolBrush && t.HasElement(237, 1))
        {
            OnSelectComplete = SetAutoActBrush;
        }
        else
        {
            Reset();
            return;
        }

        LastHeld = t;

        if (LeftClickPoint.HasValue())
        {
            SetSelected(LeftClickPoint, EClass.scene.mouseTarget.pos, t.trait is TraitBlock);
        }
        else if (RightClickPoint.HasValue())
        {
            SetSelected(RightClickPoint, EClass.scene.mouseTarget.pos);
        }
        else
        {
            Selected.Clear();
        }

        if (!Active)
        {
            if ((Range.Count > 0 || CharaRange.Count > 0) && EClass.pc.ai is not AutoAct)
            {
                OnSelectComplete?.Invoke();
            }
            return;
        }


        if (EInput.leftMouse.down)
        {
            if (LeftClickPoint.HasValue())
            {
                AddRange();
                Selected.Clear();
                LeftClickPoint = null;
            }
            else if (RightClickPoint.HasValue())
            {
                Selected.Clear();
                RightClickPoint = null;
            }
            else
            {
                LeftClickPoint = Scene.HitPoint.Copy();
            }
        }
        else if (EInput.rightMouse.down)
        {
            if (LeftClickPoint.HasValue())
            {
                Selected.Clear();
                LeftClickPoint = null;
            }
            else if (RightClickPoint.HasValue())
            {
                RemoveRange();
                Selected.Clear();
                RightClickPoint = null;
            }
            else
            {
                RightClickPoint = Scene.HitPoint.Copy();
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Player), nameof(Player.MarkMapHighlights))]
    static void MarkMapHighlights_Patch()
    {
        if (CharaRange.Count > 0)
        {
            CharaRange.ForEach(c => c.pos.SetHighlight(2));
        }
        else
        {
            Range.RemoveWhere(p =>
            {
                if (p.IsInBounds) { p.SetHighlight(8); }
                return !p.IsInBounds;
            });
        }

        var color = LeftClickPoint.HasValue() ? 8 : 4;
        Selected.RemoveAll(p =>
        {
            if (p.IsInBounds) { p.SetHighlight(color); }
            return !p.IsInBounds;
        });
    }

    static void OnRangeActionStart(AutoAct autoAct)
    {
        autoAct.startPos = StartPos;
        autoAct.startDir = 2;
    }

    static AutoAct SetAutoAct(AutoAct a)
    {
        var autoAct = AutoAct.SetAutoAct(EClass.pc, a);
        autoAct.useOriginalPos = false;
        if (autoAct is AutoActPlow or AutoActDig or AutoActPourWater || (autoAct is AutoActBuild aab && aab.Child.held.trait is not TraitBlock))
        {
            if (Width == Height && (Width == 3 || Width == 5))
            {
                var p = new Point(StartPos.x + Width / 2, StartPos.z + Width / 2);
                if (Range.Contains(p))
                {
                    (autoAct.child as TaskPoint).pos = p;
                    autoAct.useOriginalPos = true;
                    autoAct.InsertAction(new AI_Goto(p, 0, true));
                }
            }
        }
        return autoAct;
    }

    static void SetAutoActBuild()
    {
        var autoAct = new AutoActBuild(HotItemHeld.taskBuild)
        {
            hasSowRange = true,
            onStart = OnRangeActionStart,
        };

        autoAct.SetRange(Range);

        var pointChecker = (Point p) => true;
        if (EClass.pc.held.trait is TraitSeed seed)
        {
            if (seed.row.id == 88 && Range.First(p => p.IsWater).HasValue())
            {
                pointChecker = p => p.IsWater;
            }
            else if (Range.First(p => p.IsFarmField).HasValue())
            {
                pointChecker = p => p.IsFarmField;
            }
        }
        Range.RemoveWhere(p => !autoAct.PointChecker(p) || !pointChecker(p));
        if (Range.Count > 0)
        {
            SetAutoAct(autoAct);
        }
    }

    static void SetAutoActDig()
    {
        var dig = new TaskDig
        {
            pos = StartPos.Copy(),
            mode = TaskDig.Mode.RemoveFloor,
        };

        var autoAct = new AutoActDig(dig)
        {
            w = Width,
            h = Height,
            onStart = OnRangeActionStart,
            range = Range
        };

        Range.RemoveWhere(p => !autoAct.Filter(p.cell));
        if (Range.Count > 0)
        {
            SetAutoAct(autoAct);
        }
    }

    static void SetAutoActPlow()
    {
        var plow = new TaskPlow { pos = StartPos.Copy() };

        var autoAct = new AutoActPlow(plow)
        {
            w = Width,
            h = Height,
            onStart = OnRangeActionStart,
            range = Range
        };

        Range.RemoveWhere(p => !autoAct.Filter(p.cell));
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
        Range.RemoveWhere(p => !AutoActDrawWater.CanDrawWaterSimple(p.cell));
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
            onStart = OnRangeActionStart,
            simpleIdentify = 1,
            range = Range,
        });
    }

    static void SetAutoActPourWater(TraitToolWaterPot pot)
    {
        Range.RemoveWhere(p => !AutoActPourWater.CanPourWater(p.cell));
        if (Range.Count == 0)
        {
            return;
        }

        var subAct = new AutoActPourWater.SubActPourWater
        {
            pos = StartPos.Copy(),
            pot = pot,
            targetCount = Settings.PourDepth,
        };

        SetAutoAct(new AutoActPourWater(subAct)
        {
            w = Width,
            h = Height,
            onStart = OnRangeActionStart,
            range = Range,
        });
    }

    static void SetAutoActHarvestMine()
    {
        var taskHarvest = new TaskHarvest { pos = StartPos };
        Range.RemoveWhere(p =>
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
        if (CharaRange.Count == 0)
        {
            return;
        }

        SetAutoAct(new AutoActSlaughter(new AI_Slaughter())
        {
            range = CharaRange
        });
    }

    static void SetAutoActBrush()
    {
        if (CharaRange.Count == 0)
        {
            return;
        }

        SetAutoAct(new AutoActBrush(new AI_TendAnimal())
        {
            range = CharaRange
        });
    }
}