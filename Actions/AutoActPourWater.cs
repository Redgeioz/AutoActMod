using System.Collections.Generic;
using UnityEngine;

namespace ElinAutoAct.Actions;

public class AutoActPourWater : AutoAct
{
    public int w;
    public int h;
    public TaskPourWaterCustom Child => child as TaskPourWaterCustom;

    AutoActPourWater(TaskPourWaterCustom source) : base(source)
    {
        SetTarget(Cell.sourceSurface);
        w = Settings.BuildRangeW;
        h = Settings.BuildRangeH;
        if (Settings.StartFromCenter)
        {
            h = 0;
        }
    }

    public static AutoActPourWater TryCreate(AIAct source)
    {
        if (source is not TaskPourWater a) { return null; }
        return new AutoActPourWater(new TaskPourWaterCustom(a, Settings.PourDepth));
    }

    public override bool CanProgress()
    {
        if (owner.held?.trait is not TraitToolWaterPot pot)
        {
            return false;
        }

        if (pot.owner.c_charges == 0)
        {
            var nextPot = owner.things.Find(t => t.trait is TraitToolWaterPot twp && twp.owner.c_charges > 0);
            if (nextPot.HasValue())
            {
                pot = nextPot.trait as TraitToolWaterPot;
                owner.HoldCard(nextPot);
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    public override IEnumerable<Status> Run()
    {
        yield return StartNextTask();
        while (CanProgress())
        {
            var targetPos = FindNextTargetRefToStartPos(cell => !cell.HasBridge && IsTarget(cell.sourceFloor), w, h);
            if (targetPos.IsNull())
            {
                End();
                break;
            }

            Child.pos = targetPos;
            Child.pot = owner.held.trait as TraitToolWaterPot;
            yield return StartNextTask();
        }
        yield break;
    }
}

public class TaskPourWaterCustom : TaskPourWater
{
    public int count = 0;
    public int targetCount;

    public TaskPourWaterCustom(TaskPourWater source, int depth) : base()
    {
        source.Cancel();
        pos = source.pos;
        pot = source.pot;
        targetCount = depth;
    }

    public override bool CanProgress()
    {
        return base.CanProgress() && count < targetCount;
    }

    public override void OnCreateProgress(Progress_Custom p)
    {
        base.OnCreateProgress(p);
        var action = p.onProgressComplete;
        p.onProgressComplete = () =>
        {
            action();
            count += 1;
        };
    }

    public override void OnReset()
    {
        count = 0;
    }
}