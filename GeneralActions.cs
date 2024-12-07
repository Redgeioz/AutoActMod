using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoAct;

static class OnActionComplete
{
    public static void Run(AIAct __instance, AIAct.Status __result)
    {
        if (!AutoAct.active || __instance != AutoAct.runningTask || __instance is TaskBuild || __result != AIAct.Status.Success)
        {
            return;
        }

        PointSetter.Reset();

        // Debug.Log($"Try continuing {__instance}, status {__instance.status}");
        AutoAct.retry = true;
        if (AutoAct.backToHarvest)
        {
            AutoAct.backToHarvest = false;
            ContinueHarvest();
            return;
        }

        AIAct ai = __instance;
        if (ai is AI_Shear)
        {
            ContinueShear();
        }
        else if ((__instance as DynamicAIAct)?.lang == "actClean__AutoAct")
        {
            ContinueClean();
        }
        else if (ai is AI_Pick)
        {
            ContinuePick();
        }
        else if (ai is AI_Read)
        {
            ContinueRead();
        }
        else if (ai is TaskPlow)
        {
            ContinuePlow();
        }
        else if (ai is TaskDig)
        {
            ContinueDig();
        }
        else if (ai is TaskMine)
        {
            ContinueMine();
        }
        else if (ai is TaskHarvest th)
        {
            if (th.wasReapSeed)
            {
                if (Settings.SeedReapingCount > 0)
                {
                    int count = 0;
                    EClass.pc.things.ForEach(t =>
                    {
                        if (t.trait is TraitSeed seed && (seed.row.id == AutoAct.seedId || Settings.SimpleIdentify))
                        {
                            count += t.Num;
                        }
                    });
                    if (count >= Settings.SeedReapingCount + AutoAct.originalSeedCount)
                    {
                        return;
                    }
                }
            }
            else
            {
                AutoAct.seedId = -1;
            }

            ContinueHarvest();
        }
        else if (ai is TaskDrawWater)
        {
            ContinueDrawWater();
        }
        else if (ai is TaskPourWater)
        {
            ContinuePourWater();
        }
    }

    static void ContinueShear()
    {
        (Chara target, int _) =
        EClass._map.charas
            .Select((Chara chara) =>
            {
                if (!chara.CanBeSheared())
                {
                    return (chara, -1);
                }

                int dist2 = Utils.Dist2(EClass.pc.pos, chara.pos);
                if (dist2 <= 2)
                {
                    return (chara, dist2 == 0 ? 0 : 1);
                }

                PathProgress path = EClass.pc.path;
                path.RequestPathImmediate(EClass.pc.pos, chara.pos, 1, true, -1);
                if (path.state == PathProgress.State.Fail)
                {
                    return (chara, -1);
                }
                return (chara, path.nodes.Count);
            })
            .Where(Tuple => Tuple.Item2 != -1)
            .OrderBy(Tuple => Tuple.Item2)
            .FirstOrDefault();

        if (target == null)
        {
            AutoAct.SayNoTarget();
            return;
        }

        AutoAct.SetNextTask(new AI_Shear { target = target });
    }

    public static void ContinueClean()
    {
        Card held = EClass.pc.held;
        if (held == null || held.trait is not TraitBroom traitBroom)
        {
            return;
        }

        Point targetPoint = GetNextTarget(cell => !cell.HasObj && !cell.HasBlock && cell.Installed == null && (cell.decal > 0 || (cell.effect != null && cell.effect.IsLiquid)));
        if (targetPoint == null)
        {
            AutoAct.SayNoTarget();
            return;
        }

        ActPlan actPlan = new ActPlan { pos = targetPoint };
        traitBroom.TrySetHeldAct(actPlan);

        DynamicAIAct task = new DynamicAIAct("actClean__AutoAct", actPlan.GetAction(), false) { pos = targetPoint };
        AutoAct.SetNextTask(task);
    }

