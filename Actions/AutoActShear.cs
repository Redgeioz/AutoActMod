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
            var list = new List<(Chara, int)>();
            _map.charas.ForEach(chara =>
            {
                if (!chara.CanBeSheared())
                {
                    return;
                }

                var dist2 = CalcDist2(chara.pos);
                if (dist2 <= 2)
                {
                    selector.TrySet(chara, dist2 == 0 ? -1 : 0);
                    return;
                }

                list.Add((chara, dist2));
            });

            foreach (var (chara, dist2) in list.OrderBy(Tuple => Tuple.Item2))
            {
                if (selector.curtPoint.HasValue() && dist2 > selector.MaxDist2)
                {
                    break;
                }

                Path.RequestPathImmediate(pc.pos, chara.pos, 1, true, -1);
                if (Path.state == PathProgress.State.Fail)
                {
                    continue;
                }

                selector.TrySet(chara, Path.nodes.Count);
            }

            var target = selector.FinalTarget as Chara;
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