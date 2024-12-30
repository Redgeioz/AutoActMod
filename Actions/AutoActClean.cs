using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActClean : AutoAct
{
    public int detRangeSq;
    public Point pos;
    public override Point Pos => pos;

    public AutoActClean()
    {
        detRangeSq = Settings.DetRangeSq;
    }

    public static AutoActClean TryCreate(string id, Card target, Point pos)
    {
        if (id != "actClean") { return null; }
        return new AutoActClean { pos = pos };
    }

    public static bool CanClean(Point p)
    {
        return CanClean(p.cell);
    }

    public static bool CanClean(Cell cell)
    {
        return !cell.HasBlock && (cell.decal > 0 || (cell.effect.HasValue() && cell.effect.IsLiquid));
    }

    public override bool CanProgress()
    {
        return base.CanProgress() && owner.held?.trait is TraitBroom;
    }

    public override IEnumerable<Status> Run()
    {
        IEnumerable<Status> Process()
        {
            yield return DoGoto(pos, 1, true);

            var held = owner.held;
            if (held?.trait is not TraitBroom || !CanClean(pos))
            {
                yield return Fail();
            }

            var dur = pos.cell.HasLiquid ? 5 : 1;
            for (int i = 0; i < dur; i++)
            {
                owner.LookAt(pos);
                owner.renderer.NextFrame();
                yield return KeepRunning();
            }

            _map.SetDecal(pos.x, pos.z, 0, 1, true);
            _map.SetLiquid(pos.x, pos.z, 0, 0);
            pos.PlayEffect("vanish");
            owner.Say("clean", held, null, null);
            owner.PlaySound("clean_floor", 1f, true);
            owner.stamina.Mod(-1);
            owner.ModExp(293, 30);
            yield return KeepRunning();
        };

        while (CanProgress())
        {
            foreach (var status in Process())
            {
                yield return status;
            }

            var targetPos = FindPos(cell => CanClean(cell), detRangeSq);
            if (targetPos.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            pos = targetPos;
        }
        yield return FailOrSuccess();
    }
}