    public static void ContinuePick(Thing refThing = null, bool installed = false)
    {
        if (refThing == null)
        {
            AI_Pick last = AutoAct.runningTask as AI_Pick;
            refThing = last.refThing;
            installed = last.installed;
        }

        Point targetPoint = GetNextTarget(cell =>
        {
            if (cell.HasBlock)
            {
                return false;
            }

            Point p = cell.GetPoint();
            if (installed)
            {
                if (cell.Installed != null && cell.Installed.CanStackTo(refThing))
                {
                    return true;
                }

                return p.Things.Find(t => t.placeState == PlaceState.installed && refThing.CanStackTo(t)) != null;
            }

            return p.Things.Find(t => refThing.CanStackTo(t)) != null;
        });
        if (targetPoint == null)
        {
            AutoAct.SayNoTarget();
            return;
        }

        AI_Pick task = new AI_Pick { pos = targetPoint, refThing = refThing, installed = installed };
        AutoAct.SetNextTask(task);
    }

    static void ContinueRead()
    {
        AI_Read lastTask = AutoAct.runningTask as AI_Read;
        Card book = lastTask.target;
        if (book.isDestroyed)
        {
            return;
        }

        lastTask.Reset();
        AutoAct.SetNextTask(lastTask);
    }

    static void ContinueHarvest()
    {
        TaskHarvest task;
        Point targetPoint = null;
        BaseTaskHarvest lastTask = AutoAct.runningTask as BaseTaskHarvest;
        if (lastTask != null && lastTask.harvestType == BaseTaskHarvest.HarvestType.Thing)
        {
            Thing thing = GetNextThingTarget();
            if (thing == null)
            {
                AutoAct.SayNoTarget();
                return;
            }

            task = new TaskHarvest
            {
                pos = thing.pos.Copy(),
                mode = BaseTaskHarvest.HarvestType.Thing,
                target = thing
            };

            AutoAct.SetNextTask(task);
            return;
        }
        else if (lastTask != null && Settings.SameFarmfieldOnly && (lastTask.pos.IsFarmField || (lastTask.pos.sourceObj.id == 88 && lastTask.pos.IsWater)))
        {
            targetPoint = GetNextFarmfieldTarget();
            AutoAct.curtField.Remove(targetPoint);
        }
        else
        {
            bool isRemoveTrack = EClass.pc.held != null && EClass.pc.held.Thing.HasElement(230, 1);
            targetPoint = GetNextTarget(CommonFilter, !Settings.SimpleIdentify && !isRemoveTrack);
        }

        if (targetPoint == null)
        {
            AutoAct.SayNoTarget();
            return;
        }

        if (!targetPoint.HasObj && !targetPoint.HasBlock)
        {
            AutoAct.SetNextTask(new AI_Goto(targetPoint, 0));
            AutoAct.backToHarvest = true;
            return;
        }

        task = TaskHarvest.TryGetAct(EClass.pc, targetPoint);
        if (task != null)
        {
            AutoAct.SetNextTask(task);
        }
        else if (TaskMine.CanMine(targetPoint, EClass.pc.held))
        {
            AutoAct.SetNextTask(new TaskMine { pos = targetPoint });
            AutoAct.backToHarvest = true;
        }
    }

    static void ContinueDig()
    {
        TaskDig t = AutoAct.runningTask as TaskDig;
        if (EClass._zone.IsRegion)
        {
            TaskDig repeatedTaskDig = new TaskDig
            {
                pos = EClass.pc.pos.Copy(),
                mode = TaskDig.Mode.RemoveFloor
            };
            AutoAct.SetNextTask(repeatedTaskDig);
            return;
        }

        Point targetPoint = GetNextTarget2(
            cell => AutoAct.IsTarget(cell.sourceFloor) && !cell.HasBlock && !cell.HasObj
        );
        if (targetPoint == null)
        {
            return;
        }

        TaskDig task = new TaskDig
        {
            pos = targetPoint,
            mode = TaskDig.Mode.RemoveFloor
        };
        AutoAct.SetNextTask(task);
    }

    static void ContinueMine()
    {
        Point targetPoint = GetNextTarget(CommonFilter);
        if (targetPoint == null)
        {
            AutoAct.SayNoTarget();
            return;
        }

        TaskMine task = new TaskMine { pos = targetPoint };
        AutoAct.SetNextTask(task);
    }

    static void ContinuePlow()
    {
        Point targetPoint = GetNextTarget2(
            cell => !cell.HasBlock && !cell.HasObj && cell.Installed == null && !cell.IsTopWater && !cell.IsFarmField && (cell.HasBridge ? cell.sourceBridge : cell.sourceFloor).tag.Contains("soil")
        );
        if (targetPoint == null)
        {
            return;
        }

        TaskPlow task = new TaskPlow { pos = targetPoint };
        AutoAct.SetNextTask(task);
    }

