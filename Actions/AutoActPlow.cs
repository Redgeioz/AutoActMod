using System;
using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActPlow(TaskPlow source) : AutoAct(source)
{
    public int w;
    public int h;
    public TaskPlow Child => child as TaskPlow;
    public HashSet<Point> range;

    public static AutoActPlow TryCreate(AIAct source)
    {
        if (source is not TaskPlow a || !Filter(a.pos.cell)) { return null; }
        return new AutoActPlow(a)
        {
            range = InitRange(a.pos, p => Filter(p.cell))
        };
    }

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

    public static bool Filter(Cell cell) => !cell.HasBlock
        && !cell.HasObj
        && cell.Installed?.trait is null or TraitLight
        && !cell.IsTopWater
        && !cell.IsFarmField
        && (cell.HasBridge ? cell.sourceBridge : cell.sourceFloor).tag.Contains("soil");
}