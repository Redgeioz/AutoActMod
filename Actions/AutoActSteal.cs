using System;
using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActSteal : AutoAct
{
    public int detRangeSq;
    public AI_Steal steal;
    public AI_Steal Child => steal;
    public Point pos;
    public override Point Pos => pos;

    public AutoActSteal(AIAct source) : base(source)
    {
        steal = source as AI_Steal;
        steal.target ??= TC;
        pos = Child.target.pos;
        detRangeSq = Settings.DetRangeSq;
        if (Settings.SimpleIdentify > 0 && Child.target is not Chara)
        {
            targetId = -1;
        }
        else
        {
            SetTarget(Child.target);
        }
    }

    public static AutoActSteal TryCreate(AIAct source)
    {
        if (source is not AI_Steal a) { return null; }
        return new AutoActSteal(a);
    }

    public bool CanSteal(Card c)
    {
        if (c.things.FindStealable().IsNull() && c.ChildrenAndSelfWeight > owner.Evalue(281) * 200 + owner.STR * 100 + 1000)
        {
            return false;
        }
        return !_zone.IsUserZone && !(c.isThing & (_zone is Zone_LittleGarden)) && (c.isNPCProperty || !c.isThing) && c.trait.CanBeStolen && c.c_lockLv <= 0 && (c.isThing || !c.IsPCFaction);
    }

    public bool IsTargetChara(Chara chara) => !chara.IsPCFaction && chara.things.FindStealable().HasValue();

    public override IEnumerable<Status> Run()
    {
        var lastTarget = Child.target;
        do
        {
            Card target = null;

            var chara = lastTarget as Chara;
            if (chara.IsNull())
            {
                target = FindThing(t => IsTarget(t) && CanSteal(t), detRangeSq);
            }
            else
            {
                var keepTarget = useOriginalPos;
                target = FindChara(IsTargetChara, detRangeSq);
                if (keepTarget && target.HasValue() && !Child.target.pos.Equals(target.pos))
                {
                    owner.Say("steal_chara_nothing", owner, chara);
                }
            }

            if (target.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            pos = target.pos;
            lastTarget = target;
            Child.target = target;

            yield return DoGoto(Pos, 1, true);
            yield return SetNextTask(steal);
        } while (CanProgress());
        yield break;
    }
}