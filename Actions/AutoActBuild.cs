using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElinAutoAct.Actions;

public class AutoActBuild : AutoAct
{
    public int w;
    public int h;
    public bool hasSowRange;
    public HashSet<Point> field = new();
    public Card Held => owner.held;
    public TaskBuild Child => child as TaskBuild;
    public override int MaxRestart => 0;

    public AutoActBuild(TaskBuild source) : base(source)
    {
        w = Settings.BuildRangeW;
        h = Settings.BuildRangeH;
        if (Settings.StartFromCenter)
        {
            h = 0;
        }

        hasSowRange = Settings.SowRangeExists;
    }

    public static AutoActBuild TryCreate(AIAct source)
    {
        if (source is not TaskBuild a) { return null; }
        var held = source.owner.held;
        if (held.IsNull() || held.Num == 1) { return null; }
        return new AutoActBuild(a);
    }

    public override bool CanProgress()
    {
        return base.CanProgress() && Held == Child.held;
    }

    public override IEnumerable<Status> Run()
    {
        var performFirstTask = Init();
        if (performFirstTask)
        {
            yield return StartNextTask();
        }
        while (CanProgress())
        {
            var targetPos = FindNextBuildPosition();
            if (targetPos.IsNull())
            {
                End();
                yield break;
            }

            Child.pos = targetPos;
            Child.lastPos = null;
            Child.isDestroyed = false;
            field.Remove(targetPos);
            yield return StartNextTask();
        }
        yield break;
    }

    public bool Init()
    {
        field.Clear();
        Child.held = Held;
        if (Held.category.id == "seed" || Held.category.id == "fertilizer")
        {
            InitFarmfield(field, startPos);
        }
        else
        {
            InitField(field, startPos, p => !p.HasBlock);
            var startFromCenter = h == 0;
            if (startFromCenter)
            {
                if (Child.recipe.IsWallOrFence || Child.recipe.IsBlock)
                {
                    // skip
                    return false;
                }
            }
            else if (Child.recipe.IsWallOrFence)
            {
                var dir = CalcBuildDirection(0b1001);
                if (dir == 3)
                {
                    // skip
                    return false;
                }
                else
                {
                    Child.recipe._dir = dir;
                }
            }
        }

        return true;
    }

    public int CalcPositionInfo(int d1, int d2)
    {
        var h = this.h == 0 ? w : this.h;
        var f1 = d1 == 0 ? 1 : 0;
        var f2 = d2 == w - 1 ? 1 : 0;
        var f3 = d1 == h - 1 ? 1 : 0;
        var f4 = d2 == 0 ? 1 : 0;
        return f1 << 3 | f2 << 2 | f3 << 1 | f4;
    }

