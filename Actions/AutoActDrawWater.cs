using System.Collections.Generic;
using UnityEngine;

namespace AutoActMod.Actions;

public class AutoActDrawWater : AutoAct
{
    public static int priority = 120;
    public int detRangeSq = 0;
    public TaskDrawWater Child => child as TaskDrawWater;

    AutoActDrawWater(TaskDrawWater source) : base(source)
    {
        targetName = (Pos.HasBridge ? Pos.matBridge : Pos.matFloor).alias;
    }

    public static AutoActDrawWater TryCreate(AIAct source)
    {
        if (source is not TaskDrawWater a) { return null; }
        return new AutoActDrawWater(a);
    }

    public override bool CanProgress()
    {
        var pot = Child.pot;
        return owner.held == pot.owner && pot.owner.c_charges < pot.MaxCharge;
    }

    public override IEnumerable<Status> Run()
    {
        yield return StartNextTask();
        while (CanProgress())
        {
            var targetPos = FindNextPos(cell =>
            {
                if (!cell.IsTopWaterAndNoSnow)
                {
                    return false;
                }

                var p = cell.GetPoint();
                return (p.HasBridge ? p.matBridge : p.matFloor).alias == targetName && !cell.HasObj && !cell.HasBlock;
            }, detRangeSq);

            if (targetPos.IsNull())
            {
                break;
            }

            Child.pos = targetPos;
            yield return StartNextTask();
        }
        yield return Fail();
    }
}