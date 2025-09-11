using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActBrush(AI_TendAnimal source) : AutoAct(source)
{
    public int detRangeSq = Settings.DetRangeSq;
    public AI_TendAnimal Child => child as AI_TendAnimal;
    public override Point Pos => Child.target?.pos;
    public bool isTargetPCFaction = source.target?.IsPCFaction is true;
    public override bool IsAutoTurn => Child is AI_TendAnimal act && !(act.child is AI_Goto move && move.IsRunning);
    public override int CurrentProgress => Child.progress;
    public override int MaxProgress => Child.maxProgress;
    public List<Chara> range;

    public static AutoActBrush TryCreate(AIAct source)
    {
        if (source is not AI_TendAnimal a) { return null; }
        return new AutoActBrush(a);
    }

    public override bool CanProgress()
    {
        return base.CanProgress() && owner.Tool?.trait is TraitToolBrush && owner.Tool.HasElement(237, 1);
    }

    public static bool CanBeBrushed(Chara chara) => chara.interest > 0;

    public override IEnumerable<Status> Run()
    {
        while (CanProgress())
        {
            var target = FindChara(chara => CanBeBrushed(chara) && (range.HasValue() || chara.IsPCFaction == isTargetPCFaction), Settings.DetRangeSq, range);
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

    public override void OnChildSuccess()
    {
        if (CanBeBrushed(Child.target))
        {
            return;
        }

        range?.Remove(Child.target);
    }
}