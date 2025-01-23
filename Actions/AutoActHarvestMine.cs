using System;
using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActHarvestMine : AutoAct
{
    public int simpleIdentify;
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
    public BaseTaskHarvest Child => child as BaseTaskHarvest;
    public bool SimpleIdentify => simpleIdentify > 0;

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
                targetPos = FindPos(CommonFilter, range: range);
            }
            else
            {
                var tryBetterPath = 0;
                if (isHarvest && Child is TaskMine)
                {
                    tryBetterPath = 2;
                }
                else if (!SimpleIdentify && owner.held.HasValue() && owner.held.HasElement(220, 1))
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

            RestoreChild();
        }

        yield return FailOrSuccess();
    }

    public void SetRange(List<Point> range)
    {
        this.range = range;
        hasRange = true;
        simpleIdentify = 2;
        targetSeedCount = 0;
    }

    public bool IsWoodTree(GrowSystem growth) => growth.IsTree && !growth.CanHarvest();

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
        else if ((!Pos.HasObj || SimpleIdentify) && Pos.HasBlock)
        {
            if (SimpleIdentify)
            {
                targetId = -1;
                return;
            }
            SetTarget(Pos.sourceBlock);
        }
        else if (Pos.HasObj)
        {
            if (SimpleIdentify && Pos.sourceObj.HasGrowth)
            {
                targetId = IsWoodTree(Pos.growth) ? -2 : -3;
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

        PrepareForHarvest();
    }

    void PrepareForHarvest()
    {
        if (Pos.growth.HasValue())
        {
            var growth = Pos.sourceObj.growth;
            targetIsWithered = growth.IsWithered();
            targetIsWoodTree = IsWoodTree(Pos.growth);
            targetCanHarvest = targetIsWoodTree ? growth.IsMature : growth.CanHarvest();
        }

        if (Settings.SameFarmfieldOnly && (Pos.IsFarmField || (Pos.sourceObj.id == 88 && Pos.IsWater)))
        {
            range = InitFarmField(Pos);
            hasRange = true;
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
        if (Settings.SimpleIdentify == 2 && (CanHarvest(owner, Pos) || TaskMine.CanMine(Pos, owner.held)))
        {
            return;
        }

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
                if (thing.trait is TraitSeed seed && (seed.row.id == SeedId || SimpleIdentify))
                {
                    count += thing.Num;
                    return;
                }
                thing.things.ForEach(t =>
                {
                    if (t.trait is TraitSeed seed && (seed.row.id == SeedId || SimpleIdentify))
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

        if (simpleIdentify == 2)
        {
            return true;
        }

        var isWoodTree = targetIsWoodTree && !cell.CanHarvest();
        if ((isWoodTree && cell.growth.IsMature != targetCanHarvest) ||
            (!isWoodTree && cell.growth.CanHarvest() != targetCanHarvest))
        {
            return false;
        }

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
        if (taskHarvest.CanProgress() || TaskMine.CanMine(Pos, owner.held))
        {
            Child.SetTarget(owner);
            Child.pos.Set(originalX, originalZ);
            return !Child.IsTooHard;
        }
        else
        {
            Child.pos.Set(originalX, originalZ);
            return false;
        }
    }

    public void SetPosition(Point p)
    {
        Child.pos.Set(p.x, p.z);
    }

    public void RestoreChild()
    {
        taskHarvest.SetOwner(owner);
        taskMine.SetOwner(owner);
        taskHarvest.isDestroyed = false;
        taskHarvest.harvestingCrop = false;
        taskMine.isDestroyed = false;
    }

    public static bool CanHarvest(Chara c, Point p)
    {
        Thing t = c.Tool;
        bool hasTool = t != null && (t.HasElement(225) || t.HasElement(220));
        bool hasDiggingTool = t != null && t.HasElement(230);
        if (t != null)
        {
            if (t.trait is TraitToolShears)
            {
                return false;
            }

            if (t.trait is TraitToolWaterCan)
            {
                return false;
            }

            if (t.trait is TraitToolMusic)
            {
                return false;
            }

            if (t.trait is TraitToolSickle && !p.cell.CanReapSeed())
            {
                return false;
            }
        }

        if (p.HasObj && IsValidTarget(p.sourceObj.reqHarvest))
        {
            return true;
        }

        if (p.HasThing)
        {
            for (int num = p.Things.Count - 1; num >= 0; num--)
            {
                t = p.Things[num];
                if (t.trait.ReqHarvest != null && IsValidTarget(t.trait.ReqHarvest.Split(',')))
                {
                    return true;
                }
            }

            for (int num2 = p.Things.Count - 1; num2 >= 0; num2--)
            {
                t = p.Things[num2];
                if (!t.isHidden && !t.isMasked && t.trait.CanBeDisassembled && c.Tool?.trait is TraitToolHammer)
                {
                    return true;
                }
            }
        }

        return false;
        bool IsValidTarget(string[] raw)
        {
            if (raw[0] == "digging")
            {
                return hasDiggingTool;
            }

            bool num3 = p.cell.CanHarvest();
            int num4 = num3 ? 250 : sources.elements.alias[raw[0]].id;
            bool flag = !num3 && num4 != 250;
            if (!flag && t != null && !t.trait.CanHarvest)
            {
                return false;
            }

            return !flag || hasTool;
        }
    }
}