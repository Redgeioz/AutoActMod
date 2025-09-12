using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoActMod.Actions;

public class AutoActBuild(TaskBuild source) : AutoAct(source)
{
    public bool hasSowRange;
    public HashSet<Point> range = [];
    public Dictionary<Point, int> directions = [];
    public TaskBuild Child => child as TaskBuild;
    public Card Held => Child.held;
    public override int MaxRestart => 0;

    public static AutoActBuild TryCreate(AIAct source)
    {
        if (source is not TaskBuild a) { return null; }
        if (source.owner.IsPC
            && (source.owner.held is not Thing t
            || (t.Num == 1 && t.trait is not (TraitSeed or TraitFertilizer))
            || (t.trait is not (TraitSeed or TraitFertilizer))))
        {
            return null;
        }
        return new AutoActBuild(a);
    }

    public override bool CanProgress()
    {
        return base.CanProgress() && !Held.isDestroyed && owner.held == Held && Held.placeState != PlaceState.installed;
    }

    public override void OnStart()
    {
        base.OnStart();
        Init();
    }

    public override void OnChildSuccess()
    {
        range.Remove(Pos);
    }

    public override IEnumerable<Status> Run()
    {
        while (CanProgress())
        {
            var targetPos = FindNextBuildPosition();
            if (targetPos.IsNull())
            {
                yield break;
            }

            SetPosition(targetPos);
            yield return StartNextTask();

            if (!owner.IsPCParty)
            {
                continue;
            }

            if (!CheckHeld())
            {
                if (range.Count == 0)
                {
                    yield break;
                }
                else
                {
                    yield return Fail();
                }
            }
        }
        yield break;
    }

    public void Init()
    {
        RestoreChild();
        _ = HeldChecker;
        if (Held.trait is TraitSeed || Held.trait is TraitFertilizer)
        {
            if (range.Count == 0)
            {
                range = InitFarmField(startPos);
            }
            return;
        }
    }

    public void SetRange(HashSet<Point> range)
    {
        this.range = range;
        if (!Child.recipe.IsWallOrFence)
        {
            return;
        }

        var neighbor = new Point(0, 0);
        foreach (var p in range)
        {
            if (range.Contains(neighbor.Set(p.x - 1, p.z)) && range.Contains(neighbor.Set(p.x, p.z + 1)))
            {
                // ┘
                directions.Add(p, 2);
            }
            else if (range.Contains(neighbor.Set(p.x - 1, p.z)) && range.Contains(neighbor.Set(p.x, p.z - 1)))
            {
                // ┐
                directions.Add(p, 0);
            }
            else if (range.Contains(neighbor.Set(p.x + 1, p.z)) && range.Contains(neighbor.Set(p.x, p.z - 1))
                && !(range.Contains(neighbor.Set(p.x - 1, p.z)) && range.Contains(neighbor.Set(p.x + 1, p.z)))
                && !(range.Contains(neighbor.Set(p.x, p.z - 1)) && range.Contains(neighbor.Set(p.x, p.z + 1))))
            {
                // ┌
                directions.Add(p, 3);
            }
            else if (range.Contains(neighbor.Set(p.x, p.z - 1)) || range.Contains(neighbor.Set(p.x, p.z + 1)))
            {
                // |
                directions.Add(p, 1);
            }
            else
            {
                // ——
                directions.Add(p, 0);
            }
        }
    }

