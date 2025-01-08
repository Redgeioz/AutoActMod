using System;
using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActWait : AutoAct
{
    public override int MaxRestart => 0;
    public override Point Pos => owner.pos;
    public new Func<bool> canContinue;

    public AutoActWait() { }

    // public static AutoActWait TryCreate(AIAct source)
    // {
    //     if (source is not GoalEndTurn) { return null; }
    //     return new AutoActWait();
    // }

    public override void OnStart()
    {
        SetStartPos();
        child?.Reset();
    }

    public override bool CanProgress()
    {
        return canContinue.IsNull() || canContinue();
    }

    public override IEnumerable<Status> Run()
    {
        while (CanProgress())
        {
            yield return KeepRunning();
        }
    }
}