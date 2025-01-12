using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActTendAnimal(AI_TendAnimal source) : AutoAct(source)
{
    public int detRangeSq = Settings.DetRangeSq;
    public AI_TendAnimal Child => child as AI_TendAnimal;
    public override Point Pos => Child.target?.pos;
    public bool isTargetPCFaction = source.target.IsPCFaction;

    public static AutoActTendAnimal TryCreate(AIAct source)
    {
        if (source is not AI_TendAnimal a) { return null; }
        return new AutoActTendAnimal(a);
    }

    public override bool CanProgress()
    {
        return base.CanProgress() && owner.Tool?.trait is TraitToolBrush && owner.Tool.HasElement(237, 1);
    }

    public override IEnumerable<Status> Run()
    {
        while (CanProgress())
        {
            var target = FindChara(chara => chara.interest > 0 && chara.IsPCFaction == isTargetPCFaction, Settings.DetRangeSq);
            if (target.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            Child.target = target;
            yield return StartNextTask();
        }
        yield return FailOrSuccess();
    }
}