    Point FindNextBuildPosition()
    {
        Func<Point, bool> filter;
        var hasRange = true;
        var edgeOnly = false;
        var startFromCenter = h == 0;
        if (Held.category.id == "seed")
        {
            filter = p => !p.HasThing && (!p.HasBlock || p.HasWallOrFence) && !p.HasObj && p.growth.IsNull() && p.Installed.IsNull();
            hasRange = hasSowRange;
        }
        else if (Held.category.id == "fertilizer")
        {
            filter = ShouldFertilize;
            hasRange = false;
        }
        else if (Held.category.id == "floor" || Held.category.id == "foundation")
        {
            filter = p => !p.HasThing && !p.HasBlock && !p.HasObj && p.cell.sourceSurface != startPos.cell.sourceSurface;
        }
        else
        {
            filter = p => !p.HasThing && !p.HasBlock && !p.HasObj;
            edgeOnly = true;
        }

        var list = new List<(Point, int, int, int)>();
        foreach (var p in field)
        {
            if (!filter(p))
            {
                continue;
            }

            var dist2 = CalcDist2(p);
            var dist2ToLastPoint = CalcDist2ToLastPoint(p);
            if (startFromCenter)
            {
                var max = CalcMaxDeltaToStartPos(p);
                if (hasRange && max > w / 2)
                {
                    continue;
                }

                if (edgeOnly && max != w / 2)
                {
                    continue;
                }

                if (max <= 1)
                {
                    if (Child.recipe.IsWallOrFence)
                    {
                        var refPos = new Point(startPos.x + w / 2, startPos.z + w / 2);
                        var (d1, d2) = CalcDelta(p, refPos, 0);
                        var dir = CalcBuildDirection(CalcPositionInfo(d1, d2), 0);
                        if (dir != 3 && selector.TrySet(p, max, max - 1, dist2ToLastPoint))
                        {
                            Child.recipe._dir = dir;
                        }
                        continue;
                    }
                    selector.TrySet(p, max, max - 1, dist2ToLastPoint);
                    continue;
                }

                list.Add((p, max, dist2, dist2ToLastPoint));
            }
            else
            {
                list.Add((p, 0, dist2, dist2ToLastPoint));
            }
        }

        foreach (var item in list.OrderBy(tuple => tuple.Item2).ThenBy(tuple => tuple.Item3))
        {
            var (p, max, dist2, dist2ToLastPoint) = item;
            if (selector.curtPoint.HasValue() &&
                ((startFromCenter && max > selector.Factor) ||
                (!startFromCenter && !edgeOnly && dist2ToLastPoint > selector.Factor)))
            {
                break;
            }

            if (startFromCenter)
            {
                Path.RequestPathImmediate(owner.pos, p, 1, true, -1);
                if (Path.state == PathProgress.State.Fail)
                {
                    continue;
                }

                if (Child.recipe.IsWallOrFence)
                {
                    var refPos = new Point(startPos.x + w / 2, startPos.z + w / 2);
                    var (d1, d2) = CalcDelta(p, refPos, 0);
                    var dir = CalcBuildDirection(CalcPositionInfo(d1, d2), 0);
                    if (dir != 3 && selector.TrySet(p, max, Path.nodes.Count, dist2ToLastPoint))
                    {
                        Child.recipe._dir = dir;
                    }
                    continue;
                }

                selector.TrySet(p, max, Path.nodes.Count, dist2ToLastPoint);
            }
            else
            {
                var (d1, d2) = CalcStartPosDelta(p);
                if (hasRange && (d1 < 0 || d2 < 0 || d1 >= h || d2 >= w))
                {
                    continue;
                }

                if (edgeOnly)
                {
                    var info = CalcPositionInfo(d1, d2);
                    if (info.GetBit(3) == 1)
                    {
                        // nothing
                    }
                    else if (info.GetBit(2) == 1)
                    {
                        d2 = d1;
                        d1 = 1;
                    }
                    else if (info.GetBit(1) == 1)
                    {
                        d2 = -d2;
                        d1 = 2;
                    }
                    else if (info.GetBit(0) == 1)
                    {
                        d2 = -d1;
                        d1 = 3;
                    }
                    else
                    {
                        continue;
                    }

                    if (dist2 > 2)
                    {
                        Path.RequestPathImmediate(owner.pos, p, 1, true, -1);
                        if (Path.state == PathProgress.State.Fail)
                        {
                            continue;
                        }
                    }

                    if (!Child.recipe.IsWallOrFence)
                    {
                        selector.TrySet(p, d1, d2, 0);
                        continue;
                    }

                    var dir = CalcBuildDirection(info);
                    if (dir == 3)
                    {
                        continue;
                    }

                    if (selector.TrySet(p, d1, d2, 0))
                    {
                        Child.recipe._dir = dir;
                    }
                    continue;
                }

                if (dist2 > 2)
                {
                    Path.RequestPathImmediate(owner.pos, p, 1, true, -1);
                    if (Path.state == PathProgress.State.Fail)
                    {
                        continue;
                    }
                }

                if (d1 >= 0)
                {
                    (d1, d2) = CalcDelta(p);
                    if (d1 < 0)
                    {
                        d1 = -d1 * 2;
                    }
                }

                selector.TrySet(p, dist2ToLastPoint, d1, d2);
            }
        }

        return selector.FinalPoint;
    }

    static bool ShouldFertilize(Point p)
    {
        bool hasPlant = p.growth.HasValue();
        if (!p.HasThing)
        {
            return hasPlant;
        }

        bool fert = false;
        bool seed = false;
        p.Things.ForEach(t =>
        {
            if (t.trait is TraitFertilizer)
            {
                fert = true;
            }
            else if (t.trait is TraitSeed)
            {
                seed = true;
            }
        });

        return (seed || hasPlant) && !fert;
    }
}