using System;
using System.Collections.Generic;

namespace AutoAct
{
    public class AI_Pick : TaskPoint
    {
        public Thing refThing;

        public bool installed;

        public override IEnumerable<AIAct.Status> Run()
        {
            yield return DoGoto(pos, 0, false, null);
            if (installed)
            {
                Thing t = pos.Installed;
                if (t != null && refThing.CanStackTo(t))
                {
                    if (!pc.CanLift(t))
                    {
                        pc.Say("tooHeavy", t, null, null);
                    }
                    if (t.HasEditorTag(EditorTag.TreasureMelilith))
                    {
                        if (player.flags.pickedMelilithTreasure)
                        {
                            pc.PlaySound("curse3", 1f, true);
                            pc.PlayEffect("curse", true, 0f);
                            pc.SetFeat(1206, 1, true);
                            player.flags.gotMelilithCurse = true;
                        }
                        else
                        {
                            Msg.Say("pickedMelilithTreasure");
                            player.flags.pickedMelilithTreasure = true;
                            QuestCursedManor questCursedManor = game.quests.Get<QuestCursedManor>();
                            questCursedManor?.NextPhase();
                        }
                        t.c_editorTags = null;
                    }
                    pc.HoldCard(t, -1);
                    if (pc.held != null)
                    {
                        t.PlaySoundHold(false);
                        player.RefreshCurrentHotItem();
                        ActionMode.Adv.planRight.Update(ActionMode.Adv.mouseTarget);
                        pc.renderer.Refresh();
                    }
                }
            }
            else if (pos.HasThing)
            {
                foreach (Card t in pos.ListCards())
                {
                    if (t is Thing && t.CanStackTo(refThing))
                    {
                        pc.Pick(t as Thing, true, true);
                    }
                }
            }
            yield return Success();
            yield break;
        }
    }
}
