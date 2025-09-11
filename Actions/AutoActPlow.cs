using System;
using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActPlow(TaskPlow source) : AutoAct(source)
{
    public int w;
    public int h;
    public TaskPlow Child => child as TaskPlow;
    public HashSet<Point> range;

    public override IEnumerable<Status> Run()
    {
        do
        {
            var targetPos = FindPosRefToStartPos(Filter, range);

            if (targetPos.IsNull())
            {
                yield break;
            }

            Child.pos = targetPos;
            yield return StartNextTask();
        } while (CanProgress());
        yield return FailOrSuccess();
    }

    public override void OnChildSuccess()
    {
        range?.Remove(Pos);
    }

    public bool Filter(Cell cell) => !cell.HasBlock
        && !cell.HasObj
        && cell.Installed?.trait is null or TraitLight
        && !cell.IsTopWater
        && !cell.IsFarmField
        && (cell.HasBridge ? cell.sourceBridge : cell.sourceFloor).tag.Contains("soil");
}