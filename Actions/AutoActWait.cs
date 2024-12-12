using System.Collections.Generic;
using UnityEngine;

namespace AutoActMod.Actions;

public class AutoActWait : AutoAct
{
    public override int MaxRestart => 0;

    public AutoActWait() { }

    public static AutoActWait TryCreate(AIAct source)
    {
        if (source is not AI_Wait a) { return null; }
        return new AutoActWait();
    }

    public override bool CanProgress()
    {
        return true;
    }

    public override IEnumerable<Status> Run()
    {
        while (true)
        {
            yield return KeepRunning();
        }
    }
}