using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActRead(AIAct source) : AutoAct(source)
{
    public AI_Read Child => child as AI_Read;
    public override Point Pos => owner.pos;

    public static AutoActRead TryCreate(AIAct source)
    {
        if (source is not AI_Read a || a.target.trait is not TraitBaseSpellbook) { return null; }
        return new AutoActRead(a);
    }

    public override bool CanProgress()
    {
        return !Child.target.isDestroyed && (Child.target.trait is not TraitAncientbook || !Child.target.isOn);
    }

    public override IEnumerable<Status> Run()
    {
        while (true)
        {
            yield return StartNextTask();

            if (CanProgress())
            {
                continue;
            }

            if (Settings.SimpleIdentify == 0)
            {
                break;
            }

            var next = owner.things.Find(t => t.trait is TraitBaseSpellbook && (t.trait is not TraitAncientbook || !t.isOn));
            if (next.HasValue())
            {
                Child.target = next;
                continue;
            }

            break;
        }
        yield break;
    }
}