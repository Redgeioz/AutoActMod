using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElinAutoAct.Actions;

public class AutoActPick : AutoAct
{
    public int detRangeSq;
    public TaskPick Child => child as TaskPick;

    public AutoActPick(TaskPick source) : base(source)
    {
        detRangeSq = Settings.DetRangeSq;
        SetTarget(source.refThing);
    }

    public static AutoActPick TryCreate(string id, Point pos)
    {
        if (id == "actPickOne")
        {
            List<Thing> list = pos.Things;
            Thing refThing = list.FindLast(t => t.placeState == PlaceState.roaming);
            return new AutoActPick(new TaskPick(pos, refThing, refThing.IsInstalled));
        }
        else if (id == "actHold")
        {
            List<Thing> list = pos.Things;
            Thing refThing = list.LastOrDefault();
            return new AutoActPick(new TaskPick(pos, refThing, refThing.IsInstalled));
        }
        return null;
    }

    public override IEnumerable<Status> Run()
    {
        yield return StartNextTask();
        while (CanProgress())
        {
            var targetThing = FindNextThingTarget(detRangeSq);
            if (targetThing.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            Child.pos = targetThing.pos;
            Child.refThing = targetThing;
            yield return StartNextTask();
        }
        yield break;
    }
}