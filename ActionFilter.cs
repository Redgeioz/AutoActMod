using System;
using System.Collections.Generic;
using System.Linq;
using AutoActMod.Actions;
using BepInEx.Configuration;

namespace AutoActMod;

// Names are persisted as configuration keys; keep them stable.
public enum AutoAction
{
    Mine, Collect, ReapSeed, Dig, Pick, Build, Disassemble, Smash,
    Plow, PlantSeed, Fertilize, WaterCrops, DrawWater, PourWater,
    Shear, Brush, Slaughter, Clean, Read, Chat, Steal, Unlock, FeedBaby,
    FeedCoin, OpenGacha
}

public static class ActionFilter
{
    public static readonly AutoAction[] Actions = (AutoAction[])Enum.GetValues(typeof(AutoAction));
    static readonly Dictionary<AutoAction, ConfigEntry<bool>> entries = new();

    public static void Initialize(ConfigFile config)
    {
        foreach (var action in Actions)
        {
            entries.Add(action, config.Bind("ActionFilter", action.ToString(), true,
                "Allow creating new AutoAct jobs for this action. Existing jobs and manual actions are unaffected."));
        }
        MergeLegacyHarvest(config, entries[AutoAction.Collect]);
    }

    internal static void MergeLegacyHarvest(ConfigFile config, ConfigEntry<bool> collect)
    {
        // Bind consumes the old unregistered value. Remove and save so migration runs only once.
        var harvest = config.Bind("ActionFilter", "Harvest", true);
        var enabled = collect.Value && harvest.Value;
        config.Remove(harvest.Definition);
        collect.Value = enabled;
        config.Save();
    }

    public static bool IsEnabled(AutoAction? action) =>
        !action.HasValue || !entries.TryGetValue(action.Value, out var entry) || entry.Value;

    public static int EnabledCount => Actions.Count(action => IsEnabled(action));

    public static void SetEnabled(AutoAction action, bool enabled) => entries[action].Value = enabled;

    public static void SetAll(bool enabled)
    {
        foreach (var entry in entries.Values) entry.Value = enabled;
    }

    public sealed class CreationPolicy
    {
        readonly HashSet<AutoAction> disabled;
        internal CreationPolicy(IEnumerable<AutoAction> actions) => disabled = new(actions);
        public bool IsEnabled(AutoAction action) => !disabled.Contains(action);
    }

    // Shared harvest/mine jobs retain the action scope chosen when they are created.
    public static CreationPolicy CaptureCreationPolicy() => new(Actions.Where(action => !IsEnabled(action)));

    public static AutoAction GetBuildAction(Card held) => held?.trait switch
    {
        TraitSeed => AutoAction.PlantSeed,
        TraitFertilizer => AutoAction.Fertilize,
        _ => AutoAction.Build
    };

    public static AutoAction GetHarvestAction(TaskHarvest task) =>
        GetHarvestAction(task.pos, task.owner ?? EClass.core?.game?.player?.chara, task.mode);

    public static AutoAction GetHarvestAction(Point pos, Chara owner,
        BaseTaskHarvest.HarvestType mode = BaseTaskHarvest.HarvestType.Obj)
    {
        if (mode == BaseTaskHarvest.HarvestType.Disassemble) return AutoAction.Disassemble;
        if (mode == BaseTaskHarvest.HarvestType.Obj && pos != null)
        {
            var tool = owner?.Tool;
            if (pos.cell.CanReapSeed() && tool != null
                && (tool.trait is TraitToolSickle || tool.category.IsChildOf("scythe"))) return AutoAction.ReapSeed;
        }
        return AutoAction.Collect;
    }

    public static bool CanCreate(AIAct source) => IsEnabled(GetAction(source));

    public static AutoAction? GetAction(AIAct source) => source switch
    {
        // A range can contain several actions; its positions use the captured creation policy.
        AutoActHarvestMine a => a.hasRange ? null : GetAction(a.initTask),
        AutoActBuild a => GetBuildAction(a.Child.held),
        AutoActDig => AutoAction.Dig,
        AutoActPick => AutoAction.Pick,
        AutoActSmash => AutoAction.Smash,
        AutoActPlow => AutoAction.Plow,
        AutoActWater => AutoAction.WaterCrops,
        AutoActDrawWater => AutoAction.DrawWater,
        AutoActPourWater => AutoAction.PourWater,
        AutoActShear => AutoAction.Shear,
        AutoActBrush => AutoAction.Brush,
        AutoActSlaughter => AutoAction.Slaughter,
        AutoActClean => AutoAction.Clean,
        AutoActRead => AutoAction.Read,
        AutoActChat => AutoAction.Chat,
        AutoActSteal => AutoAction.Steal,
        AutoActUnlock => AutoAction.Unlock,
        AutoActThrowMilk => AutoAction.FeedBaby,
        TaskMine => AutoAction.Mine,
        TaskHarvest task => GetHarvestAction(task),
        TaskBuild task => GetBuildAction(task.held),
        TaskDig => AutoAction.Dig,
        TaskPlow => AutoAction.Plow,
        TaskWater => AutoAction.WaterCrops,
        TaskDrawWater => AutoAction.DrawWater,
        TaskPourWater => AutoAction.PourWater,
        AI_Shear => AutoAction.Shear,
        AI_TendAnimal => AutoAction.Brush,
        AI_Slaughter => AutoAction.Slaughter,
        TaskClean => AutoAction.Clean,
        AI_Read => AutoAction.Read,
        AI_Steal => AutoAction.Steal,
        AI_OpenLock => AutoAction.Unlock,
        _ => null // Unknown helpers (including trap handling) and extensions remain available.
    };

    // This must not create an AutoAct: its constructor changes the original task's status.
    public static AutoAction? GetAction(string text, Card target)
    {
        if (text == "actPickOne".lang() || text == "actHold".lang()) return AutoAction.Pick;
        if (text == ACT.Chat.source.GetName()) return AutoAction.Chat;
        if (text == "AI_Slaughter".lang()) return AutoAction.Slaughter;
        if (text == "ActDrawWater".lang()) return AutoAction.WaterCrops;
        if (text == Element.Get(6011).GetName()) return AutoAction.Steal;
        if ((text == "ActThrow".lang() || text == "actMilk".lang())
            && EClass.pc.held?.trait is TraitDrinkMilkMother) return AutoAction.FeedBaby;
        if (text == AutoActSmash.GetActMeleeLang() && AutoActSmash.CanSmash(target)) return AutoAction.Smash;
        return null;
    }

    public static bool CanShowHint(Act act, Card target, string text)
    {
        if (act is AIAct ai) return CanCreate(ai);
        if (act is ActChat) return IsEnabled(AutoAction.Chat);
        if (act is ActDrawWater) return IsEnabled(AutoAction.WaterCrops);
        return IsEnabled(GetAction(text, target));
    }
}
