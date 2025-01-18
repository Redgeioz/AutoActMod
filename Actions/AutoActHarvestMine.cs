using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActHarvestMine : AutoAct
{
    public bool simpleIdentify;
    public int detRangeSq = 0;
    public bool hasRange = false;
    public bool targetIsWithered = false;
    public bool targetIsWoodTree = false;
    public bool targetCanHarvest = false;
    public static int SeedId = -1;
    public static int OriginalSeedCount = 0;
    public int targetSeedCount = 0;
    public List<Point> range = [];
    public BaseTaskHarvest initTask;
    public TaskHarvest taskHarvest;
    public TaskMine taskMine;
    public bool isHarvest;
    public bool IsTaskMine => !isHarvest;
    public bool IsTaskHarvest => isHarvest;
    public BaseTaskHarvest Child => child as BaseTaskHarvest;

    public AutoActHarvestMine(BaseTaskHarvest source) : base(source)
    {
        initTask = source;
        if (source is TaskHarvest th)
        {
            isHarvest = true;
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

    public override IEnumerable<Status> Run()
    {
        while (CanProgress())
        {
            if (IsSeedCountEnough())
            {
                yield break;
            }

            Point targetPos;
            if (Child is TaskHarvest && Child.target.HasValue())
            {
                var thing = FindThing(IsTarget, detRangeSq);
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

            if (hasRange)
            {
                targetPos = FindPosInField(range, CommonFilter);
            }
            else
            {
                var tryBetterPath = 0;
                if (isHarvest && Child is TaskMine)
                {
                    tryBetterPath = 2;
                }
                else if (!simpleIdentify && owner.held.HasValue() && owner.held.HasElement(220, 1))
                {
                    tryBetterPath = 1;
                }
                targetPos = FindPos(CommonFilter, detRangeSq, tryBetterPath);
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

        yield return FailOrSuccess();
    }

    public void SetRange(List<Point> range)
    {
        this.range = range;
        hasRange = true;
        simpleIdentify = true;
        targetSeedCount = 0;
    }

    public void Init()
    {
        RestoreChild();
        if (hasRange)
        {
            targetId = -4;
            return;
        }
        else if (Child.target.HasValue())
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
            SetTarget(Pos.sourceBlock);
        }
        else if (Pos.HasObj)
        {
            if (simpleIdentify && Pos.sourceObj.HasGrowth)
            {
                targetId = Pos.sourceObj.growth.IsTree ? -2 : -3;
                if (Child is TaskHarvest)
                {
                    PrepareForHarvest();
                }
                return;
            }
            SetTarget(Pos.sourceObj);
        }

        if (Child is not TaskHarvest)
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

        PrepareForHarvest();
    }

    void PrepareForHarvest()
    {
        if (Settings.SameFarmfieldOnly && (Pos.IsFarmField || (Pos.sourceObj.id == 88 && Pos.IsWater)))
        {
            SetRange(InitFarmfield(Pos));
        }

        if (taskHarvest.IsReapSeed)
        {
            taskHarvest.wasReapSeed = true;
        }

        if (owner.IsPC)
        {
            SeedId = Pos.sourceObj.id;
            OriginalSeedCount = CountSeed();
        }
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

    bool IsSeedCountEnough()
    {
        if (!owner.IsPCParty || !taskHarvest.wasReapSeed || targetSeedCount <= 0)
        {
            return false;
        }

        var count = CountSeed();

        if (count >= targetSeedCount + OriginalSeedCount)
        {
            return true;
        }
        return false;
    }

    public int CountSeed()
    {
        var count = 0;
        pc.party.members.ForEach(chara =>
        {
            chara.things.ForEach(thing =>
            {
                if (thing.trait is TraitSeed seed && (seed.row.id == SeedId || simpleIdentify))
                {
                    count += thing.Num;
                    return;
                }
                thing.things.ForEach(t =>
                {
                    if (t.trait is TraitSeed seed && (seed.row.id == SeedId || simpleIdentify))
                    {
                        count += t.Num;
                    }
                });
            });
        });

        return count;
    }

    public bool PlantFilter(Cell cell)
    {
        if (taskHarvest.wasReapSeed)
        {
            return cell.CanReapSeed();
        }

        if (simpleIdentify)
        {
            return true;
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
        if (!(cell.HasObj && IsTarget(cell.sourceObj)) && !(cell.HasBlock && !cell.HasObj && IsTarget(cell.sourceBlock)))
        {
            return false;
        }

        if (cell.sourceObj.HasGrowth && !PlantFilter(cell))
        {
            return false;
        }

        var originalX = Child.pos.x;
        var originalZ = Child.pos.z;
        Child.pos.Set(cell.x, cell.z);
        Child.SetTarget(owner);
        Child.pos.Set(originalX, originalZ);

        return !Child.IsTooHard;
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