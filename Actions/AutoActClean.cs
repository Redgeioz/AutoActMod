using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActClean : AutoAct
{
    public int detRangeSq;
    public SubActClean Child => child as SubActClean;
    public override Point Pos => Child.pos;
    public override bool IsAutoTurn => Child.child is SubActClean act && !(act.child is AI_Goto move && move.IsRunning);
    public override int CurrentProgress => Child.progress;
    public override int MaxProgress => Child.maxProgress;

    public AutoActClean(Point p)
    {
        detRangeSq = Settings.DetRangeSq;
        child = new SubActClean { pos = p };
    }

    public static AutoActClean TryCreate(AIAct source)
    {
        if (source is not TaskClean a) { return null; }
        return new AutoActClean(a.dest);
    }

    public static bool CanClean(Point p)
    {
        return TaskClean.CanClean(p);
    }

    public static bool CanClean(Cell cell)
    {
        return CanClean(cell.GetPoint());
    }

    public override bool CanProgress()
    {
        return base.CanProgress() && owner.held?.trait is TraitBroom;
    }

    public override IEnumerable<Status> Run()
    {
        while (CanProgress())
        {
            var targetPos = FindPos(CanClean, detRangeSq);
            if (targetPos.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            Child.pos = targetPos;
            yield return StartNextTask();
        }
        yield return FailOrSuccess();
    }

    public class SubActClean : AIAct
    {
        public Point pos;
        public int maxProgress;
        public int progress;
        public override IEnumerable<Status> Run()
        {
            yield return DoGoto(pos, 1, true);

            if (owner.held?.trait is not TraitBroom || !CanClean(pos))
            {
                yield return Cancel();
            }

            progress = 0;
            maxProgress = pos.cell.HasLiquid ? 5 : 1;
            while (true)
            {
                progress++;
                owner.LookAt(pos);
                owner.renderer.NextFrame();
                if (progress == maxProgress) { break; }
                yield return KeepRunning();
            }

            _map.SetDecal(pos.x, pos.z, 0, 1, true);
            _map.SetLiquid(pos.x, pos.z, 0, 0);
            pos.PlayEffect("vanish");
            owner.Say("clean", owner, null, null);
            owner.PlaySound("clean_floor", 1f, true);
            owner.stamina.Mod(-1);
            owner.ModExp(293, 30);
            yield return KeepRunning();
        }
    }
}