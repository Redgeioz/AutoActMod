using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElinAutoAct.Actions;

public class AutoActClean : AutoAct
{
    public int detRangeSq;
    public TaskClean Child => child as TaskClean;

    public AutoActClean(TaskClean source) : base(source)
    {
        detRangeSq = Settings.DetRangeSq;
    }

    public static AutoActClean TryCreate(string id, Point pos)
    {
        if (id != "actClean") { return null; }
        return new AutoActClean(new TaskClean { pos = pos });
    }

    public override IEnumerable<Status> Run()
    {
        if (Child.broom.IsNull())
        {
            Child.broom = owner.held?.trait as TraitBroom;
        }
        yield return StartNextTask();
        while (CanProgress())
        {
            var targetPos = FindNextTarget(cell => !cell.HasBlock && (cell.decal > 0 || (cell.effect.HasValue() && cell.effect.IsLiquid)), detRangeSq);
            if (targetPos.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            Child.pos = targetPos;
            yield return StartNextTask();
        }
        yield break;
    }
}