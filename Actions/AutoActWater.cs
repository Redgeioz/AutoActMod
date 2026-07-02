using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoActMod.Actions;

public class AutoActWater : AutoAct
{
    public bool waterFirst;
    public TraitToolWaterCan waterCan;
    public SubActWater subActWater = new();
    public override Point Pos => subActWater.dest;

    public AutoActWater(Point pos)
    {
        subActWater.dest = pos;
    }

    public static AutoActWater TryCreate(AIAct source)
    {
        if (source is not TaskWater a || a.dest.cell.HasFire) { return null; }
        return new AutoActWater(a.dest) { waterFirst = true };
    }

    public static AutoActWater TryCreate(string lang, Card target, Point pos)
    {
        if (lang != "ActDrawWater".lang()) { return null; }
        return new AutoActWater(pos);
    }

    public bool IsWaterCanValid()
    {
        return waterCan?.Equals(owner.held?.trait) is true && waterCan.owner.c_charges > 0;
    }


    public override bool CanProgress()
    {
        return waterCan?.Equals(owner.held?.trait) is true;
    }

    public override IEnumerable<Status> Run()
    {
        waterCan = owner.held?.trait as TraitToolWaterCan;
        if (waterCan.IsNull())
        {
            yield return Fail();
        }

        do
        {
            if (waterCan.owner.c_charges < waterCan.MaxCharge && !waterFirst)
            {
                var targetPos = FindPos(c => ActDrawWater.HasWaterSource(c.GetPoint()), 80000);
                if (targetPos.IsNull())
                {
                    if (owner.IsPC && waterCan.owner.c_charges == 0)
                    {
                        Msg.Say("water_deplete");
                    }
                    yield return Fail();
                }

                // Avoid using ActDrawAct here because it might create another AutoAct
                // (No way to check if it is performed by an AutoAct instance)
                yield return Do(new DynamicAIAct(
                    "SubActDrawWater_AutoAct",
                    () =>
                    {
                        owner.PlaySound("water_draw", 1f, true);
                        waterCan.owner.SetCharge(waterCan.MaxCharge);
                        owner.Say("water_draw", owner, waterCan.owner, null, null);
                        return true;
                    }
                )
                { pos = targetPos });
            }

            waterFirst = false;

            // Why not use TaskWater?
            // Because TaskWater sorts targets by distance, while AutoAct usually sorts targets by path length
            var range = new HashSet<Point>();
            _map.ForeachPoint(p =>
            {
                if (TaskWater.ShouldWater(p))
                {
                    range.Add(p.Copy());
                }
            });

            if (range.Count == 0)
            {
                SayNoTarget();
                yield break;
            }

            while (range.Count > 0 && IsWaterCanValid())
            {
                var targetPos = FindPos(cell => TaskWater.ShouldWater(cell.GetPoint()), range: range);
                if (targetPos.IsNull())
                {
                    SayNoTarget();
                    yield break;
                }

                range.Remove(targetPos);
                subActWater.dest = targetPos;
                yield return SetNextTask(subActWater, KeepRunning);
            }
        } while (CanProgress());
        yield return FailOrSuccess();
    }

    public class SubActWater : AIAct
    {
        public Point dest;

        public override IEnumerable<Status> Run()
        {
            yield return DoGoto(dest, 1, true);

            var parent = this.parent as AutoActWater;
            if (!parent.IsWaterCanValid())
            {
                yield return Cancel();
            }

            var num = parent.waterCan.owner.Evalue(770);
            num = (num <= 0) ? 1 : Mathf.Min(parent.waterCan.owner.c_charges, 2 + num / 10);
            if (num > 1)
            {
                var list2 = ListPointsInSquare(dest, num - 1, false, false);
                list2.Sort((a, b) => a.Distance(dest) - b.Distance(dest));
                foreach (var item in list2)
                {
                    Water(item);
                }
            }
            else
            {
                Water(dest);
            }

            owner.PlaySound("water_farm");
            owner.Say("water_farm", owner, dest.cell.GetFloorName());
            parent.waterCan.owner.ModCharge(-num);
        }

        public void Water(Point point)
        {
            point.cell.isWatered = true;
            if (!point.cell.blocked && rnd(5) == 0)
            {
                _map.SetLiquid(point.x, point.z, 1);
            }
            if (point.cell.HasFire)
            {
                point.ModFire(-50, true);
            }
            owner.ModExp(286, 15);
        }

        public List<Point> ListPointsInSquare(Point center, int radius, bool mustBeWalkable = true, bool los = true)
        {
            var list = new List<Point>();
            ForeachSquare(center.x, center.z, radius, p =>
            {
                if ((!mustBeWalkable || !p.cell.blocked) && (!los || Los.IsVisible(center, p)))
                {
                    list.Add(p.Copy());
                }
            });
            return list;
        }

        public void ForeachSquare(int _x, int _z, int r, Action<Point> action)
        {
            var point = new Point();
            for (int i = _x - r; i < _x + r + 1; i++)
            {
                if (i < 0 || i >= _map.Size)
                {
                    continue;
                }
                for (int j = _z - r; j < _z + r + 1; j++)
                {
                    if (j >= 0 && j < _map.Size)
                    {
                        point.Set(i, j);
                        action(point);
                    }
                }
            }
        }
    }
}