using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace AutoAct
{
    [HarmonyPatch]
    static class OnCreateProgress_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TaskHarvest), "OnCreateProgress")]
        static void TaskHarvest_Patch(TaskHarvest __instance)
        {
            AutoAct.UpdateState(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TaskDig), "OnCreateProgress")]
        static void TaskDig_Patch(TaskDig __instance)
        {
            AutoAct.UpdateState(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TaskMine), "OnCreateProgress")]
        static void TaskMine_Patch(TaskMine __instance)
        {
            AutoAct.UpdateState(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TaskPlow), "OnCreateProgress")]
        static void TaskPlow_Patch(TaskPlow __instance)
        {
            AutoAct.UpdateState(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TaskDrawWater), "OnCreateProgress")]
        static void TaskDrawWater_Patch(TaskDrawWater __instance)
        {
            AutoAct.UpdateState(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TaskPourWater), "OnCreateProgress")]
        static void TaskPourWater_Patch(TaskPourWater __instance)
        {
            if (AutoAct.active && !AutoAct.IsTarget(__instance.pos.sourceFloor))
            {
                AutoAct.pourCount += 1;
                if (AutoAct.pourCount >= Settings.PourDepth)
                {
                    __instance.Success();
                }
            }
            else
            {
                AutoAct.UpdateState(__instance);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DynamicAct), "Perform")]
        static void DaynamicAct_Patch(DynamicAct __instance)
        {
            // Debug.Log($"DynamicAct: {__instance.id}");
            if (!AutoAct.IsSwitchOn) { return; }
            if (__instance.id == "actClean")
            {
                AutoAct.active = true;
                AIAct_Success_Patch.ContinueClean();
            }
            else if (__instance.id == "actPickOne")
            {
                if (!(Scene.HitPoint.ListCards()[0] is Thing refThing)) { return; }
                AutoAct.active = true;
                AIAct_Success_Patch.ContinuePick(refThing);
            }
            else if (__instance.id == "actHold")
            {
                if (!(Scene.HitPoint.ListCards()[0] is Thing refThing)) { return; }
                AutoAct.active = true;
                AIAct_Success_Patch.ContinuePick(refThing, refThing.placeState == PlaceState.installed);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DynamicAIAct), "Perform")]
        static void DynamicAIAct_Patch(DynamicAIAct __instance)
        {
            if (__instance.lang == "actClean__AutoAct")
            {
                AutoAct.UpdateState(__instance);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AI_Read), "Run")]
        static void AIRead_Patch(AI_Read __instance)
        {
            AutoAct.UpdateState(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AI_Shear), "Run")]
        static void AIShear_Patch(AI_Shear __instance)
        {
            AutoAct.UpdateState(__instance);
        }
    }

    [HarmonyPatch(typeof(AIAct), "Success")]
    static class AIAct_Success_Patch
    {
        [HarmonyPostfix]
        static void Postfix(AIAct __instance)
        {
            if (!AutoAct.active || __instance != EClass.pc.ai || __instance is TaskBuild)
            {
                return;
            }

            // Debug.Log($"Try continuing {__instance}, status {__instance.status}");
            if (__instance.status != AIAct.Status.Success)
            {
                Debug.LogWarning($"AutoAct: Failed to continue {__instance}");
                return;
            }

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
                    int count = 0;
                    EClass.pc.things.ForEach(t =>
                    {
                        if (t.trait is TraitSeed seed && seed.row.id == AutoAct.seedId)
                        {
                            count += t.Num;
                        }
                    });
                    if (Settings.SeedReapingCount > 0 && count >= Settings.SeedReapingCount + AutoAct.originalSeedCount)
                    {
                        return;
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

                    PathProgress path = EClass.pc.path;
                    path.RequestPathImmediate(EClass.pc.pos, chara.pos, 1, false, -1);
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
                return;
            }

            AutoAct.SetNextTask(new AI_Shear { target = target });
        }

        public static void ContinueClean()
        {
            Card held = EClass.pc.held;
            if (held == null || !(held.trait is TraitBroom traitBroom))
            {
                return;
            }

            Point targetPoint = GetNextTarget(cell => !cell.HasObj && !cell.HasBlock && cell.Installed == null && (cell.decal > 0 || (cell.effect != null && cell.effect.IsLiquid)));
            if (targetPoint == null)
            {
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
                AI_Pick last = EClass.pc.ai as AI_Pick;
                refThing = last.refThing;
                installed = last.installed;
            }

            Point targetPoint = GetNextTarget(cell =>
            {
                if (cell.HasBlock || cell.HasObj)
                {
                    return false;
                }

                if (installed)
                {
                    return cell.Installed != null && refThing.CanStackTo(refThing);
                }

                Point p = cell.GetPoint();
                return p.HasThing && p.Things.Find(t => refThing.CanStackTo(t)) != null;
            });
            if (targetPoint == null)
            {
                return;
            }

            AI_Pick task = new AI_Pick { pos = targetPoint, refThing = refThing, installed = installed };
            AutoAct.SetNextTask(task);
        }

        static void ContinueRead()
        {
            AI_Read lastTask = EClass.pc.ai as AI_Read;
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
            BaseTaskHarvest lastTask = EClass.pc.ai as BaseTaskHarvest;
            if (lastTask != null && lastTask.harvestType == BaseTaskHarvest.HarvestType.Thing)
            {
                Thing thing = GetNextThingTarget();
                if (thing == null)
                {
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
                if (!AutoAct.curtField.Contains(lastTask.pos))
                {
                    AutoAct.InitFarmfield(lastTask.pos, lastTask.pos.IsWater);
                }
                targetPoint = GetNextFarmfieldTarget();
            }
            else
            {
                targetPoint = GetNextTarget(CommonFilter, !Settings.SimpleIdentify);
            }

            if (targetPoint == null)
            {
                return;
            }

            if (TaskMine.CanMine(targetPoint, EClass.pc.held))
            {
                AutoAct.SetNextTask(new TaskMine { pos = targetPoint });
                AutoAct.backToHarvest = true;
                return;
            }
            else if (!targetPoint.HasObj && !targetPoint.HasBlock)
            {
                AutoAct.SetNextTask(new AI_Goto(targetPoint, 0));
                AutoAct.backToHarvest = true;
                return;
            }

            task = TaskHarvest.TryGetAct(EClass.pc, targetPoint);
            if (task == null)
            {
                return;
            }

            AutoAct.SetNextTask(task);
        }

        static void ContinueDig()
        {
            TaskDig t = EClass.pc.ai as TaskDig;
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

            if (!(EClass.pc.held.trait is TraitToolWaterPot pot) || pot.owner.c_charges >= pot.MaxCharge)
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
                return (p.HasBridge ? p.matBridge : p.matFloor).alias == AutoAct.targetTypeStr && !cell.HasObj && !cell.HasBlock;
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
            if (!(EClass.pc.held.trait is TraitToolWaterPot pot))
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

        static bool CommonFilter(Cell cell)
        {
            if (!((cell.HasObj && AutoAct.IsTarget(cell.sourceObj)) || (cell.HasBlock && !cell.HasObj && AutoAct.IsTarget(cell.sourceBlock))))
            {
                return false;
            }

            if (cell.growth != null)
            {
                if (AutoAct.seedId >= 0 && cell.CanReapSeed())
                {
                    return true;
                }

                if (cell.growth.CanHarvest() != AutoAct.targetCanHarvest)
                {
                    return false;
                }

                // Check if is withered
                if (AutoAct.targetGrowth == 4 && cell.growth.stage.idx != AutoAct.targetGrowth)
                {
                    return false;
                }
            }
            return true;
        }

        static Point GetNextTarget(Func<Cell, bool> filter, bool tryBetterPath = false)
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
                if (dist2 > Settings.DetRangeSq)
                {
                    return;
                }

                int dist2ToLastPoint = EClass.pc.ai is TaskPoint ? Utils.Dist2((EClass.pc.ai as TaskPoint).pos, p) : dist2;
                if (dist2 <= 2)
                {
                    list.Add((p, dist2 == 0 ? 0 : 1, dist2ToLastPoint, 0));
                    return;
                }

                PathProgress path = EClass.pc.path;
                bool TryDestroyObstacle()
                {
                    if (dist2 > 5 || dist2 < 4 || !tryBetterPath)
                    {
                        return false;
                    }

                    int dx = p.x - EClass.pc.pos.x;
                    int dz = p.z - EClass.pc.pos.z;
                    Point obstacle = new Point(EClass.pc.pos.x + dx / 2, EClass.pc.pos.z + dz / 2);
                    bool CanDestroyObstacle() => !obstacle.HasObj && obstacle.HasBlock && (obstacle.sourceBlock.id == 1 || obstacle.sourceBlock.id == 167);
                    if (CanDestroyObstacle())
                    {
                        list.Add((obstacle, 1, dist2ToLastPoint, 0));
                        return true;
                    }
                    else if (!obstacle.HasBlock && !obstacle.HasObj)
                    {
                        obstacle = new Point(p.x - dx / 2, p.z - dz / 2);
                        if (CanDestroyObstacle())
                        {
                            list.Add((obstacle, 1, dist2ToLastPoint, 0));
                            return true;
                        }
                    }

                    return false;
                }

                bool TryCheckDiagonalPoint()
                {
                    if (!tryBetterPath)
                    {
                        return false;
                    }

                    int min = 0;
                    Point np = null;
                    Utils.ForEachNeighborPoint(p, pt =>
                    {
                        if (pt.HasBlock || pt.HasObj)
                        {
                            return;
                        }

                        path.RequestPathImmediate(EClass.pc.pos, pt, 0, false, -1);
                        if (path.state == PathProgress.State.Fail)
                        {
                            return;
                        }

                        if (np == null)
                        {
                            min = path.nodes.Count;
                            np = pt;
                        }
                        else if (path.nodes.Count < min)
                        {
                            min = path.nodes.Count;
                            np = pt;
                        }
                    });

                    if (np != null)
                    {
                        list.Add((np, min, dist2ToLastPoint, 0));
                        return true;
                    }

                    return false;
                }

                path.RequestPathImmediate(EClass.pc.pos, p, 1, false, -1);
                if (path.state == PathProgress.State.Fail)
                {
                    if (!TryCheckDiagonalPoint())
                    {
                        TryDestroyObstacle();
                    }
                    return;
                }

                if (path.nodes.Count > dist2 && (TryDestroyObstacle() || TryCheckDiagonalPoint()))
                {
                    return;
                }

                int d2 = 0;
                if (cell.HasBlock)
                {
                    d2 = Math.Abs(AutoAct.GetDelta(p, EClass.pc.pos, EClass.pc.dir).Item2);
                }

                list.Add((p, path.nodes.Count, dist2ToLastPoint, d2));
            });

            (Point targetPoint, int _, int _, int _) = list.OrderBy(tuple => tuple.Item2).ThenBy(tuple => tuple.Item3).ThenBy(tuple => tuple.Item4).FirstOrDefault();

            // if (targetPoint != null)
            // {
            //     if (targetPoint.cell.growth != null)
            //     {
            //         Debug.Log($"Target stage: {targetPoint.cell.growth.stage.idx}, OriginalStage: {AutoAct.targetGrowth}, CanHarvest: {targetPoint.cell.growth.CanHarvest()}");
            //     }
            //     Debug.Log($"Target: {targetPoint.cell.sourceObj.id} | {targetPoint.cell.sourceObj.name} | {targetPoint}");
            //     Debug.Log($"Target: {targetPoint.cell.sourceBlock.id} | {targetPoint.cell.sourceBlock.name} | {targetPoint}");
            //     Debug.Log($"Target should be: {AutoAct.targetType}, self: {EClass.pc.pos}");
            // }

            return targetPoint;
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

                Thing thing = p.Things.Find((Thing t) => t.Name == AutoAct.targetTypeStr);
                if (thing == null)
                {
                    return;
                }

                int dist2ToLastPoint = Utils.Dist2((EClass.pc.ai as TaskPoint).pos, p);
                if (dist2 <= 2)
                {
                    list.Add((thing, dist2 == 0 ? 0 : 1, dist2ToLastPoint));
                    return;
                }

                PathProgress path = EClass.pc.path;
                path.RequestPathImmediate(EClass.pc.pos, p, 1, false, -1);
                if (path.state == PathProgress.State.Fail)
                {
                    return;
                }

                list.Add((thing, path.nodes.Count, dist2ToLastPoint));
            });

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

                if (AutoAct.startPoint == null)
                {
                    Debug.LogWarning("AutoAct StartPoint: null");
                    return;
                }

                Point p = cell.GetPoint();
                PathProgress path = EClass.pc.path;
                if (Settings.StartFromCenter)
                {
                    int max = AutoAct.MaxDeltaToStartPoint(p);
                    if (max > Settings.BuildRangeW / 2)
                    {
                        return;
                    }

                    int dist2 = Utils.Dist2((EClass.pc.ai as TaskPoint).pos, p);
                    if (max <= 1)
                    {
                        list.Add((p, max, max - 1, dist2));
                        return;
                    }

                    path.RequestPathImmediate(EClass.pc.pos, p, 1, false, -1);
                    if (path.state == PathProgress.State.Fail)
                    {
                        return;
                    }

                    list.Add((p, max, path.nodes.Count, dist2));
                    return;
                }

                (int d1, int d2) = AutoAct.GetDelta(p);
                if (d1 < 0 || d2 < 0 || d1 >= Settings.BuildRangeH || d2 >= Settings.BuildRangeW)
                {
                    return;
                }

                path.RequestPathImmediate(EClass.pc.pos, p, 1, false, -1);
                if (path.state == PathProgress.State.Fail)
                {
                    return;
                }

                if (d1 % 2 == 1)
                {
                    d2 *= -1;
                }

                list.Add((p, d1, d2, 0));
            });

            (Point targetPoint, int _, int _, int _) = list
                .OrderBy(tuple => tuple.Item2)
                .ThenBy(tuple => tuple.Item3)
                .ThenBy(tuple => tuple.Item4)
                .FirstOrDefault();
            return targetPoint;
        }

        static Point GetNextFarmfieldTarget()
        {
            List<(Point, int, int)> list = new List<(Point, int, int)>();
            foreach (Point p in AutoAct.curtField)
            {
                Cell cell = p.cell;
                if (cell.sourceObj.id != AutoAct.targetType || !(cell.HasObj || cell.HasBlock))
                {
                    continue;
                }

                if (cell.growth != null)
                {
                    if (cell.growth.CanHarvest() != AutoAct.targetCanHarvest)
                    {
                        continue;
                    }

                    // Check if is withered
                    if (AutoAct.targetGrowth == 4 && cell.growth.stage.idx != AutoAct.targetGrowth)
                    {
                        continue;

                    }
                }
                else
                {
                    continue;
                }

                int dist2 = Utils.Dist2((EClass.pc.ai as TaskPoint).pos, p);
                int max = Utils.MaxDelta(EClass.pc.pos, p);
                if (max <= 1)
                {
                    list.Add((p, max - 1, dist2));
                    continue;
                }

                PathProgress path = EClass.pc.path;
                path.RequestPathImmediate(EClass.pc.pos, p, 1, false, -1);
                if (path.state == PathProgress.State.Fail)
                {
                    continue;
                }

                list.Add((p, path.nodes.Count, dist2));
            }

            (Point targetPoint, int _, int _) = list
                .OrderBy(tuple => tuple.Item2)
                .ThenBy(tuple => tuple.Item3)
                .FirstOrDefault();
            // if (targetPoint != null && targetPoint.cell.growth != null)
            // {
            //     Debug.Log($"Target stage: {targetPoint.cell.growth.stage.idx}, original stage: {AutoAct.targetGrowth}, can harvest: {targetPoint.cell.growth.CanHarvest()}");
            //     Debug.Log($"Target: {targetPoint?.cell.sourceObj.id} | {targetPoint?.cell.sourceObj.name} | {targetPoint}");
            //     Debug.Log($"Target should be: {AutoAct.targetType}");
            // }
            return targetPoint;
        }
    }
}