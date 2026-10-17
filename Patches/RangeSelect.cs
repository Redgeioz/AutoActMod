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
    public static long LastColorChange = 0;
    public static int CharaHighlightColor = 0;

    internal static bool Active => !ActionFilterConfig.IsOpen && HeldActionEnabled()
        && (Input.GetKey(Settings.RangeSelectKeyCode) || Selected.Count > 0);

    static bool HeldActionEnabled()
    {
        if (EClass.pc.held is not Thing t) return ActionFilter.IsEnabled(AutoAction.Pick);
        if (HotItemHeld.taskBuild.HasValue()
            && (t.Num > 1 || t.trait is TraitSeed)
            && t.trait is TraitSeed or TraitFloor or TraitPlatform or TraitBlock or TraitFertilizer)
            return ActionFilter.IsEnabled(ActionFilter.GetBuildAction(t));
        if (t.trait is TraitTool && t.HasElement(230, 1)) return ActionFilter.IsEnabled(AutoAction.Dig);
        if (t.trait is TraitTool && t.HasElement(286, 1)) return ActionFilter.IsEnabled(AutoAction.Plow);
        if (t.trait is TraitToolWaterPot)
            return ActionFilter.IsEnabled(AutoAction.DrawWater) || ActionFilter.IsEnabled(AutoAction.PourWater);
        if (t.trait is TraitToolButcher) return ActionFilter.IsEnabled(AutoAction.Slaughter);
        if (t.trait is TraitToolBrush && t.HasElement(237, 1)) return ActionFilter.IsEnabled(AutoAction.Brush);
        if (t.trait is TraitToolShears or TraitToolWaterCan or TraitToolMusic or TraitFertilizer) return false;
        return ActionFilter.IsEnabled(AutoAction.Mine) || ActionFilter.IsEnabled(AutoAction.Collect)
            || ActionFilter.IsEnabled(AutoAction.ReapSeed);
    }

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

            CharaRange.RemoveAll(chara => chara.pos.Equals(p));
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
                var p = new Point(x, z);
                if (p.IsInBounds)
                {
                    Selected.Add(p);
                }
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
        RightClickPoint = null;
        LastHeld = null;
        OnSelectComplete = null;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(AM_Adv), nameof(AM_Adv.SetPressedAction))]
    static bool SetPressedAction_Patch() => !ActionFilterConfig.IsOpen && !Active;

    [HarmonyPostfix, HarmonyPatch(typeof(AM_Adv), nameof(AM_Adv._OnUpdateInput))]
    static void AM_Adv_OnUpdateInput_Patch()
    {
        if (ActionFilterConfig.IsOpen || !HeldActionEnabled())
        {
            Reset();
            return;
        }
        if ((LastHeld.HasValue() && LastHeld != EClass.pc.held) || EInput.middleMouse.down)
        {
            Reset();
            return;
        }

        if (EClass.pc.held is not Thing t)
        {
            OnSelectComplete = SetAutoActPick;
        }
        else if (HotItemHeld.taskBuild.HasValue()
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
        else if (t.trait is TraitToolBrush && t.HasElement(237, 1))
        {
            OnSelectComplete = SetAutoActBrush;
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

        LastHeld = EClass.pc.held as Thing;

        if (LeftClickPoint.HasValue())
        {
            SetSelected(LeftClickPoint, EClass.scene.mouseTarget.pos, LastHeld?.trait is TraitBlock);
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
                Reset();
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
                RemoveRange();
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
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (now - LastColorChange > 400)
            {
                LastColorChange = now;
                CharaHighlightColor = (CharaHighlightColor + 1) % 2;
            }
            var cColor = CharaHighlightColor == 0 ? 2 : 8;
            CharaRange.ForEach(c => c.pos.SetHighlight(cColor));
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
        if (autoAct.IsNull())
        {
            Reset();
            return null;
        }
        autoAct.useOriginalPos = false;
        if (autoAct is AutoActPlow or AutoActDig or AutoActPourWater || (autoAct is AutoActBuild aab && aab.Child.held.trait is not TraitBlock))
        {
            // start from the center
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
        if (!ActionFilter.IsEnabled(ActionFilter.GetBuildAction(EClass.pc.held))) return;
        var autoAct = new AutoActBuild(HotItemHeld.taskBuild)
        {
            hasSowRange = true,
            onStart = OnRangeActionStart,
        };

        autoAct.SetRange(Range);

        var pointChecker = (Point p) => true;
        if (EClass.pc.held.trait is TraitSeed seed)
        {
            if (seed.row.id == 88 && Range.FirstOrDefault(p => p.IsWater).HasValue())
            {
                pointChecker = p => p.IsWater;
            }
            else if (Range.FirstOrDefault(p => p.IsFarmField).HasValue())
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
        if (!ActionFilter.IsEnabled(AutoAction.Dig)) return;
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
        if (!ActionFilter.IsEnabled(AutoAction.Plow)) return;
        Range.RemoveWhere(p => !AutoActPlow.Filter(p.cell));
        if (Range.Count == 0)
        {
            return;
        }

        var plow = new TaskPlow { pos = StartPos.Copy() };

        var autoAct = new AutoActPlow(plow)
        {
            w = Width,
            h = Height,
            onStart = OnRangeActionStart,
            range = Range
        };

        SetAutoAct(autoAct);
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
        if (!ActionFilter.IsEnabled(AutoAction.DrawWater)) return;
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
        if (!ActionFilter.IsEnabled(AutoAction.PourWater)) return;
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
        if (!ActionFilter.IsEnabled(AutoAction.Mine) && !ActionFilter.IsEnabled(AutoAction.Collect)
            && !ActionFilter.IsEnabled(AutoAction.ReapSeed)) return;
        var taskHarvest = new TaskHarvest { pos = StartPos };
        Range.RemoveWhere(p =>
        {
            if (!p.HasObj && !p.HasBlock) return true;
            if (AutoActHarvestMine.CanHarvest(EClass.pc, p))
            {
                return !ActionFilter.IsEnabled(ActionFilter.GetHarvestAction(p, EClass.pc));
            }
            return !ActionFilter.IsEnabled(AutoAction.Mine) || !TaskMine.CanMine(p, EClass.pc.held);
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
        if (!ActionFilter.IsEnabled(AutoAction.Slaughter)) return;
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
        if (!ActionFilter.IsEnabled(AutoAction.Brush)) return;
        if (CharaRange.Count == 0)
        {
            return;
        }

        SetAutoAct(new AutoActBrush(new AI_TendAnimal())
        {
            range = CharaRange
        });
    }

    static void SetAutoActPick()
    {
        if (!ActionFilter.IsEnabled(AutoAction.Pick)) return;
        var pick = new AutoActPick.SubActPick
        {
            pos = StartPos.Copy(),
            installed = false,
            pickAll = true,
        };

        var autoAct = new AutoActPick(pick)
        {
            range = Range
        };

        Range.RemoveWhere(p => p.Things.Find(t => t.placeState != PlaceState.installed).IsNull());
        if (Range.Count > 0)
        {
            SetAutoAct(autoAct);
        }
    }
}
