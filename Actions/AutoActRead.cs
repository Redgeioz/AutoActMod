using System.Collections.Generic;
using UnityEngine;

namespace AutoActMod.Actions;

public class AutoActRead : AutoAct
{
    public AI_Read Child => child as AI_Read;

    public AutoActRead(AIAct source) : base(source) { }

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
        do
        {
            yield return StartNextTask();
        } while (CanProgress());
        yield break;
    }
}