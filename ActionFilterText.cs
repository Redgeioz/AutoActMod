using System;
using System.Collections.Generic;

namespace AutoActMod;

public static class ActionFilterText
{
    static readonly Dictionary<string, string[]> translations = new()
    {
        ["title"] = ["AutoAct 动作过滤器", "AutoAct 動作篩選器", "AutoAct Action Filter", "AutoAct 行動フィルター"],
        ["allOn"] = ["全部启用", "全部啟用", "Enable All", "すべて有効"],
        ["allOff"] = ["全部禁用", "全部停用", "Disable All", "すべて無効"],
        ["help"] = ["开关只控制新建的自动行动，已开始的任务继续执行。手动操作始终可用，修改立即保存。", "開關只控制新建的自動行動，已開始的任務繼續執行。手動操作始終可用，修改立即儲存。", "Switches apply to new automation jobs. Existing jobs continue; manual actions remain available. Changes are saved immediately.", "設定は新しく開始する自動行動に適用されます。実行中の行動と手動操作には影響せず、変更はすぐに保存されます。"],
        ["count"] = ["已启用 {0}/{1} 项", "已啟用 {0}/{1} 項", "Enabled: {0}/{1}", "有効: {0}/{1}"],
        ["gathering"] = ["采集与建设", "採集與建設", "Gathering and Construction", "採集と建設"],
        ["farming"] = ["农务", "農務", "Farming", "農作業"],
        ["animals"] = ["动物作业", "動物作業", "Animal Work", "動物の世話"],
        ["interactions"] = ["日常与交互", "日常與互動", "Other Interactions", "日常と交流"],
        ["Smash"] = ["砸碎物件", "砸碎物件", "Smash Objects", "物を砕く"],
        ["Build"] = ["建造", "建造", "Build", "建設"],
        ["FeedCoin"] = ["扭蛋投币", "扭蛋投幣", "Feed Coin (Gacha)", "ガチャにコインを入れる"],
        ["OpenGacha"] = ["开扭蛋", "開扭蛋", "Open Gacha Ball", "ガチャ玉を開ける"]
    };

    // Use game language keys/source names except for the explicitly named Build, Gacha and Smash switches, without creating tasks
    // or calling patched GetText methods (which can append the AutoAct hint or require a target).
    public static string ActionName(AutoAction action) => action switch
    {
        AutoAction.Mine => Element.GetName(nameof(TaskMine)),
        AutoAction.Collect => Element.GetName(nameof(TaskHarvest)),
        AutoAction.ReapSeed => "TaskHarvestSeed".lang(),
        AutoAction.Dig => Element.GetName(nameof(TaskDig)),
        AutoAction.Pick => "actPickOne".lang(),
        AutoAction.Build => Get("Build"),
        AutoAction.Disassemble => "TaskDisassemble".lang(),
        AutoAction.Smash => Get("Smash"),
        AutoAction.Plow => Element.GetName(nameof(TaskPlow)),
        AutoAction.PlantSeed => "actInstallSeed".lang(),
        AutoAction.Fertilize => "actInstallFertilizer".lang(),
        AutoAction.WaterCrops => "TaskWater".lang(),
        AutoAction.DrawWater => "TaskDrawWater".lang(),
        AutoAction.PourWater => "TaskPourWater".lang(),
        AutoAction.Shear => "AI_Shear".lang(),
        AutoAction.Brush => Element.GetName(nameof(AI_TendAnimal)),
        AutoAction.Slaughter => "AI_Slaughter".lang(),
        AutoAction.Clean => "actClean".lang(),
        AutoAction.Read => Element.GetName(nameof(AI_Read)),
        AutoAction.Chat => ACT.Chat.source.GetName(),
        AutoAction.Steal => Element.Get(6011).GetName(),
        AutoAction.Unlock => Element.GetName(nameof(AI_OpenLock)),
        AutoAction.FeedBaby => "actMilk".lang(),
        AutoAction.FeedCoin => Get("FeedCoin"),
        AutoAction.OpenGacha => Get("OpenGacha"),
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown automation action.")
    };

    public static string Get(string key)
    {
        if (key == "gacha") return "gacha".lang();
        var index = EClass.core.config.lang switch { "CN" => 0, "ZHTW" => 1, "JP" => 3, _ => 2 };
        return translations[key][index];
    }
}
