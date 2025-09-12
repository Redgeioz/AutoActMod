using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActThrowMilk(Chara target) : AutoAct
{
    public int detRangeSq = Settings.DetRangeSq;
    public Chara target = target;
    public override Point Pos => target.pos;

    public static AutoActThrowMilk TryCreate(string lang, Card target, Point pos)
    {
        if ((lang != "ActThrow".lang() && lang != "actMilk".lang())
            || pc.held.trait is not TraitDrinkMilkMother
            || pos.FindChara(NeedMilk) is not Chara t)
        { return null; }
        return new AutoActThrowMilk(t);
    }

    public override bool CanProgress() => owner.held?.trait is TraitDrinkMilkMother;

    public static bool NeedMilk(Chara chara) => chara.Evalue(1232) > 0 && chara.IsPCFaction;

    public override IEnumerable<Status> Run()
    {
        while (true)
        {
            ActThrow.Throw(owner, this.target.pos, this.target, owner.held.Split(1));
            yield return KeepRunning();

            if (!CanProgress() && !TrySwitchToMilk())
            {
                yield break;
            }

            var target = FindChara(NeedMilk);
            if (target.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            this.target = target;
            yield return DoGoto(Pos, 6, true);
        }
    }

    public bool TrySwitchToMilk()
    {
        Thing item = null;
        foreach (var thing in pc.things.Flatten())
        {
            if (thing.trait is not TraitDrinkMilkMother)
            {
                continue;
            }

            if (item.IsNull() || thing.encLV > item.encLV)
            {
                item = thing;
            }
        }

        if (item.HasValue())
        {
            pc.HoldCard(item);
        }

        return item.HasValue();
    }
}