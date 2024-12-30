using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace AutoActMod;

public static class Settings
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

    public static void SetupSettings(ActPlan actPlan)
    {
        var text = AALang.GetText("settings");
        var dynamicAct = new DynamicAct(text, SetupSettingsUI, false);
        actPlan.list.Add(new ActPlan.Item { act = dynamicAct });
    }

    public static bool SetupSettingsUI()
    {
        var list = new UIContextMenuItem[2];
        void ToSquare()
        {
            var item1 = list[0];
            if (item1.IsNull()) { return; }
            var item2 = list[1];
            var min = Math.Max(Math.Min((int)item1.slider.value, (int)item2.slider.value), 3) / 2 * 2 + 1;
            SetSquare(min);
        }
        void SetSquare(int v)
        {
            var item1 = list[0];
            if (item1.IsNull()) { return; }
            var item2 = list[1];
            if (item2.IsNull()) { return; }
            item1.slider.value = v / 2;
            item2.slider.value = v / 2;
            item1.slider.maxValue = 12;
            item2.slider.maxValue = 12;
            item1.textSlider.text = v.ToString();
            item2.textSlider.text = v.ToString();
            BuildRangeW = v;
            BuildRangeH = v;
        }
        void ToRect()
        {
            var item1 = list[0];
            if (item1.IsNull()) { return; }
            var item2 = list[1];
            item1.slider.maxValue = 25;
            item2.slider.maxValue = 25;
            item1.slider.value = item1.slider.value * 2 + 1;
            item2.slider.value = item2.slider.value * 2 + 1;
            item1.textSlider.text = item1.slider.value.ToString();
            item2.textSlider.text = item2.slider.value.ToString();
        }
        var menu = EClass.ui.CreateContextMenu();
        menu.AddToggle(AALang.GetText("sameFarmfieldOnly"), SameFarmfieldOnly, v => SameFarmfieldOnly = v);
        menu.AddToggle(AALang.GetText("staminaCheck"), StaminaCheck, v => StaminaCheck = v);
        menu.AddToggle(AALang.GetText("ignoreEnemySpotted"), IgnoreEnemySpotted, v => IgnoreEnemySpotted = v);
        menu.AddToggle(AALang.GetText("simpleIdentify"), SimpleIdentify, v => SimpleIdentify = v);
        menu.AddToggle(AALang.GetText("startFromCenter"), StartFromCenter, v =>
        {
            StartFromCenter = v;
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
            AALang.GetText("keyMode"),
            v =>
            {
                if (v == 1)
                {
                    KeyMode = true;
                    return AALang.GetText("toggle");
                }
                else
                {
                    KeyMode = false;
                    return AALang.GetText("press");
                }
            }
            , KeyMode ? 1 : 0,
            v => { },
            0,
            1,
            true,
            false
        );
        menu.AddSlider(
            AALang.GetText("detDist"),
            v =>
            {
                var n = v / 2;
                DetRangeSq = (int)(n * n);
                return n.ToString();
            },
            (int)(Math.Sqrt(DetRangeSq) * 2),
            v => { },
            3,
            50,
            true,
            false
        );
        list[0] = menu.AddSlider(
            AALang.GetText("buildRangeW"),
            v =>
            {
                if (StartFromCenter)
                {
                    BuildRangeW = (int)v * 2 + 1;
                    SetSquare(BuildRangeW);
                }
                else
                {
                    BuildRangeW = (int)v;
                }
                return BuildRangeW.ToString();
            },
            StartFromCenter ? BuildRangeW / 2 : BuildRangeW,
            v => { },
            1,
            StartFromCenter ? 12 : 25,
            true,
            false
        );
        list[1] = menu.AddSlider(
            AALang.GetText("buildRangeH"),
            v =>
            {
                if (StartFromCenter)
                {
                    BuildRangeH = (int)v * 2 + 1;
                    SetSquare(BuildRangeH);
                }
                else
                {
                    BuildRangeH = (int)v;
                }
                return BuildRangeH.ToString();
            },
            StartFromCenter ? BuildRangeH / 2 : BuildRangeH,
            v => { },
            1,
            StartFromCenter ? 12 : 25,
            true,
            false
        );
        menu.AddSlider(
            AALang.GetText("sowRange"),
            v =>
            {
                SowRangeExists = v == 0;
                if (SowRangeExists)
                {
                    return AALang.GetText("followBuildRange");
                }
                else
                {
                    return AALang.GetText("entireFarmfield");
                }
            },
            SowRangeExists ? 0 : 1,
            v => { },
            0,
            1,
            true,
            false
        );
        menu.AddSlider(
            AALang.GetText("pourDepth"),
            v =>
            {
                PourDepth = (int)v;
                return v.ToString();
            },
            PourDepth,
            v => { },
            1,
            4,
            true,
            false
        );
        var seedReapingCountMax = 101;
        menu.AddSlider(
            AALang.GetText("seedReapingCount"),
            v =>
            {
                SeedReapingCount = (int)v == seedReapingCountMax ? 0 : (int)v;
                if (SeedReapingCount > 0)
                {
                    return v.ToString();
                }
                else
                {
                    return "∞".ToString();
                }
            },
            SeedReapingCount == 0 ? seedReapingCountMax : SeedReapingCount,
            v => { },
            1,
            seedReapingCountMax,
            true,
            false
        );
        menu.Show();
        return false;

    }
}

public static class AALang
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

    public static Dictionary<string, Dictionary<string, string>> langData = new Dictionary<string, Dictionary<string, string>> {
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
                { "simpleIdentify", "简单识别　　　　　　　　　　　" },
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
                { "simpleIdentify", "簡單識別　　　　　　　　　　　" },
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
                { "simpleIdentify", "簡単な識別　　　　　　　　　　" },
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
                { "simpleIdentify", "Simple Identification　　　　　　　　　　　　　　　　　 　 　" },
                { "ignoreEnemySpotted", "Ignore Enemy Spotted　　　　　　　　　　　　　　　　　　　" },
                { "startFromCenter", "Start From The Center (Square Only)　　　　 　　　　" },
                { "sameFarmfieldOnly", "Harvest On The Same Farmfield Only　　　　　　　　" },
            }
        }
    };
}
