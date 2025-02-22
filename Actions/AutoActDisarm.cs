using System;
using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActDisarm(TraitTrap target) : AutoAct
{
    public TraitTrap target = target;
    public override int MaxRestart => 0;
    public override Point Pos => owner.pos;

    public static AutoActDisarm TryCreate(string lang, Card target, Point pos)
    {
        if (lang != "actDisarm".lang()) { return null; }
        var trap = target?.trait as TraitTrap ?? pos.FindThing(t => t.trait is TraitTrap && !t.isHidden)?.trait as TraitTrap;
        if (trap.IsNull()) { return null; }
        return new AutoActDisarm(trap);
    }

    public override bool CanProgress() => canContinue;

    public override IEnumerable<Status> Run()
    {
        while (CanProgress())
        {
            if (target.TryDisarmTrap(pc))
            {
                yield return Success();
            }

            if (pc.Evalue(1656) < 3 && rnd(2) == 0)
            {
                target.ActivateTrap(pc);
            }

            yield return KeepRunning();
        }
    }
}