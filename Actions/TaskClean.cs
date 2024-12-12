using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElinAutoAct.Actions;

public class TaskClean : TaskPoint
{
    public TraitBroom broom;

    public override IEnumerable<Status> Run()
    {
        yield return DoGoto(pos, 1, true, null);
        _map.SetDecal(pos.x, pos.z, 0, 1, true);
        _map.SetLiquid(pos.x, pos.z, 0, 0);
        pos.PlayEffect("vanish");
        owner.Say("clean", broom.owner, null, null);
        owner.PlaySound("clean_floor", 1f, true);
        owner.stamina.Mod(-1);
        owner.ModExp(293, 40);
        yield break;
    }
}