    static void ContinueDrawWater()
    {
        if (AutoAct.drawWaterPoint.IsWater)
        {
            // Waite for repeated water drawing
            return;
        }

        if (EClass.pc.held.trait is not TraitToolWaterPot pot || pot.owner.c_charges >= pot.MaxCharge)
        {
            return;
        }

        Point targetPoint = GetNextTarget(cell =>
        {
            if (!cell.IsTopWaterAndNoSnow)
            {
                return false;
            }

            Point p = cell.GetPoint();
            return (p.HasBridge ? p.matBridge : p.matFloor).alias == AutoAct.targetTypeName && !cell.HasObj && !cell.HasBlock;
        });

        if (targetPoint == null)
        {
            return;
        }

        TaskDrawWater task = new TaskDrawWater { pot = pot, pos = targetPoint };
        AutoAct.SetNextTask(task);
        AutoAct.drawWaterPoint = targetPoint.Copy();
    }

    static void ContinuePourWater()
    {
        if (EClass.pc.held.trait is not TraitToolWaterPot pot)
        {
            return;
        }

        if (AutoAct.pourCount < Settings.PourDepth - 1)
        {
            return;
        }

        if (pot.owner.c_charges == 0)
        {
            Card nextPot = EClass.pc.things.Find(t => t.trait is TraitToolWaterPot twp && twp.owner.c_charges > 0);
            if (nextPot != null)
            {
                pot = nextPot.trait as TraitToolWaterPot;
                EClass.pc.HoldCard(nextPot);
            }
            else
            {
                return;
            }
        }

        Point targetPoint = GetNextTarget2(cell => !cell.HasBridge && AutoAct.IsTarget(cell.sourceFloor));
        if (targetPoint == null)
        {
            return;
        }

        TaskPourWater task = new TaskPourWater { pos = targetPoint, pot = pot };
        AutoAct.SetNextTask(task);
        AutoAct.pourCount = 0;
    }

    static bool PlantFilter(Cell cell)
    {
        if (AutoAct.seedId >= 0)
        {
            return cell.CanReapSeed();
        }

        bool isWoodTree = AutoAct.targetIsWoodTree && !cell.CanHarvest();
        if ((isWoodTree && cell.growth.IsMature != AutoAct.targetCanHarvest) ||
            (!isWoodTree && cell.growth.CanHarvest() != AutoAct.targetCanHarvest))
        {
            return false;
        }

        // Check if is withered
        if (AutoAct.targetGrowth == 4 && cell.growth.stage.idx != AutoAct.targetGrowth)
        {
            return false;
        }

        return true;
    }

    static bool CommonFilter(Cell cell)
    {
        if (!((cell.HasObj && AutoAct.IsTarget(cell.sourceObj)) || (cell.HasBlock && !cell.HasObj && AutoAct.IsTarget(cell.sourceBlock))))
        {
            return false;
        }

        if (cell.growth != null)
        {
            return PlantFilter(cell);
        }
        return true;
    }

