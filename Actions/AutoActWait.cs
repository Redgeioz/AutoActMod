using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoActMod.Actions;

public class AutoActWait : AutoAct
{
    public override int MaxRestart => 0;
    public override Point Pos => owner.pos;
    public new Func<bool> canContinue;

    public AutoActWait() { }

    public override void OnStart()
    {
        SetStartPos();
        child?.Reset();
    }

    public override bool CancelWhenDamaged => !pc.party.members.Any(chara => chara.ai.Current is GoalCombat && chara.ai is AutoAct);

    public override bool CanProgress()
    {
        return canContinue?.Invoke() is true;
    }

    public override IEnumerable<Status> Run()
    {
        while (CanProgress())
        {
            yield return KeepRunning();
        }
    }
}