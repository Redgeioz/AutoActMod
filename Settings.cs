using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AutoAct
{
    static class Settings
    {
        public static ConfigEntry<int> detRangeSq;
        public static ConfigEntry<int> digRange;
        public static ConfigEntry<int> plowRange;
        public static ConfigEntry<int> sowRange;
        public static ConfigEntry<int> pourRange;
        public static ConfigEntry<int> pourDepth;
        public static ConfigEntry<int> seedReapingCount;
        public static ConfigEntry<bool> staminaCheck;
        public static ConfigEntry<bool> sameFarmfieldOnly;
        public static ConfigEntry<bool> keyMode;

        public static ConfigEntry<KeyCode> keyCode;

        public static int DetRangeSq
        {
            get { return detRangeSq.Value; }
            set { detRangeSq.Value = value; }
        }

        public static int DigRange
        {
            get { return digRange.Value; }
            set { digRange.Value = value; }
        }

        public static int PlowRange
        {
            get { return plowRange.Value; }
            set { plowRange.Value = value; }
        }

        public static int SowRange
        {
            get { return sowRange.Value; }
            set { sowRange.Value = value; }
        }

        public static int PourRange
        {
            get { return pourRange.Value; }
            set { pourRange.Value = value; }
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
            DynamicAct dynamicAct = new DynamicAct(text, delegate
            {
                UIContextMenu menu = EClass.ui.CreateContextMenu();
                menu.AddToggle(ALang.GetText("sameFarmfieldOnly"), Settings.SameFarmfieldOnly, v => Settings.SameFarmfieldOnly = v);
                menu.AddToggle(ALang.GetText("staminaCheck"), Settings.StaminaCheck, v => Settings.StaminaCheck = v);
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
                    ALang.GetText("detRange"),
                    v =>
                    {
                        float n = v / 2;
                        Settings.DetRangeSq = (int)(n * n);
                        return n.ToString();
                    },
                    (int)Math.Sqrt(Settings.DetRangeSq) * 2,
                    v => { },
                    3,
                    30,
                    true,
                    false
                );
                menu.AddSlider(
                    ALang.GetText("digRange"),
                    v =>
                    {
                        Settings.DigRange = (int)v;
                        string str = (Settings.DigRange * 2 + 1).ToString();
                        return str + "x" + str;
                    },
                    Settings.DigRange,
                    v => { },
                    1,
                    12,
                    true,
                    false
                );
                menu.AddSlider(
                    ALang.GetText("plowRange"),
                    v =>
                    {
                        Settings.PlowRange = (int)v;
                        string str = (Settings.PlowRange * 2 + 1).ToString();
                        return str + "x" + str;
                    },
                    Settings.PlowRange,
                    v => { },
                    1,
                    12,
                    true,
                    false
                );
                int sowRangeMax = 13;
                menu.AddSlider(
                    ALang.GetText("sowRange"),
                    v =>
                    {
                        Settings.SowRange = (int)v == sowRangeMax ? 0 : (int)v;
                        if (Settings.SowRange > 0)
                        {
                            string str = (Settings.SowRange * 2 + 1).ToString();
                            return str + "x" + str;
                        }
                        else
                        {
                            return ALang.GetText("entireFarmfield");
                        }
                    },
                    Settings.SowRange == 0 ? sowRangeMax : Settings.SowRange,
                    v => { },
                    1,
                    sowRangeMax,
                    true,
                    false
                );
                menu.AddSlider(
                    ALang.GetText("pourRange"),
                    v =>
                    {
                        Settings.PourRange = (int)v;
                        string str = (Settings.PourRange * 2 + 1).ToString();
                        return str + "x" + str;
                    },
                    Settings.PourRange,
                    v => { },
                    1,
                    12,
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
            }, false)
            {
                id = text,
                dist = 1,
                isHostileAct = false,
                localAct = true,
                canRepeat = () => false
            };
            Act act = dynamicAct;
            __instance.list.Add(new ActPlan.Item
            {
                act = act,
                tc = null,
                pos = EClass.pc.pos.Copy()
            });
            return;
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
                    { "detRange", "探测范围" },
                    { "digRange", "地面挖掘范围" },
                    { "plowRange", "耕地范围" },
                    { "sowRange", "播种范围" },
                    { "pourRange", "倒水范围" },
                    { "pourDepth", "倒水深度" },
                    { "seedReapingCount", "种子收获数" },
                    { "keyMode", "按键模式" },
                    { "press", "按住" },
                    { "toggle", "切换" },
                    { "on", "自动行动，启动！"},
                    { "off", "自动行动，关闭。"},
                    { "staminaCheck", "精力为零停止" },
                    { "entireFarmfield", "整个田地" },
                    { "sameFarmfieldOnly", "只在同一田地上收割" },
                }
            },
            {
                "ZHTW", new Dictionary<string, string> {
                    { "autoact", "自動行動" },
                    { "settings", "自動行動設定" },
                    { "detRange", "探測範圍" },
                    { "digRange", "地面挖掘範圍" },
                    { "plowRange", "耕地範圍" },
                    { "sowRange", "播種範圍" },
                    { "pourRange", "倒水範圍" },
                    { "pourDepth", "倒水深度" },
                    { "seedReapingCount", "種子收獲數" },
                    { "keyMode", "按鍵模式" },
                    { "press", "按住" },
                    { "toggle", "切換" },
                    { "on", "自動行動，啟動！"},
                    { "off", "自動行動，關閉。"},
                    { "staminaCheck", "精力為零停止" },
                    { "entireFarmfield", "整個田地" },
                    { "sameFarmfieldOnly", "只在同一田地上收割" },
                }
            },
            {
                "JP", new Dictionary<string, string> {
                    { "autoact", "自動行動" },
                    { "settings", "自動行動設定" },
                    { "detRange", "検出範囲" },
                    { "digRange", "地面掘削範囲" },
                    { "plowRange", "耕す範囲" },
                    { "sowRange", "播種範囲" },
                    { "pourRange", "注水範囲" },
                    { "pourDepth", "注水深さ" },
                    { "seedReapingCount", "種子収穫数" },
                    { "keyMode", "キーモード" },
                    { "press", "押す" },
                    { "toggle", "切り替え" },
                    { "on", "自動行動：オン。"},
                    { "off", "自動行動：オフ。"},
                    { "staminaCheck", "スタミナゼロで停止" },
                    { "entireFarmfield", "現在は農地全体" },
                    { "sameFarmfieldOnly", "同じ農地での収穫のみ" },
                }
            },
            {
                "EN", new Dictionary<string, string> {
                    { "autoact", "Auto Act" },
                    { "settings", "Auto Act Settings" },
                    { "detRange", "Detection Range" },
                    { "digRange", "Digging Range" },
                    { "plowRange", "Plowing Range" },
                    { "sowRange", "Sowing Range" },
                    { "pourRange", "Pouring Range" },
                    { "pourDepth", "Pouring Depth" },
                    { "seedReapingCount", "Count For Seed Reaping" },
                    { "keyMode", "Key Mode" },
                    { "press", "Press" },
                    { "toggle", "Toggle" },
                    { "on", "Auto Act: On."},
                    { "off", "Auto Act: Off."},
                    { "staminaCheck", "Stop When Zero Stamina" },
                    { "entireFarmfield", "The Entire Current Farmfield" },
                    { "sameFarmfieldOnly", "Harvest On The Same Farmfield Only" },
                }
            }
        };
    }
}