    static Point GetNextTarget(Func<Cell, bool> filter, bool tryBetterPath = false)
    {
        List<(Point, int, int)> list = new List<(Point, int, int)>();
        EClass._map.bounds.ForeachCell(cell =>
        {
            if (!filter(cell))
            {
                return;
            }

            Point p = cell.GetPoint();
            int dist2 = Utils.Dist2(EClass.pc.pos, p);
            if (dist2 > Settings.DetRangeSq)
            {
                return;
            }

            int dist2ToLastPoint = AutoAct.runningTask is TaskPoint lastTask ? Utils.Dist2(lastTask.pos, p) : dist2;
            if (dist2 <= 2)
            {
                PointSetter.TrySet(p, dist2 == 0 ? -1 : 0, dist2ToLastPoint, 0);
                return;
            }

            list.Add((p, dist2, dist2ToLastPoint));
        });

        foreach ((Point p, int dist2, int dist2ToLastPoint) in list.OrderBy(tuple => tuple.Item2))
        {
            if (PointSetter.FinalPoint != null && dist2 > PointSetter.MaxDist2)
            {
                break;
            }

            bool TryDestroyObstacle()
            {
                if (dist2 > 5 || dist2 < 4 || !tryBetterPath)
                {
                    return false;
                }

                int dx = p.x - EClass.pc.pos.x;
                int dz = p.z - EClass.pc.pos.z;
                Point obstacle = new Point(EClass.pc.pos.x + dx / 2, EClass.pc.pos.z + dz / 2);
                bool CanDestroyObstacle() =>
                    obstacle.HasBlock
                    // soil block
                    && (obstacle.sourceBlock.id == 1 || obstacle.sourceBlock.id == 167)
                    // wall frame
                    && (!obstacle.HasObj || obstacle.sourceObj.id == 24);
                if (CanDestroyObstacle())
                {
                    PointSetter.TrySet(obstacle, 1, dist2ToLastPoint, 0);
                    return true;
                }
                else if (!obstacle.HasBlock && !obstacle.HasObj)
                {
                    obstacle = new Point(p.x - dx / 2, p.z - dz / 2);
                    if (CanDestroyObstacle())
                    {
                        PointSetter.TrySet(obstacle, 1, dist2ToLastPoint, 0);
                        return true;
                    }
                }

                return false;
            }

            PathProgress path = EClass.pc.path;
            path.RequestPathImmediate(EClass.pc.pos, p, 1, true, -1);
            if (path.state == PathProgress.State.Fail)
            {
                TryDestroyObstacle();
                continue;
            }

            if (path.nodes.Count >= dist2 && TryDestroyObstacle())
            {
                continue;
            }

            int d2 = 0;
            if (p.HasBlock)
            {
                d2 = Math.Abs(AutoAct.GetDelta(p, EClass.pc.pos, EClass.pc.dir).Item2);
            }
            PointSetter.TrySet(p, Math.Min(path.nodes.Count, dist2ToLastPoint), dist2ToLastPoint, d2);
        }
        // if (targetPoint != null)
        // {
        //     if (targetPoint.cell.growth != null)
        //     {
        //         Debug.Log($"Target stage: {targetPoint.cell.growth.stage.idx}, OriginalStage: {AutoAct.targetGrowth}, CanHarvest: {targetPoint.cell.growth.CanHarvest()}");
        //     }
        //     Debug.Log($"Target: {targetPoint.cell.sourceObj.id} | {targetPoint.cell.sourceObj.name} | {targetPoint}");
        //     Debug.Log($"Target: {targetPoint.cell.sourceBlock.id} | {targetPoint.cell.sourceBlock.name} | {targetPoint}");
        //     Debug.Log($"Target should be: {AutoAct.targetTypeId}, self: {EClass.pc.pos}");
        // }

        return PointSetter.FinalPoint;
    }

    static Thing GetNextThingTarget()
    {
        List<(Thing, int, int)> list = new List<(Thing, int, int)>();
        EClass._map.bounds.ForeachCell(cell =>
        {
            Point p = cell.GetPoint();
            if (!p.HasThing)
            {
                return;
            }

            int dist2 = Utils.Dist2(EClass.pc.pos, p);
            if (dist2 > Settings.DetRangeSq)
            {
                return;
            }

            Thing thing = p.Things.Find((Thing t) => t.Name == AutoAct.targetTypeName);
            if (thing == null)
            {
                return;
            }

            int dist2ToLastPoint = Utils.Dist2((AutoAct.runningTask as TaskPoint).pos, p);
            if (dist2 <= 2)
            {
                PointSetter.TrySet(p, dist2 == 0 ? 0 : 1, dist2ToLastPoint);
                return;
            }

            list.Add((thing, dist2, dist2ToLastPoint));
        });

        foreach ((Thing thing, int dist2, int dist2ToLastPoint) in list.OrderBy(tuple => tuple.Item2))
        {
            if (PointSetter.FinalPoint != null && dist2 > PointSetter.MaxDist2)
            {
                break;
            }

            PathProgress path = EClass.pc.path;
            path.RequestPathImmediate(EClass.pc.pos, thing.pos, 1, true, -1);
            if (path.state == PathProgress.State.Fail)
            {
                continue;
            }

            PointSetter.TrySet(thing.pos, path.nodes.Count, dist2ToLastPoint);
        }

        (Thing target, int _, int _) = list.OrderBy(tuple => tuple.Item2).ThenBy(tuple => tuple.Item3).FirstOrDefault();
        // if (target == null) {
        //     Debug.Log("Target: null");
        // } else {
        //     Debug.Log($"Target: {target.id} | {target.Name} | {target.pos}");
        // }
        return target;
    }

