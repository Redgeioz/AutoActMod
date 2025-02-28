using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActDrawWater : AutoAct
{
    public static int priority = 120;
    public int detRangeSq = 0;
    public int simpleIdentify = 0;
    public List<Point> range;
    public TaskDrawWater Child => child as TaskDrawWater;

    public AutoActDrawWater(TaskDrawWater source) : base(source)
    {
        targetName = (Pos.HasBridge ? Pos.matBridge : Pos.matFloor).alias;
        detRangeSq = Settings.DetRangeSq;
        simpleIdentify = Settings.SimpleIdentify;
    }

    public static AutoActDrawWater TryCreate(AIAct source)
    {
        if (source is not TaskDrawWater a) { return null; }
        return new AutoActDrawWater(a);
    }

    public override bool CanProgress()
    {
        var pot = Child.pot;
        return canContinue && owner.held == pot.owner && pot.owner.c_charges < pot.MaxCharge;
    }

    public override IEnumerable<Status> Run()
    {
        do
        {
            var targetPos = FindPos(
               Settings.SimpleIdentify > 0 ? CanDrawWaterSimple : CanDrawWater,
                detRangeSq,
                range: range
            );

            if (targetPos.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            Child.pos = targetPos;
            yield return StartNextTask();
        } while (CanProgress());
        yield return FailOrSuccess();
    }

    public bool CanDrawWater(Cell cell) => cell.IsTopWaterAndNoSnow
        && (cell.HasBridge ? cell.matBridge : cell.matFloor).alias == targetName
        && !cell.HasObj
        && !cell.HasBlock;

    public static bool CanDrawWaterSimple(Cell cell) => cell.IsTopWaterAndNoSnow
        && !cell.HasObj
        && !cell.HasBlock;
}