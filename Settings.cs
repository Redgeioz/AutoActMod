using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;

namespace AutoAct
{
    static class Settings
    {
        public static ConfigEntry<int> detRangeSq;
        public static ConfigEntry<int> digRange;
        public static ConfigEntry<int> plowRange;

        public static ConfigEntry<bool> sameFarmfieldOnly;

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

        public static bool SameFarmfieldOnly
        {
            get { return sameFarmfieldOnly.Value; }
            set { sameFarmfieldOnly.Value = value; }
        }
    }

    [HarmonyPatch(typeof(ActPlan), "ShowContextMenu")]
    static class ShowContextMenu_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ActPlan __instance)
        {
            if (__instance.pos.Equals(EClass.pc.pos))
            {
                bool flag2 = EClass.core.config.lang == "CN";
                string text = Lang.GetText("settings");
                DynamicAct dynamicAct = new DynamicAct(text, delegate
                {
                    UIContextMenu menu = EClass.ui.CreateContextMenu();
                    menu.AddToggle(Lang.GetText("sameFarmfieldOnly"), Settings.SameFarmfieldOnly, (v) => Settings.SameFarmfieldOnly = v);
                    menu.AddSlider(
                        Lang.GetText("detRange"),
                        (v) =>
                        {
                            float n = v / 2;
                            Settings.DetRangeSq = (int)(n * n);
                            return Lang.GetText("detRange") + " (" + n.ToString() + ")";
                        },
                        (int)Math.Sqrt(Settings.DetRangeSq) * 2,
                        (v) => { },
                        3,
                        30,
                        true
                    );
                    menu.AddSlider(
                        Lang.GetText("digRange"),
                        (v) =>
                        {
                            Settings.DigRange = (int)v;
                            string str = (Settings.DigRange * 2 + 1).ToString();
                            return Lang.GetText("digRange") + " (" + str + "x" + str + ")";
                        },
                        Settings.DigRange,
                        (v) => { },
                        1,
                        12,
                        true
                    );
                    menu.AddSlider(
                        Lang.GetText("plowRange"),
                        (v) =>
                        {
                            Settings.PlowRange = (int)v;
                            string str = (Settings.PlowRange * 2 + 1).ToString();
                            return Lang.GetText("plowRange") + " (" + str + "x" + str + ")";
                        },
                        Settings.PlowRange,
                        (v) => { },
                        1,
                        12,
                        true
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
            }
            return true;
        }
    }

    static class Lang
    {
        static public string GetText(string text)
        {
            string lang = EClass.core.config.lang;
            if (!langData.ContainsKey(lang)) {
                lang = "EN";
            }
            return langData[lang][text];
        }

        static readonly Dictionary<string, Dictionary<string, string>> langData = new Dictionary<string, Dictionary<string, string>> {
            {
                "CN", new Dictionary<string, string> {
                    { "autoact", "自动行动" },
                    { "settings", "设置行动设置" },
                    { "detRange", "探测范围" },
                    { "digRange", "地面挖掘范围" },
                    { "plowRange", "耕地范围" },
                    { "sameFarmfieldOnly", "只在同一农田上收割" },
                }
            },
            {
                "ZHTW", new Dictionary<string, string> {
                    { "autoact", "自動行動" },
                    { "settings", "自動行動設定" },
                    { "detRange", "探測範圍" },
                    { "digRange", "地面挖掘範圍" },
                    { "plowRange", "耕地範圍" },
                    { "sameFarmfieldOnly", "只在同一農田上收割" },
                }
            },
            {
                "JP", new Dictionary<string, string> {
                    { "autoact", "自動行動" },
                    { "settings", "自動行動設定" },
                    { "detRange", "検出範囲" },
                    { "digRange", "地面掘削範囲" },
                    { "plowRange", "耕す範囲" },
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
                    { "sameFarmfieldOnly", "Harvest On The Same Farmfield Only" },
                }
            }
        };
    }
}