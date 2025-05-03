using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActSlaughter(AIAct source) : AutoAct(source)
{
    public int detRangeSq = Settings.DetRangeSq;
    public AI_Slaughter Child => child as AI_Slaughter;
    public override Point Pos => Child.target?.pos;
    public List<Chara> range;

    public static AutoActSlaughter TryCreate(AIAct source)
    {
        if (source is not AI_Slaughter a) { return null; }
        return new AutoActSlaughter(a);
    }

    public static AutoActSlaughter TryCreate(string lang, Card target, Point pos)
    {
        if (lang != "AI_Slaughter".lang()) { return null; }
        var source = new AI_Slaughter { target = target };
        return new AutoActSlaughter(source);
    }

    public override bool CanProgress()
    {
        return canContinue && owner.Tool?.trait is TraitToolButcher;
    }

    public static bool CanBeSlaughtered(Chara chara) => chara.IsPCFaction && chara.memberType == FactionMemberType.Livestock;

    public override IEnumerable<Status> Run()
    {
        while (CanProgress())
        {
            var target = FindChara(CanBeSlaughtered, detRangeSq, range);
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
        range?.Remove(Child.target as Chara);
    }

    public override void OnCancelOrSuccess()
    {
        base.OnCancelOrSuccess();
        Child.OnCancelOrSuccess();
    }
}