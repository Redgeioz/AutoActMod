using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AutoAct;

static class Settings
{
    public static ConfigEntry<int> detDistSq;
    public static ConfigEntry<int> buildRangeW;
    public static ConfigEntry<int> buildRangeH;
    public static ConfigEntry<bool> sowRangeExists;
    public static ConfigEntry<int> seedReapingCount;
    public static ConfigEntry<int> pourDepth;
    public static ConfigEntry<bool> staminaCheck;
    public static ConfigEntry<bool> sameFarmfieldOnly;
    public static ConfigEntry<bool> ignoreEnemySpotted;
    public static ConfigEntry<bool> simpleIdentify;
    public static ConfigEntry<bool> startFromCenter;
    public static ConfigEntry<bool> keyMode;

    public static ConfigEntry<KeyCode> keyCode;

    public static int DetRangeSq
    {
        get { return detDistSq.Value; }
        set { detDistSq.Value = value; }
    }

    public static int BuildRangeW
    {
        get { return buildRangeW.Value; }
        set { buildRangeW.Value = value; }
    }

    public static int BuildRangeH
    {
        get { return buildRangeH.Value; }
        set { buildRangeH.Value = value; }
    }

    public static bool SowRangeExists
    {
        get { return sowRangeExists.Value; }
        set { sowRangeExists.Value = value; }
    }

    public static int PourDepth
    {
        get { return pourDepth.Value; }
        set { pourDepth.Value = value; }
    }

    public static int SeedReapingCount
    {
        get { return seedReapingCount.Value; }
        set { seedReapingCount.Value = value; }
    }

    public static bool StaminaCheck
    {
        get { return staminaCheck.Value; }
        set { staminaCheck.Value = value; }
    }

    public static bool SameFarmfieldOnly
    {
        get { return sameFarmfieldOnly.Value; }
        set { sameFarmfieldOnly.Value = value; }
    }

    public static bool IgnoreEnemySpotted
    {
        get { return ignoreEnemySpotted.Value; }
        set { ignoreEnemySpotted.Value = value; }
    }

    public static bool SimpleIdentify
    {
        get { return simpleIdentify.Value; }
        set { simpleIdentify.Value = value; }
    }

    public static bool StartFromCenter
    {
        get { return startFromCenter.Value; }
        set { startFromCenter.Value = value; }
    }

    public static bool KeyMode
    {
        get { return keyMode.Value; }
        set { keyMode.Value = value; }
    }

    public static KeyCode KeyCode
    {
        get { return keyCode.Value; }
        set { keyCode.Value = value; }
    }
}

