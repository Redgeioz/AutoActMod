using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActHarvestMine : AutoAct
{
    public bool simpleIdentify;
    public bool sameFarmfieldOnly;
    public int detRangeSq = 0;
    public bool targetIsWithered = false;
    public bool targetIsWoodTree = false;
    public bool targetCanHarvest = false;
    public static int seedId = -1;
    public static int originalSeedCount = 0;
    public HashSet<Point> field = new();
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
        sameFarmfieldOnly = Settings.SameFarmfieldOnly;
        detRangeSq = Settings.DetRangeSq;
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
                var thing = FindThing(t => IsTarget(t), detRangeSq);
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
                targetPos = FindPosInField(field, c => c.growth.HasValue() && IsTarget(c.sourceObj) && PlantFilter(c));
                field.Remove(targetPos);
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

    public void Init()
    {
        void PrepareForHarvest()
        {
            if (sameFarmfieldOnly && (Pos.IsFarmField || (Pos.sourceObj.id == 88 && Pos.IsWater)))
            {
                InitFarmfield(field, Pos);
            }

            if (!taskHarvest.IsReapSeed || !owner.IsPC)
            {
                return;
            }

            seedId = Pos.sourceObj.id;
            pc.party.members.ForEach(chara =>
            {
                chara.things.ForEach(thing =>
                {
                    if (thing.trait is TraitSeed seed && (seed.row.id == seedId || simpleIdentify))
                    {
                        originalSeedCount += thing.Num;
                    }
                    else
                    {
                        thing.things.ForEach(t =>
                        {
                            if (t.trait is TraitSeed seed && (seed.row.id == seedId || simpleIdentify))
                            {
                                originalSeedCount += t.Num;
                            }
                        });
                    }
                });
            });
        }

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
            SetTarget(Pos.sourceBlock);
        }
        else if (Pos.HasObj)
        {
            if (simpleIdentify && Pos.sourceObj.HasGrowth)
            {
                targetId = Pos.sourceObj.growth.IsTree ? -2 : -3;
                if (Child is TaskHarvest th)
                {
                    PrepareForHarvest();
                }
                return;
            }
            SetTarget(Pos.sourceObj);
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

        PrepareForHarvest();
    }

    public override void OnStart()
    {
        base.OnStart();
        Init();
    }

    bool IsSeedCountEnough()
    {
        if (!owner.IsPC || (seedId < 0 && !simpleIdentify) || Settings.SeedReapingCount <= 0)
        {
            return false;
        }
        var count = 0;
        pc.party.members.ForEach(chara =>
        {
            owner.things.ForEach(t =>
            {
                if (t.trait is TraitSeed seed && (seed.row.id == seedId || simpleIdentify))
                {
                    count += t.Num;
                }
                else
                {
                    t.things.ForEach(tt =>
                    {
                        if (tt.trait is TraitSeed seed && (seed.row.id == seedId || simpleIdentify))
                        {
                            count += tt.Num;
                        }
                    });
                }
            });
        });

        if (count >= Settings.SeedReapingCount + originalSeedCount)
        {
            return true;
        }
        return false;
    }

    bool PlantFilter(Cell cell)
    {
        if (taskHarvest.wasReapSeed)
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