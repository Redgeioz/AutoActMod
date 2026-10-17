using System.Collections.Generic;
using BepInEx.Configuration;
using EModding;

namespace AutoActMod;

public static class ActionFilterConfig
{
    public static bool IsOpen => EClass.ui && EClass.ui.GetLayer<LayerModConfig>();

    static readonly (string name, AutoAction[] actions)[] groups =
    [
        ("gathering", [AutoAction.Mine, AutoAction.Collect,
            AutoAction.ReapSeed, AutoAction.Dig, AutoAction.Pick, AutoAction.Build,
            AutoAction.Disassemble, AutoAction.Smash]),
        ("farming", [AutoAction.Plow, AutoAction.PlantSeed, AutoAction.Fertilize,
            AutoAction.WaterCrops, AutoAction.DrawWater, AutoAction.PourWater]),
        ("animals", [AutoAction.Shear, AutoAction.Brush, AutoAction.Slaughter]),
        ("interactions", [AutoAction.Clean, AutoAction.Read, AutoAction.Chat,
            AutoAction.Steal, AutoAction.Unlock, AutoAction.FeedBaby]),
        ("gacha", [AutoAction.FeedCoin, AutoAction.OpenGacha])
    ];

    public static void Build(UINote note, ConfigFile config)
    {
        note.AddHeader(ActionFilterText.Get("title"));
        note.AddText(ActionFilterText.Get("help"), FontColor.Passive);
        var count = note.AddText(CountText()).text1;
        // Widgets belong to this page build; the native settings page recreates them on every show/reset.
        var toggles = new Dictionary<AutoAction, UIButton>();
        note.AddButton(ActionFilterText.Get("allOn"), () => SetAll(true));
        note.AddButton(ActionFilterText.Get("allOff"), () => SetAll(false));

        foreach (var (name, actions) in groups)
        {
            note.AddHeader(ActionFilterText.Get(name));
            foreach (var action in actions)
            {
                toggles[action] = note.AddToggle(ActionFilterText.ActionName(action), ActionFilter.IsEnabled(action), value =>
                {
                    ActionFilter.SetEnabled(action, value);
                    count.SetText(CountText());
                });
            }
        }

        // Implementing IModConfig replaces the generated page, so keep the existing general entries available.
        note.AddHeader(AALang.GetText("settings"));
        foreach (var entry in config)
        {
            if (entry.Key.Section == "Settings") note.Add(entry.Value);
        }

        void SetAll(bool enabled)
        {
            ActionFilter.SetAll(enabled);
            foreach (var pair in toggles) pair.Value.SetCheck(ActionFilter.IsEnabled(pair.Key));
            count.SetText(CountText());
        }
    }

    static string CountText() => string.Format(ActionFilterText.Get("count"), ActionFilter.EnabledCount, ActionFilter.Actions.Length);
}
