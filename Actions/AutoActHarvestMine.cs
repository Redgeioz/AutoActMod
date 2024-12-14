using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoActMod.Actions;

public class AutoActHarvestMine : AutoAct
{
    public bool simpleIdentify;
    public bool sameFarmfieldOnly;
    public int detRangeSq = 0;
    public bool targetIsWithered = false;
    public bool targetIsWoodTree = false;
    public bool targetCanHarvest = false;
    public int seedId = -1;
    public int targetSeedCount;
    public int originalSeedCount = 0;
    public HashSet<Point> field = new();
    public BaseTaskHarvest initTask;
    public TaskHarvest taskHarvest;
    public TaskMine taskMine;
    public BaseTaskHarvest Child => child as BaseTaskHarvest;

    public AutoActHarvestMine(BaseTaskHarvest source) : base(source)
    {
        initTask = source;
        if (source is TaskHarvest th)
        {
            taskHarvest = th;
            taskMine = new TaskMine()
            {
                // two Child use the same point reference
                pos = th.pos
            };
        }
        else if (source is TaskMine tm)
        {
            taskMine = tm;
            taskHarvest = new TaskHarvest()
            {
                pos = tm.pos
            };
        }

        simpleIdentify = Settings.SimpleIdentify;
        sameFarmfieldOnly = Settings.SameFarmfieldOnly;
        detRangeSq = Settings.DetRangeSq;
        targetSeedCount = Settings.SeedReapingCount;
    }

    public static AutoActHarvestMine TryCreate(AIAct source)
    {
        if (source is TaskMine or TaskHarvest)
        {
            return new AutoActHarvestMine(source as BaseTaskHarvest);
        }
        return null;
    }

    public void Init()
    {
        RestoreChild();
        if (Child.target.HasValue())
        {
            SetTarget(Child.target);
        }
        else if ((!Pos.HasObj || simpleIdentify) && Pos.HasBlock)
        {
            if (simpleIdentify)
            {
                targetId = -1;
                return;
            }
            else
            {
                SetTarget(Pos.sourceBlock);
            }
        }
        else if (Pos.HasObj)
        {
            if (simpleIdentify && Pos.sourceObj.HasGrowth)
            {
                targetId = Pos.sourceObj.growth.IsTree ? -2 : -3;
                return;
            }
            else
            {
                SetTarget(Pos.sourceObj);
            }
        }

        if (Child is not TaskHarvest t)
        {
            return;
        }

        if (Pos.growth.IsNull())
        {
            return;
        }

        var growth = Pos.sourceObj.growth;
        targetIsWithered = growth.IsWithered();
        targetIsWoodTree = growth.IsTree && !growth.CanHarvest();
        targetCanHarvest = targetIsWoodTree ? growth.IsMature : growth.CanHarvest();
        field.Clear();

        if (sameFarmfieldOnly && (Pos.IsFarmField || (Pos.sourceObj.id == 88 && Pos.IsWater)))
        {
            InitFarmfield(field, Pos);
        }

        if (t.IsReapSeed)
        {
            seedId = Pos.sourceObj.id;
            originalSeedCount = 0;
            Child.owner.things.ForEach(thing =>
            {
                if (thing.trait is TraitSeed seed && (seed.row.id == seedId || simpleIdentify))
                {
                    originalSeedCount += thing.Num;
                }
            });
        }
    }

    public override IEnumerable<Status> Run()
    {
        Init();
        yield return StartNextTask();
        while (CanProgress())
        {
            if (IsSeedCountEnough())
            {
                yield break;
            }
            Point targetPos;
            if (Child is TaskHarvest && Child.target.HasValue())
            {
                var thing = FindNextThingTarget(detRangeSq);
                if (thing.IsNull())
                {
                    SayNoTarget();
                    yield break;
                }

                taskHarvest.target = thing;
                SetPosition(thing.pos);

                yield return StartNextTask();
                continue;
            }

            if (Child is TaskHarvest && sameFarmfieldOnly && (Pos.IsFarmField || (Pos.sourceObj.id == 88 && Pos.IsWater)))
            {
                targetPos = FindNextPosInField(field, c => c.growth.HasValue() && IsTarget(c.sourceObj) && PlantFilter(c));
                field.Remove(targetPos);
            }
            else
            {
                bool isMining = owner.held.HasValue() && owner.held.HasElement(220, 1);
                targetPos = FindNextPos(CommonFilter, detRangeSq, !simpleIdentify && isMining);
            }

            if (targetPos.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            SetPosition(targetPos);
            if (taskHarvest.CanProgress())
            {
                yield return SetNextTask(taskHarvest);
            }
            else if (TaskMine.CanMine(Pos, owner.held))
            {
                yield return SetNextTask(taskMine);
            }
            else
            {
                yield return Fail();
            }
        }
        yield return Fail();
    }

    bool IsSeedCountEnough()
    {
        if ((seedId < 0 && !simpleIdentify) || targetSeedCount <= 0)
        {
            return false;
        }
        var count = 0;
        owner.things.ForEach(t =>
        {
            if (t.trait is TraitSeed seed && (seed.row.id == seedId || simpleIdentify))
            {
                count += t.Num;
            }
        });
        if (count >= targetSeedCount + originalSeedCount)
        {
            return true;
        }
        return false;
    }

    bool PlantFilter(Cell cell)
    {
        if (seedId >= 0)
        {
            return cell.CanReapSeed();
        }

        var isWoodTree = targetIsWoodTree && !cell.CanHarvest();
        if ((isWoodTree && cell.growth.IsMature != targetCanHarvest) ||
            (!isWoodTree && cell.growth.CanHarvest() != targetCanHarvest))
        {
            return false;
        }

        // Check if is withered
        if (targetIsWithered && !cell.growth.IsWithered())
        {
            return false;
        }

        return true;
    }

    public bool CommonFilter(Cell cell)
    {
        if (!((cell.HasObj && IsTarget(cell.sourceObj)) || (cell.HasBlock && !cell.HasObj && IsTarget(cell.sourceBlock))))
        {
            return false;
        }

        if (cell.growth.HasValue())
        {
            return PlantFilter(cell);
        }

        return true;
    }

    public void SetPosition(Point p)
    {
        RestoreChild();
        Child.pos.Set(p.x, p.z);
    }

    public void RestoreChild()
    {
        taskHarvest.SetOwner(owner);
        taskMine.SetOwner(owner);
        taskHarvest.isDestroyed = false;
        taskMine.isDestroyed = false;
    }
}