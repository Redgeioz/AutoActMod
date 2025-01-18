using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActDig : AutoAct
{
    public int w;
    public int h;
    public TaskDig Child => child as TaskDig;
    public List<Point> range;

    public AutoActDig(TaskDig source) : base(source)
    {
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

    public override bool CanProgress()
    {
        return base.CanProgress() && owner.held.HasValue() && owner.held.HasElement(230, 1) && owner.held == Child.tool;
    }

    public override IEnumerable<Status> Run()
    {
        do
        {
            if (_zone.IsRegion)
            {
                yield return StartNextTask();
                continue;
            }

            var originalPos = Pos.Copy();
            var targetPos = FindPosRefToStartPos(
                cell =>
                {
                    Child.pos.Set(cell.x, cell.z);
                    HitResult hitResult = Child.GetHitResult();
                    Child.pos.Set(originalPos.x, originalPos.z);
                    return hitResult == HitResult.Valid || hitResult == HitResult.Warning;
                },
                w,
                h
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
        if (range.IsNull())
        {
            return;
        }

        var hitResult = Child.GetHitResult();
        if (hitResult == HitResult.Valid || hitResult == HitResult.Warning)
        {
            return;
        }

        range.Remove(Pos);
    }
}