[HarmonyPatch(typeof(ActPlan), "ShowContextMenu")]
static class ShowContextMenu_Patch
{
    [HarmonyPrefix]
    public static void Prefix(ActPlan __instance)
    {
        if (!__instance.pos.Equals(EClass.pc.pos))
        {
            return;
        }

        string text = ALang.GetText("settings");
        DynamicAct dynamicAct = new DynamicAct(text, () =>
        {
            UIContextMenuItem[] list = new UIContextMenuItem[2];
            void ToSquare()
            {
                UIContextMenuItem item1 = list[0];
                if (item1.IsNull()) { return; }
                UIContextMenuItem item2 = list[1];
                int min = Math.Max(Math.Min((int)item1.slider.value, (int)item2.slider.value), 3) / 2 * 2 + 1;
                SetSquare(min);
            }
            void SetSquare(int v)
            {
                UIContextMenuItem item1 = list[0];
                if (item1.IsNull()) { return; }
                UIContextMenuItem item2 = list[1];
                if (item2.IsNull()) { return; }
                item1.slider.value = v / 2;
                item2.slider.value = v / 2;
                item1.slider.maxValue = 12;
                item2.slider.maxValue = 12;
                item1.textSlider.text = v.ToString();
                item2.textSlider.text = v.ToString();
                Settings.BuildRangeW = v;
                Settings.BuildRangeH = v;
            }
            void ToRect()
            {
                UIContextMenuItem item1 = list[0];
                if (item1.IsNull()) { return; }
                UIContextMenuItem item2 = list[1];
                item1.slider.maxValue = 25;
                item2.slider.maxValue = 25;
                item1.slider.value = item1.slider.value * 2 + 1;
                item2.slider.value = item2.slider.value * 2 + 1;
                item1.textSlider.text = item1.slider.value.ToString();
                item2.textSlider.text = item2.slider.value.ToString();
            }
            UIContextMenu menu = EClass.ui.CreateContextMenu();
            menu.AddToggle(ALang.GetText("sameFarmfieldOnly"), Settings.SameFarmfieldOnly, v => Settings.SameFarmfieldOnly = v);
            menu.AddToggle(ALang.GetText("staminaCheck"), Settings.StaminaCheck, v => Settings.StaminaCheck = v);
            menu.AddToggle(ALang.GetText("ignoreEnemySpotted"), Settings.IgnoreEnemySpotted, v => Settings.IgnoreEnemySpotted = v);
            menu.AddToggle(ALang.GetText("simpleIdentify"), Settings.SimpleIdentify, v => Settings.SimpleIdentify = v);
            menu.AddToggle(ALang.GetText("startFromCenter"), Settings.StartFromCenter, v =>
            {
                Settings.StartFromCenter = v;
                if (v)
                {
                    ToSquare();
                }
                else
                {
                    ToRect();
                }
            });
            menu.AddSlider(
                ALang.GetText("keyMode"),
                v =>
                {
                    if (v == 1)
                    {
                        Settings.KeyMode = true;
                        return ALang.GetText("toggle");
                    }
                    else
                    {
                        Settings.KeyMode = false;
                        return ALang.GetText("press");
                    }
                }
                , Settings.KeyMode ? 1 : 0,
                v => { },
                0,
                1,
                true,
                false
            );
            menu.AddSlider(
                ALang.GetText("detDist"),
                v =>
                {
                    float n = v / 2;
                    Settings.DetRangeSq = (int)(n * n);
                    return n.ToString();
                },
                (int)(Math.Sqrt(Settings.DetRangeSq) * 2),
                v => { },
                3,
                50,
                true,
                false
            );
            list[0] = menu.AddSlider(
                ALang.GetText("buildRangeW"),
                v =>
                {
                    if (Settings.StartFromCenter)
                    {
                        Settings.BuildRangeW = (int)v * 2 + 1;
                        SetSquare(Settings.BuildRangeW);
                    }
                    else
                    {
                        Settings.BuildRangeW = (int)v;
                    }
                    return Settings.BuildRangeW.ToString();
                },
                Settings.StartFromCenter ? Settings.BuildRangeW / 2 : Settings.BuildRangeW,
                v => { },
                1,
                Settings.StartFromCenter ? 12 : 25,
                true,
                false
            );
            list[1] = menu.AddSlider(
                ALang.GetText("buildRangeH"),
                v =>
                {
                    if (Settings.StartFromCenter)
                    {
                        Settings.BuildRangeH = (int)v * 2 + 1;
                        SetSquare(Settings.BuildRangeH);
                    }
                    else
                    {
                        Settings.BuildRangeH = (int)v;
                    }
                    return Settings.BuildRangeH.ToString();
                },
                Settings.StartFromCenter ? Settings.BuildRangeH / 2 : Settings.BuildRangeH,
                v => { },
                1,
                Settings.StartFromCenter ? 12 : 25,
                true,
                false
            );
            menu.AddSlider(
                ALang.GetText("sowRange"),
                v =>
                {
                    Settings.SowRangeExists = v == 0;
                    if (Settings.SowRangeExists)
                    {
                        return ALang.GetText("followBuildRange");
                    }
                    else
                    {
                        return ALang.GetText("entireFarmfield");
                    }
                },
                Settings.SowRangeExists ? 0 : 1,
                v => { },
                0,
                1,
                true,
                false
            );
            menu.AddSlider(
                ALang.GetText("pourDepth"),
                v =>
                {
                    Settings.PourDepth = (int)v;
                    return v.ToString();
                },
                Settings.PourDepth,
                v => { },
                1,
                4,
                true,
                false
            );
            int seedReapingCountMax = 101;
            menu.AddSlider(
                ALang.GetText("seedReapingCount"),
                v =>
                {
                    Settings.SeedReapingCount = (int)v == seedReapingCountMax ? 0 : (int)v;
                    if (Settings.SeedReapingCount > 0)
                    {
                        return v.ToString();
                    }
                    else
                    {
                        return "∞".ToString();
                    }
                },
                Settings.SeedReapingCount == 0 ? seedReapingCountMax : Settings.SeedReapingCount,
                v => { },
                1,
                seedReapingCountMax,
                true,
                false
            );
            menu.Show();
            return false;
        }, false);
        Act act = dynamicAct;
        __instance.list.Add(new ActPlan.Item
        {
            act = act,
        });
    }
}

[HarmonyPatch(typeof(ActPlan.Item), "Perform")]
static class ActPlan_Patch
{
    [HarmonyPrefix]
    static bool Prefix(ActPlan.Item __instance)
    {
        if (__instance.act is DynamicAct act && act.id == ALang.GetText("settings"))
        {
            act.Perform();
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharaRenderer), "OnEnterScreen")]
static class CharaRenderer_OnEnterScreen_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (AutoAct.active && Settings.IgnoreEnemySpotted)
        {
            EClass.player.enemySpotted = false;
        }
    }
}

static class ALang
{
    static public string GetText(string text)
    {
        string lang = EClass.core.config.lang;
        if (!langData.ContainsKey(lang))
        {
            lang = "EN";
        }
        return langData[lang][text];
    }

