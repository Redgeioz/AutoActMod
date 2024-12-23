using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoActMod.Actions;

public class AutoActWater : AutoAct
{
    public int detRangeSq;
    public bool waterFirst;
    public Point pos;
    public TraitToolWaterCan waterCan;
    public override Point Pos => pos;

    public AutoActWater()
    {
        detRangeSq = Settings.DetRangeSq;
    }

    public static AutoActWater TryCreate(AIAct source)
    {
        if (source is not TaskWater a) { return null; }
        return new AutoActWater { waterFirst = true };
    }

    public static AutoActWater TryCreate(string id, Point pos)
    {
        if (id != "ActDrawWater") { return null; }
        return new AutoActWater();
    }

    public bool IsWaterCanValid(bool msg = true)
    {
        bool num = waterCan.HasValue() && owner.held?.trait == waterCan && waterCan.owner.c_charges > 0;
        if (!num && msg)
        {
            Msg.Say("water_deplete");
        }

        return num;
    }

    public override bool CanProgress()
    {
        return base.CanProgress() && waterCan.HasValue() && owner.held?.trait == waterCan && waterCan.owner.c_charges < waterCan.MaxCharge;
    }

    public override IEnumerable<Status> Run()
    {
        pos = owner.pos;
        waterCan = owner.held?.trait as TraitToolWaterCan;
        if (waterCan.IsNull())
        {
            yield return Fail();
        }

        do
        {
            if (waterCan.owner.c_charges < waterCan.MaxCharge && !waterFirst)
            {
                var targetPos = FindNextPos(c => ActDrawWater.HasWaterSource(c.GetPoint()), detRangeSq);
                if (targetPos.IsNull())
                {
                    yield return Fail();
                }

                // Avoid using ActDrawAct here because it might create another AutoAct
                yield return Do(new DynamicAIAct(
                    "ActDrawWater_AutoAct",
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
            while (list.Count > 0)
            {
                var targetPos2 = FindNextPosInField(list, _ => true);
                if (targetPos2.IsNull())
                {
                    SayNoTarget();
                    yield break;
                }

                list.Remove(targetPos2);
                yield return DoGoto(targetPos2, 1, true, () =>
                {
                    return Status.Running;
                });

                if (!IsWaterCanValid())
                {
                    yield return KeepRunning();
                    break;
                }

                targetPos2.cell.isWatered = true;
                if (!targetPos2.cell.blocked && rnd(5) == 0)
                {
                    _map.SetLiquid(targetPos2.x, targetPos2.z, 1);
                }

                if (targetPos2.cell.HasFire)
                {
                    _map.ModFire(targetPos2.x, targetPos2.z, -50);
                }

                owner.PlaySound("water_farm");
                owner.Say("water_farm", owner, targetPos2.cell.GetFloorName());
                waterCan.owner.ModCharge(-1);
                owner.ModExp(286, 15);
                yield return KeepRunning();
                if (!IsWaterCanValid())
                {
                    break;
                }
            }
        } while (CanProgress());
        yield return Fail();
    }
}