using System.Collections.Generic;

namespace AutoActMod.Actions;

public class AutoActChat(Chara target) : AutoAct
{
    public int detRangeSq = Settings.DetRangeSq;
    public Chara target = target;
    public bool isTargetSpeaking = target.IsHumanSpeak;
    public HashSet<Chara> visited = [];
    public override Point Pos => target.pos;
    public override int MaxRestart => 0;

    public static AutoActChat TryCreate(string lang, Card target, Point pos)
    {
        if (lang != ACT.Chat.source.GetName() || target is not Chara c || c.IsPC) { return null; }
#if DEBUG
        AutoActMod.Log($"AutoActChat start: {Describe(c)} simpleIdentify={Settings.SimpleIdentify}");
#endif
        return new AutoActChat(c);
    }

#if DEBUG
    static string Describe(Chara c) =>
        $"#{c.uid} {c.Name} race={c.race.id} sleep={c.conSleep.HasValue()} speak={c.IsHumanSpeak} human={c.IsHuman} interest={c.interest} hostile={c.IsHostile()}";
#endif

    public bool CanChat(Chara c) => !c.IsPC
        && c.interest > 0
        && !c.IsHostile()
        && (!c.IsDeadOrSleeping || (Settings.WakeSleeping && c.conSleep.HasValue()))
        && !visited.Contains(c)
        && (c.IsHumanSpeak || pc.HasElement(1640))
        && (Settings.SimpleIdentify == 2 || c.IsHumanSpeak == isTargetSpeaking)
        && (!c.IsUnique || Lang.GetDialogSheet("unique").map.ContainsKey(c.id));

    public override IEnumerable<Status> Run()
    {
        while (CanProgress())
        {
            target = FindChara(CanChat, detRangeSq);
#if DEBUG
            AutoActMod.Log($"AutoActChat next: {(target.IsNull() ? "none" : Describe(target))} targetSpeaking={isTargetSpeaking}");
#endif
            if (target.IsNull())
            {
                SayNoTarget();
                yield break;
            }

            yield return DoGoto(Pos, 1, true);
            if (Settings.WakeSleeping && target.conSleep.HasValue())
            {
#if DEBUG
                AutoActMod.Log($"AutoActChat kick: {Describe(target)}");
#endif
                owner.Kick(target);
                yield return DoGoto(Pos, 1, true);
            }

            TalkUntilBored(target);
#if DEBUG
            AutoActMod.Log($"AutoActChat done: {Describe(target)}");
#endif
            visited.Add(target);
            yield return KeepRunning();
        }
        yield return FailOrSuccess();
    }

    static void TalkUntilBored(Chara chara)
    {
        chara.ShowDialog();
        var layer = ui.GetLayer<LayerDrama>();
        if (layer.IsNull()) { return; }

        var letsTalk = "letsTalk".lang();
        UIButton clicked = null;
        while (layer.drama._choices.Find(c => c.text == letsTalk) is DramaChoice choice
            && choice.button.HasValue()
            && !ReferenceEquals(choice.button, clicked))
        {
            clicked = choice.button;
            clicked.soundClick = null;
            clicked.onClick.Invoke();
        }

        layer.drama.sequence.Exit();
        layer.Close();
    }
}