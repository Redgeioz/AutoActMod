using System.Collections.Generic;
using UnityEngine;

namespace ElinAutoAct.Actions;

public class AutoActDig : AutoAct
{
    public int w;
    public int h;
    public TaskDig Child => child as TaskDig;

    public AutoActDig(TaskDig source) : base(source)
    {
        SetTarget(Cell.sourceSurface);
        w = Settings.BuildRangeW;
        h = Settings.BuildRangeH;
        if (Settings.StartFromCenter)
        {
            h = 0;
        }
    }

    public static AutoActDig TryCreate(AIAct source)
    {
        if (source is not TaskDig a) { return null; }
        return new AutoActDig(a);
    }

    public override IEnumerable<Status> Run()
    {
        yield return StartNextTask();
        while (CanProgress())
        {
            if (_zone.IsRegion)
            {
                yield return StartNextTask();
                continue;
            }

            var targetPos = FindNextTargetRefToStartPos(
                cell => IsTarget(cell.sourceSurface) && !cell.HasBlock && !cell.HasObj,
                w,
                h
            );

            if (targetPos.IsNull())
            {
                End();
                yield break;
            }

            Child.pos = targetPos;
            yield return StartNextTask();
        }
        yield break;
    }
}