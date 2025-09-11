using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActSmash : AutoAct
{
    public int detRangeSq;
    public SubActSmash Child => child as SubActSmash;
    public override Point Pos => Child.pos;

    public AutoActSmash(SubActSmash source) : base(source)
    {
        detRangeSq = Settings.DetRangeSq;
        SetTarget(source.refThing);
    }

    public static AutoActSmash TryCreate(string lang, Card target, Point pos)
    {
        if (pc.ai is AutoActSmash) { return null; }
        if (lang != GetActMeleeLang()) { return null; }
        if (!CanSmash(target)) { return null; }
        return new AutoActSmash(new SubActSmash(pos, target as Thing));
    }

    public static bool CanSmash(Card t)
    {
        return t is Thing && t.trait.CanBeAttacked && t.trait.CanBeSmashedToDeath && t.trait is not TraitTrainingDummy;
    }

    public static string GetActMeleeLang()
    {
        return ACT.Melee.source.GetText();
    }

    public override IEnumerable<Status> Run()
    {
        while (CanProgress())
        {
            var targetThing = FindThing(CanSmash, detRangeSq);
            if (targetThing.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            Child.pos = targetThing.pos;
            Child.refThing = targetThing;
            yield return StartNextTask();
        }
        yield return FailOrSuccess();
    }

    public class SubActSmash : AIAct
    {
        public Thing refThing;
        public Point pos;

        public SubActSmash() { }
        public SubActSmash(Point pos, Thing refThing)
        {
            this.pos = pos;
            this.refThing = refThing;
        }

        public override IEnumerable<Status> Run()
        {
            yield return DoGoto(pos, 1, true);

            if (!CanSmash(refThing) || !ACT.Melee.Perform(owner, refThing))
            {
                yield return Cancel();
            }

            yield return Success();
        }
    }
}