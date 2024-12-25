using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActUnlock : AutoAct
{
    public int detRangeSq;
    public AI_OpenLock openLock;
    public AI_OpenLock Child => openLock;
    public override Point Pos => openLock.target.pos;

    public AutoActUnlock(AIAct source) : base(source)
    {
        openLock = source as AI_OpenLock;
        detRangeSq = Settings.DetRangeSq;
    }

    public static AutoActUnlock TryCreate(AIAct source)
    {
        if (source is not AI_OpenLock a) { return null; }
        return new AutoActUnlock(a);
    }

    public override IEnumerable<Status> Run()
    {
        do
        {
            yield return DoGoto(Pos, 1, true);
            yield return SetNextTask(openLock);

            var target = FindThing(t => t.trait is TraitContainer && t.c_lockLv > 0, detRangeSq);
            if (target.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            Child.target = target;
        } while (CanProgress());
        yield break;
    }
}