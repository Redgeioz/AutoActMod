using System;
using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActPlow : AutoAct
{
    public int w;
    public int h;
    public TaskPlow Child => child as TaskPlow;
    public List<Point> range;

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
        do
        {
            var targetPos = FindPosRefToStartPos(
                Filter,
                w,
                h,
                range
            );

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