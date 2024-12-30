using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActSteal : AutoAct
{
    public int detRangeSq;
    public AI_Steal steal;
    public AI_Steal Child => steal;
    public override Point Pos => steal.target.pos;

    public AutoActSteal(AIAct source) : base(source)
    {
        steal = source as AI_Steal;
        steal.target ??= TC;
        detRangeSq = Settings.DetRangeSq;
        if (Settings.SimpleIdentify && Child.target is not Chara)
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
        if (c.ChildrenAndSelfWeight > owner.Evalue(281) * 200 + owner.STR * 100 + 1000)
        {
            return false;
        }
        return !_zone.IsUserZone && !(c.isThing & (_zone is Zone_LittleGarden)) && (c.isNPCProperty || !c.isThing) && c.trait.CanBeStolen && c.c_lockLv <= 0 && (c.isThing || !c.IsPCFaction);
    }

    public override IEnumerable<Status> Run()
    {
        do
        {
            Card target = null;
            var chara = Child.target as Chara;
            if (chara.IsNull())
            {
                target = FindThing(t => IsTarget(t) && CanSteal(t), detRangeSq);
            }
            else if (chara.things.FindStealable().HasValue())
            {
                target = chara;
            }
            else
            {
                target = FindChara(chara => !chara.IsPCFaction && chara.things.FindStealable().HasValue(), detRangeSq);
            }

            if (target.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            Child.target = target;

            yield return DoGoto(Pos, 1, true);
            yield return SetNextTask(steal);
        } while (CanProgress());
        yield break;
    }
}