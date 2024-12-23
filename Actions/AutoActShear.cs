using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoActMod.Actions;

public class AutoActShear : AutoAct
{
    public AI_Shear Child => child as AI_Shear;

    public AutoActShear(AIAct source) : base(source) { }

    public static AutoActShear TryCreate(AIAct source)
    {
        if (source is not AI_Shear a) { return null; }
        return new AutoActShear(a);
    }

    public override bool CanProgress()
    {
        return owner.Tool?.trait is TraitToolShears;
    }

    public override IEnumerable<Status> Run()
    {
        yield return StartNextTask();
        while (CanProgress())
        {
            var target = FindChara(chara => chara.CanBeSheared(), 80000);
            if (target.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            Child.target = target;
            yield return StartNextTask();
        }
        yield return Fail();
    }
}