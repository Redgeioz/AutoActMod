using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AutoActMod.Actions;

public class AutoAct : AIAct
{
    public int targetId;
    public TileRow targetRow;
    public string targetName;
    public PlaceState targetPlaceState;
    public Point startPos;
    public virtual Point Pos
    {
        get
        {
#if DEBUG
            if (child is not TaskPoint)
            {
                Debug.LogWarning("AutoAct: The child action is not TaskPoint or is null");
            }
#endif
            return (child as TaskPoint)?.pos;
        }
    }
    public Cell Cell
    {
        get
        {
            return Pos.cell;
        }
    }
    public PathProgress Path => owner.path;
    public Selector selector = new();
    public int startDir;
    public Action<AIAct> onSetNextTask;
    public override int MaxRestart => 1;
    public static Type[] subClasses = GetSubClasses();

    static Type[] GetSubClasses()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(AutoAct)))
            .OrderBy(t =>
            {
                var info = t.GetField("priority");
                var p = info.IsNull() ? 100 : (int)info.GetValue(null);
                return p;
            })
            .ToArray();
    }

    public AutoAct() { }

    public AutoAct(AIAct source)
    {
        System.Diagnostics.Debug.Assert(source.parent is not AutoAct, "Source cannot be a child of a instance of AutoAct");
        child = source;
        child.status = Status.Fail;
        // Avoid calling AIAct.Reset if it is created in AIAct.Tick
    }

    public static AIAct TryGetAutoAct(AIAct source)
    {
        if (source is AutoAct) { return null; }

        var act = source.parent;
        while (act.HasValue())
        {
            if (act is AutoAct) { return null; }
            act = act.parent;
        }

        foreach (var t in subClasses)
        {
            var info = t.GetMethod("TryCreate", new Type[] { typeof(AIAct) });
            if (info.IsNull()) { continue; }
            var a = info.Invoke(null, new object[] { source }) as AutoAct;
            if (a.HasValue())
            {
                return a;
            }
        }

        return null;
    }

    public static AutoAct TryGetAutoAct(string id, Point p)
    {
        foreach (var t in subClasses)
        {
            var info = t.GetMethod("TryCreate", new Type[] { typeof(string), typeof(Point) });
            if (info.IsNull()) { continue; }
            var a = info.Invoke(null, new object[] { id, p }) as AutoAct;
            if (a.HasValue())
            {
                return a;
            }
        }

        return null;
    }

    public static AutoAct TrySetAutoAct(Chara c, AIAct source)
    {
        if (TryGetAutoAct(source) is not AutoAct a)
        {
            return null;
        }

        c.SetAIImmediate(a);
        source.status = Status.Fail;
        return a;
    }

    public static AutoAct TrySetAutoAct(Chara c, Act source, Point p)
    {
        var id = source is DynamicAct d ? d.id : source.ToString();
#if DEBUG
        Debug.Log("=== TrySetAutoAct === " + id);
#endif
        if (TryGetAutoAct(id, p) is not AutoAct a)
        {
            return null;
        }

        c.SetAIImmediate(a);
        return a;
    }

    public Status StartNextTask(bool resetRestartCount = true)
    {
        return SetNextTask(child, null, resetRestartCount);
    }

    public Status StartNextTask(Func<Status> _onChildFail, bool resetRestartCount = true)
    {
        return SetNextTask(child, _onChildFail, resetRestartCount);
    }

    public Status SetNextTask(AIAct a, Func<Status> _onChildFail = null, bool resetRestartCount = true)
    {
        SetChild(a, _onChildFail);
        if (a is Task t)
        {
            t.isDestroyed = false;
        }
        if (a is BaseTaskHarvest bth)
        {
            bth.SetTarget(owner);
        }
        if (resetRestartCount)
        {
            restartCount = 0;
        }
        if (onSetNextTask.HasValue())
        {
            onSetNextTask(a);
        }

        return Status.Running;
    }

    public override bool CanProgress()
    {
        if (!owner.IsPC || Settings.StaminaCheck)
        {
            return owner.stamina.value >= 0;
        }

        return true;
    }

    public override bool CanManualCancel()
    {
        CancelRetry();
        return true;
    }

    public void CancelRetry()
    {
        restartCount = (byte)MaxRestart;
    }

    public override void OnStart()
    {
        SayStart();
        SetStartPos();
    }

    public override void OnSuccess()
    {
        CancelRetry();
    }

    public Status Retry()
    {
        return child.IsNull() ? End() : StartNextTask(false);
    }

    public Status End()
    {
        CancelRetry();
        return Cancel();
    }

    public override Status Cancel()
    {
        // Retries are mainly used to deal with random pathfinding failures (they
        // do happen sometimes even if the player is able to get there) or animal
        // movement during shearing.
        restartCount++;
        if (restartCount <= MaxRestart)
        {
            return Retry();
        }

        SayFail();
        return base.Cancel();
    }

    public void SetTarget(TileRow r)
    {
        var id = r.id;
        if (id == 167)
        {
            id = 1;
        }
        targetId = id;
        targetRow = r;
        targetName = r.name;
    }

    public void SetTarget(Card c)
    {
        targetName = c.id;
        targetPlaceState = c.placeState;
    }

    public bool IsTarget(TileRow r)
    {
        if (targetId == -1)
        {
            return r is SourceBlock.Row || (r is SourceObj.Row obj && obj.tileType.IsBlockMount);
        }
        else if (targetId == -2)
        {
            return r is SourceObj.Row obj && obj.HasGrowth && obj.growth.IsTree;
        }
        else if (targetId == -3)
        {
            return r is SourceObj.Row obj && obj.HasGrowth && !obj.growth.IsTree;
        }

        var id = r.id;
        if (id == 167)
        {
            id = 1;
        }

        return id == targetId && targetRow.GetType() == r.GetType();
    }

    public bool IsTarget(Card c)
    {
        if (c.IsNull())
        {
            return false;
        }

        return c.id == targetName && c.placeState == targetPlaceState;
    }

    public void SetStartPos()
    {
        if (Pos.IsNull())
        {
            return;
        }
        startPos = Pos.Copy();
        var dx = startPos.x - owner.pos.x;
        var dz = startPos.z - owner.pos.z;

        if ((dz == -1 || dz == 0) && dx == -1)
        {
            startDir = 3;
        }
        else if ((dx == -1 || dx == 0) && dz == 1)
        {
            startDir = 2;
        }
        else if ((dz == 1 || dz == 0) && dx == 1)
        {
            startDir = 1;
        }
        else if ((dx == 0 || dx == 1) && dz == -1)
        {
            startDir = 0;
        }
        else
        {
            // dx == 0, dy == 0
            // | 0 ↓ | 1 → | 2 ↑ | 3 ← |
            startDir = owner.dir;
        }
    }

    public int CalcBuildDirection(int n)
    {
        return CalcBuildDirection(n, startDir);
    }

    public int CalcBuildDirection(int n, int dir)
    {
        n = n >> dir | n << (4 - dir);
        var f1 = n >> 3 & 1;
        var f2 = n >> 2 & 1;
        var f3 = n >> 1 & 1;
        var f4 = n & 1;
        if (f3 == 1 && f4 == 1)
        {
            return 2;
        }
        else if (f1 == 1 && f2 == 1)
        {
            return 3;
        }
        else if (f1 == 1 || (f3 == 1 && f2 == 0))
        {
            return 0;
        }
        else if (f2 == 1 || f4 == 1)
        {
            return 1;
        }
        return 3;
    }

    public (int, int) CalcStartPosDelta(Point p)
    {
        //         dir
        //       d1 ↑
        //          |
        //          |
        //          |
        // startPos □——————————→ d2
        return CalcDelta(p, startPos, startDir);
    }

    public (int, int) CalcDelta(Point p)
    {
        return CalcDelta(p, owner.pos, owner.dir);
    }

    public static (int, int) CalcDelta(Point p, Point refPoint, int dir)
    {
        var dx = p.x - refPoint.x;
        var dz = p.z - refPoint.z;

        var d1 = 0;
        var d2 = 0;
        switch (dir)
        {
            case 0:
                d1 = dz * -1;
                d2 = dx * -1;
                break;
            case 1:
                d1 = dx;
                d2 = dz * -1;
                break;
            case 2:
                d1 = dz;
                d2 = dx;
                break;
            case 3:
                d1 = dx * -1;
                d2 = dz;
                break;
        }
        return (d1, d2);
    }

    public void InitFarmfield(HashSet<Point> field, Point p)
    {
        Func<Point, bool> filter;

        if (p.IsWater)
        {
            filter = pt => pt.IsWater;
        }
        else
        {
            filter = pt => pt.IsFarmField;
        }

        InitField(field, p, filter);
        field.Remove(p);
    }

    public static void InitField(HashSet<Point> field, Point start, Func<Point, bool> filter)
    {
        var directions = new (int dx, int dz, int mask, int nextDir)[]
        {
                (-1, 0, 0b0001, 0b1101),
                (1, 0, 0b0010, 0b1110),
                (0, -1, 0b0100, 0b0111),
                (0, 1, 0b1000, 0b1011)
        };

        var stack = new Stack<(Point, int)>();
        stack.Push((start, 0b1111));

        while (stack.Count > 0)
        {
            var (current, dir) = stack.Pop();

            foreach (var (dx, dz, mask, nextDir) in directions)
            {
                if ((dir & mask) == 0) continue;

                var neighbor = new Point(current.x + dx, current.z + dz);

                if (neighbor.IsInBounds && filter(neighbor) && field.Add(neighbor))
                {
                    stack.Push((neighbor, nextDir));
                }
            }
        }
    }

    public void Say(string text)
    {
        if (owner.IsPC)
        {
            AutoActMod.Say(text);
        }
    }

    public void SayStart()
    {
        Say(ALang.GetText("start"));
    }

    public void SayNoTarget()
    {
        CancelRetry();
        Say(ALang.GetText("noTarget"));
    }

    public void SayFail()
    {
        Say(ALang.GetText("fail"));
    }

    public int CalcDist2(Point p)
    {
        return Utils.Dist2(p, owner.pos);
    }

    public int CalcDist2ToLastPoint(Point p)
    {
        return Utils.Dist2(p, Pos);
    }

    public int CalcMaxDelta(Point p)
    {
        return Utils.MaxDelta(p, owner.pos);
    }

    public int CalcMaxDeltaToStartPos(Point p)
    {
        return Utils.MaxDelta(p, startPos);
    }

    public Point FindNextPos(Func<Cell, bool> filter, int detRangeSq, bool tryBetterPath = false)
    {
        var list = new List<(Point, int, int)>();
        _map.bounds.ForeachCell(cell =>
        {
            if (!filter(cell))
            {
                return;
            }

            var p = cell.GetPoint();
            var dist2 = CalcDist2(p);
            if (dist2 > detRangeSq)
            {
                return;
            }

            var dist2ToLastPoint = child is TaskPoint ? CalcDist2ToLastPoint(p) : dist2;
            if (dist2 <= 2)
            {
                selector.TrySet(p, dist2 == 0 ? -1 : 0, dist2ToLastPoint, 0);
                return;
            }

            list.Add((p, dist2, dist2ToLastPoint));
        });

        foreach (var (p, dist2, dist2ToLastPoint) in list.OrderBy(tuple => tuple.Item2))
        {
            if (selector.curtPoint.HasValue() && dist2 > selector.MaxDist2)
            {
                break;
            }

            bool TryDestroyObstacle()
            {
                if (dist2 > 5 || dist2 < 4 || !tryBetterPath)
                {
                    return false;
                }

                var dx = p.x - owner.pos.x;
                var dz = p.z - owner.pos.z;
                var obstacle = new Point(owner.pos.x + dx / 2, owner.pos.z + dz / 2);
                bool CanDestroyObstacle() =>
                    obstacle.HasBlock
                    // soil block
                    && (obstacle.sourceBlock.id == 1 || obstacle.sourceBlock.id == 167)
                    // wall frame
                    && (!obstacle.HasObj || obstacle.sourceObj.id == 24);
                if (CanDestroyObstacle())
                {
                    selector.TrySet(obstacle, 1, dist2ToLastPoint, 0);
                    return true;
                }
                else if (!obstacle.HasBlock && !obstacle.HasObj)
                {
                    obstacle = new Point(p.x - dx / 2, p.z - dz / 2);
                    if (CanDestroyObstacle())
                    {
                        selector.TrySet(obstacle, 1, dist2ToLastPoint, 0);
                        return true;
                    }
                }

                return false;
            }

            Path.RequestPathImmediate(owner.pos, p, 1, true, -1);
            if (Path.state == PathProgress.State.Fail)
            {
                TryDestroyObstacle();
                continue;
            }

            if (Path.nodes.Count >= dist2 && TryDestroyObstacle())
            {
                continue;
            }

            var d2 = 0;
            if (p.HasBlock)
            {
                d2 = Math.Abs(CalcDelta(p).Item2);
            }
            var factor = Path.nodes.Count;
            if (factor > dist2ToLastPoint && dist2ToLastPoint <= 2)
            {
                factor = 1;
            }
            selector.TrySet(p, factor, dist2ToLastPoint, d2);
        }

        return selector.FinalPoint;
    }

    public Point FindNextPosRefToStartPos(Func<Cell, bool> filter, int w, int h = 0)
    {
        var startFromCenter = h == 0;
        var list = new List<(Point, int, int, int)>();
        _map.bounds.ForeachCell(cell =>
        {
            if (!filter(cell))
            {
                return;
            }

            var p = cell.GetPoint();

            var dist2 = CalcDist2(p);
            var dist2ToLastPoint = CalcDist2ToLastPoint(p);
            if (startFromCenter)
            {
                var max = CalcMaxDeltaToStartPos(p);
                if (max > w / 2)
                {
                    return;
                }

                if (max <= 1)
                {
                    selector.TrySet(p, max, max - 1, dist2ToLastPoint);
                    return;
                }

                list.Add((p, max, dist2, dist2ToLastPoint));
            }
            else
            {
                list.Add((p, 0, dist2, dist2ToLastPoint));
            }
        });

        foreach (var item in list.OrderBy(tuple => tuple.Item2).ThenBy(tuple => tuple.Item3))
        {
            var (p, max, dist2, dist2ToLastPoint) = item;
            if (selector.curtPoint.HasValue() &&
                ((startFromCenter && max > selector.Factor) ||
                (!startFromCenter && dist2ToLastPoint > selector.Factor)))
            {
                break;
            }

            if (startFromCenter)
            {
                Path.RequestPathImmediate(owner.pos, p, 1, false, -1);
                if (Path.state == PathProgress.State.Fail)
                {
                    continue;
                }

                selector.TrySet(p, max, Path.nodes.Count, dist2ToLastPoint);
            }
            else
            {
                var (d1, d2) = CalcStartPosDelta(p);
                if (d1 < 0 || d2 < 0 || d1 >= h || d2 >= w)
                {
                    continue;
                }

                if (dist2 > 2)
                {
                    Path.RequestPathImmediate(owner.pos, p, 1, false, -1);
                    if (Path.state == PathProgress.State.Fail)
                    {
                        continue;
                    }
                }

                selector.TrySet(p, dist2ToLastPoint, d1, d2);
            }
        }

        return selector.FinalPoint;
    }

    public Thing FindNextThingTarget(int detRangeSq)
    {
        var list = new List<(Thing, int, int)>();
        _map.bounds.ForeachCell(cell =>
       {
           var p = cell.GetPoint();
           if (!p.HasThing)
           {
               return;
           }

           var dist2 = CalcDist2(p);
           if (dist2 > detRangeSq)
           {
               return;
           }

           var thing = p.Things.Find(t => IsTarget(t));
           if (thing.IsNull())
           {
               return;
           }

           var dist2ToLastPoint = CalcDist2ToLastPoint(p);
           if (dist2 <= 2)
           {
               selector.TrySet(thing, dist2 == 0 ? 0 : 1, dist2ToLastPoint);
               return;
           }

           list.Add((thing, dist2, dist2ToLastPoint));
       });

        foreach (var (thing, dist2, dist2ToLastPoint) in list.OrderBy(tuple => tuple.Item2))
        {
            if (selector.curtPoint.HasValue() && dist2 > selector.MaxDist2)
            {
                break;
            }

            Path.RequestPathImmediate(owner.pos, thing.pos, 1, true, -1);
            if (Path.state == PathProgress.State.Fail)
            {
                continue;
            }

            selector.TrySet(thing, Path.nodes.Count, dist2ToLastPoint);
        }

        return selector.FinalTarget as Thing;
    }

    public Point FindNextPosInField(IEnumerable<Point> field, Func<Cell, bool> filter)
    {
        var list = new List<(Point, int, int)>();
        foreach (var p in field)
        {
            var cell = p.cell;
            if (!filter(cell))
            {
                continue;
            }

            var dist2 = CalcDist2(p);
            var dist2ToLastPoint = CalcDist2ToLastPoint(p);
            var max = CalcMaxDelta(p);
            if (max <= 1)
            {
                selector.TrySet(p, max - 1, dist2ToLastPoint);
                continue;
            }

            list.Add((p, dist2, dist2ToLastPoint));
        }

        foreach (var (p, dist2, dist2ToLastPoint) in list.OrderBy(tuple => tuple.Item2))
        {
            if (selector.curtPoint.HasValue() && dist2 > selector.MaxDist2)
            {
                break;
            }

            Path.RequestPathImmediate(owner.pos, p, 1, true, -1);
            if (Path.state == PathProgress.State.Fail)
            {
                continue;
            }

            selector.TrySet(p, Path.nodes.Count, dist2ToLastPoint);
        }

        return selector.FinalPoint;
    }

    public class Selector
    {
        public Point curtPoint = null;
        public Card item = null;
        public int factor1 = 0;
        public int factor2 = 0;
        public int factor3 = 0;

        public Point FinalPoint
        {
            get
            {
                var finalPoint = curtPoint;
                Reset();
                return finalPoint;
            }
        }
        public Card FinalTarget
        {
            get
            {
                var target = item;
                Reset();
                return target;
            }
        }
        public int Factor => factor1;
        public int MaxDist2 => (int)Math.Pow(factor1 + 1.5f, 2);

        public void Reset()
        {
            curtPoint = null;
            item = null;
            factor1 = 0;
            factor2 = 0;
            factor3 = 0;
        }

        public void Set(Point p, int v1, int v2, int v3 = 0)
        {
            curtPoint = p;
            factor1 = v1;
            factor2 = v2;
            factor3 = v3;
        }

        public void Set(Card c, int v1, int v2, int v3 = 0)
        {
            curtPoint = c.pos;
            item = c;
            factor1 = v1;
            factor2 = v2;
            factor3 = v3;
        }

        public bool TrySet(Point p, int v1, int v2 = 0, int v3 = 0)
        {
            if (p.IsNull()) { return false; }
            if (curtPoint.IsNull())
            {
                Set(p, v1, v2, v3);
                return true;
            }

            if (v1 < factor1 || (v1 == factor1 && v2 < factor2) || (v1 == factor1 && v2 == factor2 && v3 < factor3))
            {
                Set(p, v1, v2, v3);
                return true;
            }

            return false;
        }

        public bool TrySet(Card c, int v1, int v2 = 0, int v3 = 0)
        {
            if (c.IsNull()) { return false; }
            if (curtPoint.IsNull())
            {
                Set(c, v1, v2, v3);
                return true;
            }

            if (v1 < factor1 || (v1 == factor1 && v2 < factor2) || (v1 == factor1 && v2 == factor2 && v3 < factor3))
            {
                Set(c, v1, v2, v3);
                return true;
            }

            return false;
        }
    }
}