using System.Collections.Generic;
using System.Linq;

namespace AutoActMod.Actions;

public class AutoActPick : AutoAct
{
    public int detRangeSq;
    public SubActPick Child => child as SubActPick;
    public override Point Pos => Child.pos;

    public AutoActPick(SubActPick source) : base(source)
    {
        detRangeSq = Settings.DetRangeSq;
        SetTarget(source.refThing);
    }

    public static AutoActPick TryCreate(string lang, Card target, Point pos)
    {
        if (Settings.SimpleIdentify < 2 && target.SelfWeight > 160_000) { return null; }
        if (lang == "actPickOne".lang())
        {
            return new AutoActPick(new SubActPick(pos, target as Thing, target.IsInstalled));
        }
        else if (lang == "actHold".lang())
        {
            return new AutoActPick(new SubActPick(pos, target as Thing, target.IsInstalled));
        }
        return null;
    }

    public override IEnumerable<Status> Run()
    {
        while (CanProgress())
        {
            var targetThing = FindThing(IsTarget, detRangeSq);
            if (targetThing.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            Child.pos = targetThing.pos;
            Child.refThing = targetThing;
            yield return StartNextTask();
        }
        yield break;
    }

    public class SubActPick : AIAct
    {
        public Thing refThing;
        public Point pos;
        public bool installed;
        public bool IsTarget(Card t) => t == refThing || t.CanStackTo(refThing);

        public SubActPick() { }
        public SubActPick(Point pos, Thing refThing, bool installed = false)
        {
            this.pos = pos;
            this.refThing = refThing;
            this.installed = installed;
        }

        public override IEnumerable<Status> Run()
        {
            if (Utils.Dist2(owner.pos, pos) > 2)
            {
                yield return DoGoto(pos, 1, true);
            }

            bool success = false;
            if (installed)
            {
                var t = pos.Installed;
                if ((t.IsNull() || !IsTarget(t)) && pos.HasThing)
                {
                    t = pos.Things.Find(t => t.placeState == PlaceState.installed && IsTarget(t));
                }
                if (t.HasValue() && IsTarget(t))
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
                    if (pc.held.HasValue())
                    {
                        t.PlaySoundHold(false);
                        player.RefreshCurrentHotItem();
                        ActionMode.Adv.planRight.Update(ActionMode.Adv.mouseTarget);
                        pc.renderer.Refresh();
                        success = true;
                    }
                }
            }
            else
            {
                pos.Things.Where(IsTarget).ToArray().ForeachReverse(t =>
                {
                    pc.Pick(t, true, true);
                    success = true;
                });
            }
            if (success)
            {
                yield break;
            }
            else
            {
                yield return Cancel();
            }
        }
    }
}