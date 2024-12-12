using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElinAutoAct.Actions;

public class AutoActWater : AutoAct
{
    public int detRangeSq;
    public bool waterFirst;
    public TaskWater taskWater;
    public TraitToolWaterCan waterCan;
    // public override Point Pos => owner.pos;

    public AutoActWater()
    {
        detRangeSq = Settings.DetRangeSq;
    }

    public static AutoActWater TryCreate(AIAct source)
    {
        if (source is not TaskWater a) { return null; }
        return new AutoActWater { waterFirst = true, taskWater = a };
    }

    public static AutoActWater TryCreate(string id, Point pos)
    {
        if (id != "ActDrawWater") { return null; }
        return new AutoActWater()
        {
            taskWater = new TaskWater()
        };
    }

    public override bool CanProgress()
    {
        return base.CanProgress() && waterCan.HasValue() && owner.held?.trait == waterCan && waterCan.owner.c_charges < waterCan.MaxCharge;
    }

    public override IEnumerable<Status> Run()
    {
        taskWater.dest = owner.pos;
        waterCan = owner.held?.trait as TraitToolWaterCan;
        if (waterCan.IsNull())
        {
            yield return Cancel();
        }

        do
        {
            if (waterCan.owner.c_charges < waterCan.MaxCharge && !waterFirst)
            {
                var targetPos = FindNextPos(c => ActDrawWater.HasWaterSource(c.GetPoint()), detRangeSq);
                if (targetPos.IsNull())
                {
                    yield return Cancel();
                }

                // Avoid use ActDrawAct here because it might create another AutoAct
                yield return Do(new DynamicAIAct(
                    "ActDrawWater_AutoAct",
                    () =>
                    {
                        owner.PlaySound("water_draw", 1f, true);
                        waterCan.owner.SetCharge(this.waterCan.MaxCharge);
                        owner.Say("water_draw", owner, waterCan.owner, null, null);
                        return true;
                    }
                )
                { pos = targetPos });
            }

            waterFirst = false;
            yield return Do(taskWater, KeepRunning);
        } while (CanProgress());
        yield break;
    }
}