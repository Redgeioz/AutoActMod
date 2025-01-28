using System.Collections.Generic;
using System.Linq;

namespace AutoActMod.Actions;

public class AutoActDig : AutoAct
{
    public int w;
    public int h;
    public TaskDig Child => child as TaskDig;
    public List<Point> range;
    public bool useBuildRange;
    public int detRangeSq;

    public AutoActDig(TaskDig source) : base(source)
    {
        var surface = source.pos.cell.sourceSurface;
        useBuildRange = surface.tag.Contains("grass") || source.pos.HasBridge;
        SetTarget(surface);
        w = Settings.BuildRangeW;
        h = Settings.BuildRangeH;
        detRangeSq = Settings.DetRangeSq;
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
        return base.CanProgress() && owner.held?.HasElement(230, 1) is true && owner.held == Child.tool;
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

            Point targetPos;
            if (useBuildRange || range.HasValue())
            {
                targetPos = FindPosRefToStartPos(
                      Filter,
                      w,
                      h,
                      range
                  );
            }
            else
            {
                targetPos = FindPos(
                    cell => IsTarget(cell.sourceSurface) && Filter(cell),
                    detRangeSq
                );
            }

            if (targetPos.IsNull())
            {
                if (!useBuildRange && range.IsNull())
                {
                    SayNoTarget();
                }
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

    public bool Filter(Cell cell)
    {
        var originalX = Child.pos.x;
        var originalZ = Child.pos.z;
        Child.pos.Set(cell.x, cell.z);
        HitResult hitResult = Child.GetHitResult();
        Child.pos.Set(originalX, originalZ);
        return hitResult == HitResult.Valid || hitResult == HitResult.Warning;
    }
}