    static readonly Dictionary<string, Dictionary<string, string>> langData = new Dictionary<string, Dictionary<string, string>> {
            {
                "CN", new Dictionary<string, string> {
                    { "autoact", "自动行动" },
                    { "settings", "自动行动设置" },
                    { "detDist", "探测距离" },
                    { "buildRangeW", "建造范围宽"},
                    { "buildRangeH", "建造范围高"},
                    { "sowRange", "播种范围" },
                    { "pourDepth", "倒水深度" },
                    { "seedReapingCount", "种子收获数" },
                    { "keyMode", "按键模式" },
                    { "press", "按住" },
                    { "toggle", "切换" },
                    { "start", "自动行动，启动！"},
                    { "fail", "自动行动已中断。"},
                    { "noTarget", "自动行动没有找到下一个目标。"},
                    { "on", "自动行动，启动！"},
                    { "off", "自动行动，关闭。"},
                    { "staminaCheck", "精力为零时停止　　　　　　　　" },
                    { "entireFarmfield", "整个田地" },
                    { "followBuildRange", "同建造范围" },
                    { "simpleIdentify", "仅识别目标是方块还是植物　　　" },
                    { "ignoreEnemySpotted", "忽视发现的敌人　　　　　　　　" },
                    { "startFromCenter", "从中心开始（限制为正方形）　　" },
                    { "sameFarmfieldOnly", "只在同一田地上收割　　　　　　" },
                }
            },
            {
                "ZHTW", new Dictionary<string, string> {
                    { "autoact", "自動行動" },
                    { "settings", "自動行動設定" },
                    { "detDist", "探測距離" },
                    { "buildRangeW", "建造範圍寬" },
                    { "buildRangeH", "建造範圍高" },
                    { "sowRange", "播種範圍" },
                    { "pourDepth", "倒水深度" },
                    { "seedReapingCount", "種子收獲數" },
                    { "keyMode", "按鍵模式" },
                    { "press", "按住" },
                    { "toggle", "切換" },
                    { "start", "自動行動，啟動！"},
                    { "fail", "自動行動已中斷。"},
                    { "noTarget", "自動行動沒有找到下一個目標。"},
                    { "on", "自動行動，啟動！"},
                    { "off", "自動行動，關閉。"},
                    { "staminaCheck", "精力為零時停止　　　　　　　　" },
                    { "entireFarmfield", "整個田地" },
                    { "followBuildRange", "同建造範圍" },
                    { "simpleIdentify", "僅識別目標是方塊還是植物　　　" },
                    { "ignoreEnemySpotted", "忽視發現的敵人　　　　　　　　" },
                    { "startFromCenter", "从中心開始（限製為正方形）　　" },
                    { "sameFarmfieldOnly", "只在同一田地上收割　　　　　　" },
                }
            },
            {
                "JP", new Dictionary<string, string> {
                    { "autoact", "自動行動" },
                    { "settings", "自動行動設定" },
                    { "detDist", "検出距離" },
                    { "buildRangeW", "建築範囲の幅" },
                    { "buildRangeH", "建築範囲の高さ" },
                    { "sowRange", "播種範囲" },
                    { "pourDepth", "注水深さ" },
                    { "seedReapingCount", "種子収穫数" },
                    { "keyMode", "キーモード" },
                    { "press", "押す" },
                    { "toggle", "切り替え" },
                    { "start", "自動行動開始済み。"},
                    { "fail", "自動行動中断済み。"},
                    { "noTarget", "自動行動は次目標を発見できず。"},
                    { "on", "自動行動：オン。"},
                    { "off", "自動行動：オフ。"},
                    { "staminaCheck", "スタミナゼロで停止　　　　　　" },
                    { "entireFarmfield", "現在は農地全体" },
                    { "followBuildRange", "建築範囲と同じ" },
                    { "simpleIdentify", "ブロックか植物かだけを識別する" },
                    { "ignoreEnemySpotted", "発見した敵を無視する　　　　　" },
                    { "startFromCenter", "中心から開始（正方形に制限）　" },
                    { "sameFarmfieldOnly", "同じ農地での収穫のみ　　　　　" },
                }
            },
            {
                "EN", new Dictionary<string, string> {
                    { "autoact", "Auto Act" },
                    { "settings", "Auto Act Settings" },
                    { "detDist", "Detection Distance" },
                    { "buildRangeW", "Building Range Width" },
                    { "buildRangeH", "Building Range Height" },
                    { "sowRange", "Sowing Range" },
                    { "pourDepth", "Pouring Depth" },
                    { "seedReapingCount", "Count For Seed Reaping" },
                    { "keyMode", "Key Mode" },
                    { "press", "Press" },
                    { "toggle", "Toggle" },
                    { "start", "Auto Act started."},
                    { "fail", "Auto Act was interrupted."},
                    { "noTarget", "Auto Act could not find the next target."},
                    { "on", "Auto Act: On."},
                    { "off", "Auto Act: Off."},
                    { "staminaCheck", "Stop When Zero Stamina　　　　　　　　　　　　　　　　　" },
                    { "entireFarmfield", "The Entire Current Farmfield" },
                    { "followBuildRange", "Follow The Building Range" },
                    { "simpleIdentify", "Identify If The Target Is A Block Or A Plant Only" },
                    { "ignoreEnemySpotted", "Ignore Enemy Spotted　　　　　　　　　　　　　　　　　　　" },
                    { "startFromCenter", "Start From The Center (Square Only)　　　　 　　　　" },
                    { "sameFarmfieldOnly", "Harvest On The Same Farmfield Only　　　　　　　　" },
                }
            }
        };
}