    public Point FindNextBuildPosition()
    {
        var edgeOnly = false;
        if (Held.trait is TraitBlock)
        {
            edgeOnly = true;
        }

        if (useOriginalPos)
        {
            useOriginalPos = false;
            return Pos;
        }

        var list = new List<(Point, int, int)>();
        foreach (var p in range)
        {
            if (!PointChecker(p))
            {
                continue;
            }

            var dist2 = CalcDist2(p);
            var dist2ToLastPoint = CalcDist2ToLastPoint(p);
            list.Add((p, dist2, dist2ToLastPoint));
        }

        foreach (var item in list.OrderBy(tuple => tuple.Item2).ThenBy(tuple => tuple.Item3))
        {
            var (p, dist2, dist2ToLastPoint) = item;
            if (selector.curtPoint.HasValue() && !edgeOnly && dist2 > selector.MaxDist2)
            {
                break;
            }

            var pathLength = dist2 == 0 ? -1 : 0;
            if (edgeOnly)
            {
                if (dist2 > 2)
                {
                    Path.RequestPathImmediate(owner.pos, p, 1, true, -1);
                    if (Path.state == PathProgress.State.Fail)
                    {
                        continue;
                    }

                    pathLength = Path.nodes.Count;
                }

                if (!Child.recipe.IsWallOrFence)
                {
                    selector.TrySet(p, pathLength, dist2ToLastPoint);
                    continue;
                }

                var dir = directions[p];
                if (dir == 3)
                {
                    continue;
                }

                if (selector.TrySet(p, pathLength, dist2ToLastPoint))
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

                pathLength = Path.nodes.Count;
            }

            var (d1, d2) = CalcStartPosDelta(p);
            if (d1 >= 0)
            {
                (d1, d2) = CalcDelta(p);
                if (d1 < 0)
                {
                    d1 = -d1 * 2;
                }
            }

            selector.TrySet(p, pathLength, dist2ToLastPoint, d1, d2);
        }

        return selector.FinalPoint;
    }

    public bool CheckHeld()
    {
        var held = owner.held;
        if (owner.IsPC)
        {
            held ??= HotItemHeld.lastHeld;
        }
        if (held.IsNull() || held.isDestroyed || held.GetRootCard() != pc)
        {
            return TrySwitchHeld();
        }

        if (held == Held)
        {
            return true;
        }

        if (HeldChecker?.Invoke(held as Thing) is true)
        {
            Child.held = held;
            return true;
        }
        else
        {
            return TrySwitchHeld();
        }
    }

    public bool TrySwitchHeld()
    {
        var nextHeld = FindNextHeld();
        if (nextHeld.HasValue())
        {
            pc.HoldCard(nextHeld);
            pc.party.members.ForEach(chara =>
            {
                if (chara.ai is AutoActBuild autoAct && autoAct.IsRunning)
                {
                    chara.held = nextHeld;
                    autoAct.Child.held = nextHeld;
                }
            });
            return true;
        }
        return false;
    }

    public Thing FindNextHeld()
    {
        if (HeldChecker.IsNull())
        {
            return null;
        }

        Thing item = null;
        foreach (var t1 in pc.things.Flatten())
        {
            if (!HeldChecker(t1))
            {
                continue;
            }

            if (t1.trait is not TraitSeed)
            {
                return t1;
            }

            if (item.IsNull() || t1.encLV > item.encLV)
            {
                item = t1;
            }
        }

        return item;
    }

    public Func<Thing, bool> HeldChecker
    {
        get
        {
            if (field.HasValue())
            {
                return field;
            }

            if (Held.trait is TraitSeed)
            {
                var seedId = sources.objs.map[Held.refVal].id;
                field = t => t.trait is TraitSeed seed && seed.row.id == seedId;
            }
            else if (Held.trait is TraitDefertilizer)
            {
                field = t => t.trait is TraitDefertilizer;
            }
            else if (Held.trait is TraitFertilizer)
            {
                field = t => t.trait is TraitFertilizer && t.trait is not TraitDefertilizer;
            }

            return field;
        }
    }

    public Func<Point, bool> PointChecker
    {
        get
        {
            if (field.HasValue())
            {
                return field;
            }

            if (Held.trait is TraitSeed)
            {
                field = p => (!p.HasThing || p.Things[0].IsInstalled) && (!p.HasBlock || p.HasWallOrFence) && !p.HasObj && p.growth.IsNull() && p.Installed?.trait is null or TraitLight;
            }
            else if (Held.trait is TraitFertilizer)
            {
                field = ShouldFertilize;
            }
            else if (Held.trait is TraitFloor or TraitPlatform)
            {
                var rowId = (Held.trait as TraitTile).source.id;
                field = p => !p.HasThing && !p.HasBlock && !p.HasObj && p.cell.sourceSurface.id != rowId;
            }
            else
            {
                field = p => !p.HasThing && !p.HasBlock;
            }

            return field;
        }
    }

    public void SetPosition(Point p)
    {
        Child.pos = p;
        RestoreChild();
    }

    public void RestoreChild()
    {
        Child.lastPos = null;
        Child.isDestroyed = false;
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