    static Point GetNextTarget2(Func<Cell, bool> filter)
    {
        List<(Point, int, int, int)> list = new List<(Point, int, int, int)>();
        EClass._map.bounds.ForeachCell(cell =>
        {
            if (!filter(cell))
            {
                return;
            }

            Point p = cell.GetPoint();

            int dist2 = Utils.Dist2(EClass.pc.pos, p);
            int dist2ToLastPoint = Utils.Dist2((AutoAct.runningTask as TaskPoint).pos, p);
            if (Settings.StartFromCenter)
            {
                int max = AutoAct.MaxDeltaToStartPoint(p);
                if (max > Settings.BuildRangeW / 2)
                {
                    return;
                }

                if (max <= 1)
                {
                    PointSetter.TrySet(p, max, max - 1, dist2ToLastPoint);
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
            (Point p, int max, int dist2, int dist2ToLastPoint) = item;
            if (PointSetter.FinalPoint != null &&
                ((Settings.StartFromCenter && max > PointSetter.Factor) ||
                (!Settings.StartFromCenter && dist2ToLastPoint > PointSetter.Factor)))
            {
                break;
            }

            PathProgress path = EClass.pc.path;
            if (Settings.StartFromCenter)
            {
                path.RequestPathImmediate(EClass.pc.pos, p, 1, false, -1);
                if (path.state == PathProgress.State.Fail)
                {
                    continue;
                }

                PointSetter.TrySet(p, max, path.nodes.Count, dist2ToLastPoint);
            }
            else
            {
                (int d1, int d2) = AutoAct.GetDelta(p);
                if (d1 < 0 || d2 < 0 || d1 >= Settings.BuildRangeH || d2 >= Settings.BuildRangeW)
                {
                    continue;
                }

                if (dist2 > 2)
                {
                    path.RequestPathImmediate(EClass.pc.pos, p, 1, false, -1);
                    if (path.state == PathProgress.State.Fail)
                    {
                        continue;
                    }
                }

                PointSetter.TrySet(p, dist2ToLastPoint, d1, d2);
            }
        }

        return PointSetter.FinalPoint;
    }

    static Point GetNextFarmfieldTarget()
    {
        List<(Point, int, int)> list = new List<(Point, int, int)>();
        foreach (Point p in AutoAct.curtField)
        {
            Cell cell = p.cell;
            if (cell.sourceObj.id != AutoAct.targetTypeId || !(cell.HasObj || cell.HasBlock))
            {
                continue;
            }

            if (cell.growth != null)
            {
                if (!PlantFilter(cell))
                {
                    continue;
                }
            }
            else
            {
                continue;
            }

            int dist2 = Utils.Dist2(EClass.pc.pos, p);
            int dist2ToLastPoint = Utils.Dist2((AutoAct.runningTask as TaskPoint).pos, p);
            int max = Utils.MaxDelta(EClass.pc.pos, p);
            if (max <= 1)
            {
                PointSetter.TrySet(p, max - 1, dist2ToLastPoint);
                continue;
            }

            list.Add((p, dist2, dist2ToLastPoint));
        }

        foreach ((Point p, int dist2, int dist2ToLastPoint) in list.OrderBy(tuple => tuple.Item2))
        {
            if (PointSetter.FinalPoint != null && dist2 > PointSetter.MaxDist2)
            {
                break;
            }

            PathProgress path = EClass.pc.path;
            path.RequestPathImmediate(EClass.pc.pos, p, 1, true, -1);
            if (path.state == PathProgress.State.Fail)
            {
                continue;
            }

            PointSetter.TrySet(p, path.nodes.Count, dist2ToLastPoint);
        }
        // if (targetPoint != null && targetPoint.cell.growth != null)
        // {
        //     Debug.Log($"Target stage: {targetPoint.cell.growth.stage.idx}, original stage: {AutoAct.targetGrowth}, can harvest: {targetPoint.cell.growth.CanHarvest()}");
        //     Debug.Log($"Target: {targetPoint?.cell.sourceObj.id} | {targetPoint?.cell.sourceObj.name} | {targetPoint}");
        //     Debug.Log($"Target should be: {AutoAct.targetTypeId}");
        // }
        return PointSetter.FinalPoint;
    }
}
