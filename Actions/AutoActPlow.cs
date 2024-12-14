using System.Collections.Generic;
using UnityEngine;

namespace AutoActMod.Actions;

public class AutoActPlow : AutoAct
{
    public int w;
    public int h;
    public TaskPlow Child => child as TaskPlow;

    public AutoActPlow(TaskPlow source) : base(source)
    {
        w = Settings.BuildRangeW;
        h = Settings.BuildRangeH;
        if (Settings.StartFromCenter)
        {
            h = 0;
        }
    }

    public static AutoActPlow TryCreate(AIAct source)
    {
        if (source is not TaskPlow a) { return null; }
        return new AutoActPlow(a);
    }

    public override IEnumerable<Status> Run()
    {
        yield return StartNextTask();
        while (CanProgress())
        {
            var targetPos = FindNextPosRefToStartPos(
                cell => !cell.HasBlock
                    && !cell.HasObj
                    && cell.Installed.IsNull()
                    && !cell.IsTopWater
                    && !cell.IsFarmField
                    && (cell.HasBridge ? cell.sourceBridge : cell.sourceFloor).tag.Contains("soil"),
                w,
                h
            );

            if (targetPos.IsNull())
            {
                yield break;
            }

            Child.pos = targetPos;
            yield return StartNextTask();
        }
        yield return Fail();
    }
}