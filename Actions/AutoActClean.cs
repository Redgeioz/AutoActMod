using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    public static AutoActClean TryCreate(string id, Point pos)
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

            _map.SetDecal(pos.x, pos.z, 0, 1, true);
            _map.SetLiquid(pos.x, pos.z, 0, 0);
            pos.PlayEffect("vanish");
            owner.Say("clean", held, null, null);
            owner.PlaySound("clean_floor", 1f, true);
            owner.stamina.Mod(-1);
            owner.ModExp(293, 40);
            yield return KeepRunning();
        };

        while (CanProgress())
        {
            foreach (var status in Process())
            {
                yield return status;
            }

            var targetPos = FindNextPos(cell => CanClean(cell), detRangeSq);
            if (targetPos.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            pos = targetPos;
        }
        yield return Fail();
    }
}