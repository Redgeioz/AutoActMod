using System.Collections.Generic;
using System.Linq;

namespace AutoActMod.Actions;

public class AutoActPourWater : AutoAct
{
    public int w;
    public int h;
    public SubActPourWater pourWater;
    public SubActPourWater Child => pourWater;

    public AutoActPourWater(SubActPourWater source) : base(source)
    {
        pourWater = source;

        w = Settings.BuildRangeW;
        h = Settings.BuildRangeH;
        if (Settings.StartFromCenter)
        {
            h = 0;
        }
    }

    public static AutoActPourWater TryCreate(AIAct source)
    {
        if (source is not TaskPourWater a) { return null; }
        return new AutoActPourWater(new SubActPourWater(a, Settings.PourDepth));
    }

    public override bool CanProgress()
    {
        return canContinue && owner.held?.trait is TraitToolWaterPot;
    }

    public static bool CanPourWater(Cell cell)
    {
        return !cell.HasBridge && !cell.HasObj && cell.sourceSurface.tag.Contains("soil");
    }

    public override IEnumerable<Status> Run()
    {
        while (CanProgress())
        {
            var pot = owner.held?.trait as TraitToolWaterPot;
            if (pot.IsNull())
            {
                yield return Fail();
            }

            var targetPos = FindPosRefToStartPos(CanPourWater, w, h);
            if (targetPos.IsNull())
            {
                yield break;
            }

            if (pot.owner.c_charges == 0)
            {
                var nextPot = owner.things.Find(t => t.trait is TraitToolWaterPot twp && twp.owner.c_charges > 0);
                if (nextPot.HasValue())
                {
                    pot = nextPot.trait as TraitToolWaterPot;
                    owner.HoldCard(nextPot);
                }
                else
                {
                    yield return Fail();
                }
            }

            Child.pos = targetPos;
            Child.pot = owner.held.trait as TraitToolWaterPot;
            yield return StartNextTask();
        }
        yield break;
    }

    public class SubActPourWater : TaskPourWater
    {
        public int count = 0;
        public int targetCount;

        public SubActPourWater() : base() { }

        public SubActPourWater(TaskPourWater source, int depth) : base()
        {
            pos = source.pos;
            pot = source.pot;
            targetCount = depth;
        }

        public override bool CanProgress()
        {
            return base.CanProgress() && count < targetCount;
        }

        public override void OnCreateProgress(Progress_Custom p)
        {
            base.OnCreateProgress(p);
            var action = p.onProgressComplete;
            p.onProgressComplete = () =>
            {
                action();
                count += 1;
            };
        }

        public override void OnReset()
        {
            count = 0;
        }
    }
}