using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoActMod.Actions;

public class AutoActWait(AIAct source) : AutoAct(source)
{
    public override int MaxRestart => 0;
    public override Point Pos => owner.pos;
    public new Func<bool> canContinue;

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

    public new bool CancelWhenDamaged => EClass.pc?.party?.members?.Any(chara => chara.ai.Current is GoalCombat && chara.ai is AutoAct) ?? false;

    public override bool CanProgress()
    {
        if (!base.CanProgress())
        {
            return false;
        }

        if (CancelWhenDamaged)
        {
            return false;
        }

        return canContinue == null || canContinue();
    }

    public override IEnumerable<Status> Run()
    {
        while (CanProgress())
        {
            yield return KeepRunning();
        }
    }
}