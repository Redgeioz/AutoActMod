using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActUnlock(AIAct source) : AutoAct(source)
{
    public int detRangeSq = Settings.DetRangeSq;
    public AI_OpenLock openLock = source as AI_OpenLock;
    public AI_OpenLock Child => openLock;
    public override Point Pos => openLock.target.pos;

    public static AutoActUnlock TryCreate(AIAct source)
    {
        if (source is not AI_OpenLock a || !NeedUnlock(a.target)) { return null; }
        return new AutoActUnlock(a);
    }

    public static bool NeedUnlock(Thing t) => t.trait is TraitContainer && t.c_lockLv > 0;

    public override IEnumerable<Status> Run()
    {
        do
        {
            var target = FindThing(NeedUnlock, detRangeSq);
            if (target.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            Child.target = target;
            yield return DoGoto(Pos, 1, true);
            yield return SetNextTask(openLock);
        } while (CanProgress());
        yield break;
    }
}