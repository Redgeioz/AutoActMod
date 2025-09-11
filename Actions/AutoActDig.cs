using System.Collections.Generic;
using System.Linq;

namespace AutoActMod.Actions;

public class AutoActDig : AutoAct
{
    public int w;
    public int h;
    public TaskDig Child => child as TaskDig;
    public HashSet<Point> range;
    public int detRangeSq;

    public AutoActDig(TaskDig source) : base(source)
    {
        var surface = source.pos.cell.sourceSurface;
        SetTarget(surface);
        detRangeSq = Settings.DetRangeSq;
    }

    public static AutoActDig TryCreate(AIAct source)
    {
        if (source is not TaskDig a) { return null; }
        var surface = a.pos.cell.sourceSurface;
        var needBuildRange = !_zone.IsRegion && (surface.tag.Contains("grass") || a.pos.HasBridge);
        if (needBuildRange) { return null; }
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
            if (range.HasValue())
            {
                targetPos = FindPosRefToStartPos(Filter, range);
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
                if (range.IsNull())
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

        if (Settings.SimpleIdentify == 2)
        {
            var hitResult = Child.GetHitResult();
            if (hitResult == HitResult.Valid || hitResult == HitResult.Warning)
            {
                return;
            }
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