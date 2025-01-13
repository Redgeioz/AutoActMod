using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActWater : AutoAct
{
    public int detRangeSq = Settings.DetRangeSq;
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
        if (source is not TaskWater a) { return null; }
        return new AutoActWater(a.dest) { waterFirst = true };
    }

    public static AutoActWater TryCreate(string id, Card target, Point pos)
    {
        if (id != "ActDrawWater") { return null; }
        return new AutoActWater(pos);
    }

    public bool IsWaterCanValid(bool msg = true)
    {
        return waterCan.HasValue() && owner.held?.trait == waterCan && waterCan.owner.c_charges > 0;
    }

    public override bool CanProgress()
    {
        return base.CanProgress() && waterCan.HasValue() && owner.held?.trait == waterCan;
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
                var targetPos = FindPos(c => ActDrawWater.HasWaterSource(c.GetPoint()), detRangeSq);
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

            var list = new List<Point>();
            _map.ForeachPoint(p =>
            {
                if (CalcDist2(p) <= detRangeSq && TaskWater.ShouldWater(p))
                {
                    list.Add(p.Copy());
                }
            });

            while (list.Count > 0 && IsWaterCanValid())
            {
                var targetPos = FindPosInField(list, cell => TaskWater.ShouldWater(cell.GetPoint()));
                if (targetPos.IsNull())
                {
                    SayNoTarget();
                    yield break;
                }

                list.Remove(targetPos);
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
            yield return DoGoto(dest, 1, true, () =>
            {
                return Status.Running;
            });

            var parent = this.parent as AutoActWater;
            if (!parent.IsWaterCanValid())
            {
                yield return Cancel();
            }

            dest.cell.isWatered = true;
            if (!dest.cell.blocked && rnd(5) == 0)
            {
                _map.SetLiquid(dest.x, dest.z, 1);
            }

            if (dest.cell.HasFire)
            {
                dest.ModFire(-50, true);
            }

            owner.PlaySound("water_farm");
            owner.Say("water_farm", owner, dest.cell.GetFloorName());
            parent.waterCan.owner.ModCharge(-1);
            owner.ModExp(286, 15);
        }
    }
}