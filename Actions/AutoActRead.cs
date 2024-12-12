using System.Collections.Generic;
using UnityEngine;

namespace ElinAutoAct.Actions;

public class AutoActRead : AutoAct
{
    public AI_Read Child => child as AI_Read;

    public AutoActRead(AIAct source) : base(source) { }

    public static AutoActRead TryCreate(AIAct source)
    {
        if (source is not AI_Read a) { return null; }
        return new AutoActRead(a);
    }

    public override bool CanProgress()
    {
        return !Child.target.isDestroyed;
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