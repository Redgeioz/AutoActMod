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
    public static ConfigEntry<int> enemyEncounterResponse;
    public static ConfigEntry<int> simpleIdentify;
    public static ConfigEntry<bool> startFromCenter;
    public static ConfigEntry<bool> keyMode;

    public static ConfigEntry<KeyCode> keyCode;
    public static ConfigEntry<KeyCode> rangeSelectKeyCode;

    public static int DetRangeSq
    {
        get { return detDistSq.Value; }
        set { detDistSq.Value = value; }
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

    public static int EnemyEncounterResponse
    {
        get { return enemyEncounterResponse.Value; }
        set { enemyEncounterResponse.Value = value; }
    }

    public static int SimpleIdentify
    {
        get { return simpleIdentify.Value; }
        set { simpleIdentify.Value = value; }
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

    public static KeyCode RangeSelectKeyCode
    {
        get { return rangeSelectKeyCode.Value; }
        set { rangeSelectKeyCode.Value = value; }
    }

    public static void SetupSettings(ActPlan actPlan)
    {
        var text = AALang.GetText("settings");
        var dynamicAct = new DynamicAct(text, () =>
        {
            var list = new UIContextMenuItem[2];
            var menu = EClass.ui.CreateContextMenu();
            menu.AddToggle(AALang.GetText("sameFarmfieldOnly"), SameFarmfieldOnly, v => SameFarmfieldOnly = v);
            menu.AddToggle(AALang.GetText("staminaCheck"), StaminaCheck, v => StaminaCheck = v);
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
                },
                KeyMode ? 1 : 0,
                v => { },
                0,
                1,
                true,
                false
            );
            menu.AddSlider(
                AALang.GetText("enemyEncounterResponse"),
                v =>
                {
                    EnemyEncounterResponse = (int)v;
                    string s = "eer" + v;
                    return AALang.GetText(s);
                },
                EnemyEncounterResponse,
                v => { },
                0,
                2,
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
            menu.AddSlider(
                AALang.GetText("simpleIdentify"),
                v =>
                {
                    SimpleIdentify = (int)v;
                    if (SimpleIdentify == 0)
                    {
                        return AALang.GetText("off");
                    }
                    else
                    {
                        return v.ToString();
                    }
                },
                SimpleIdentify,
                v => { },
                0,
                2,
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
        }, false);
        actPlan.list.Add(new ActPlan.Item { act = dynamicAct });
    }
}

public static class AALang
{
    static public string GetText(string text)
    {
        var lang = EClass.core.config.lang;
        if (!langData.ContainsKey(lang))
        {
            lang = "EN";
        }
        return langData[lang][text];
    }

    public static Dictionary<string, Dictionary<string, string>> langData = new()
    {
        {
            "CN", new Dictionary<string, string> {
                { "autoact", "自动行动" },
                { "settings", "自动行动设置" },
                { "enemyEncounterResponse", "遇敌时反应" },
                { "eer0", "停下" },
                { "eer1", "无视" },
                { "eer2", "攻击" },
                { "detDist", "探测距离" },
                { "pourDepth", "倒水深度" },
                { "seedReapingCount", "种子收获数" },
                { "keyMode", "按键模式" },
                { "press", "按住" },
                { "toggle", "切换" },
                { "start", "自动行动，启动！"},
                { "fail", "自动行动已中断。"},
                { "noTarget", "自动行动没有找到下一个目标。"},
                { "aaon", "自动行动，启动！"},
                { "aaoff", "自动行动，关闭。"},
                { "staminaCheck", "精力耗尽时停止　　　　　　　　" },
                { "weightCheck", "超重时停止　　　　　　　　　　" },
                { "entireFarmfield", "所选的整个田地" },
                { "followBuildRange", "同建造范围" },
                { "simpleIdentify", "简单识别" },
                { "off", "关闭"},
                { "startFromCenter", "从中心开始（限制为正方形）　　" },
                { "sameFarmfieldOnly", "只在同一田地上收割　　　　　　" },
            }
        },
        {
            "ZHTW", new Dictionary<string, string> {
                { "autoact", "自動行動" },
                { "settings", "自動行動設定" },
                { "enemyEncounterResponse", "遇敵時反應" },
                { "eer0", "停下" },
                { "eer1", "無視" },
                { "eer2", "攻擊" },
                { "detDist", "探測距離" },
                { "pourDepth", "倒水深度" },
                { "seedReapingCount", "種子收獲數" },
                { "keyMode", "按鍵模式" },
                { "press", "按住" },
                { "toggle", "切換" },
                { "start", "自動行動，啟動！"},
                { "fail", "自動行動已中斷。"},
                { "noTarget", "自動行動沒有找到下一個目標。"},
                { "aaon", "自動行動，啟動！"},
                { "aaoff", "自動行動，關閉。"},
                { "staminaCheck", "精力耗盡時停止　　　　　　　　" },
                { "entireFarmfield", "所選的整個田地" },
                { "followBuildRange", "同建造範圍" },
                { "simpleIdentify", "簡單識別" },
                { "off", "關閉"},
                { "startFromCenter", "从中心開始（限製為正方形）　　" },
                { "sameFarmfieldOnly", "只在同一田地上收割　　　　　　" },
            }
        },
        {
            "JP", new Dictionary<string, string> {
                { "autoact", "自動行動" },
                { "settings", "自動行動設定" },
                { "enemyEncounterResponse", "敵遭遇時の対応" },
                { "eer0", "停止" },
                { "eer1", "無視" },
                { "eer2", "攻擊" },
                { "detDist", "検出距離" },
                { "pourDepth", "注水深さ" },
                { "seedReapingCount", "種子収穫数" },
                { "keyMode", "キーモード" },
                { "press", "押す" },
                { "toggle", "切り替え" },
                { "start", "自動行動開始済み。"},
                { "fail", "自動行動中断済み。"},
                { "noTarget", "自動行動は次目標を発見できず。"},
                { "aaon", "自動行動：オン。"},
                { "aaoff", "自動行動：オフ。"},
                { "staminaCheck", "精力が尽きた時に停止する　　　" },
                { "entireFarmfield", "選択された農地全体" },
                { "followBuildRange", "建設範囲と同じ" },
                { "simpleIdentify", "簡単識別" },
                { "off", "オフ"},
                { "startFromCenter", "中心から開始（正方形に制限）　" },
                { "sameFarmfieldOnly", "同じ農地での収穫のみ　　　　　" },
            }
        },
        {
            "EN", new Dictionary<string, string> {
                { "autoact", "Auto Act" },
                { "settings", "Auto Act Settings" },
                { "enemyEncounterResponse", "Enemy Encounter Response" },
                { "eer0", "Stop" },
                { "eer1", "Ignore" },
                { "eer2", "Attack" },
                { "detDist", "Detection Distance" },
                { "pourDepth", "Pouring Depth" },
                { "seedReapingCount", "Count For Seed Reaping" },
                { "keyMode", "Key Mode" },
                { "press", "Press" },
                { "toggle", "Toggle" },
                { "start", "Auto Act started."},
                { "fail", "Auto Act was interrupted."},
                { "noTarget", "Auto Act could not find the next target."},
                { "aaon", "Auto Act: On."},
                { "aaoff", "Auto Act: Off."},
                { "staminaCheck", "Stop When Stamina Runs Out　 　　　　　　　　　　　　" },
                { "entireFarmfield", "The Entire Selected Farmfield" },
                { "followBuildRange", "Follow The Building Range" },
                { "simpleIdentify", "Simple Identification" },
                { "off", "Off"},
                { "startFromCenter", "Start From The Center (Square Only)　　　　 　　　　" },
                { "sameFarmfieldOnly", "Harvest On The Same Farmfield Only　　　　　　　　" },
            }
        },
        {
            "PTBR", new Dictionary<string, string> {
                { "autoact", "Ação Automática" },
                { "settings", "Configurações de Ação Automática" },
                { "enemyEncounterResponse", "Resposta ao Encontro com Inimigos" },
                { "eer0", "Parar" },
                { "eer1", "Ignorar" },
                { "eer2", "Atacar" },
                { "detDist", "Distância de Detecção" },
                { "pourDepth", "Profundidade de Derramamento" },
                { "seedReapingCount", "Quantidade para Colheita de Sementes" },
                { "keyMode", "Modo de Tecla" },
                { "press", "Pressionar" },
                { "toggle", "Alternar" },
                { "start", "Ação Automática iniciada." },
                { "fail", "Ação Automática foi interrompida." },
                { "noTarget", "Ação Automática não encontrou o próximo alvo." },
                { "aaon", "Ação Automática: Ligada." },
                { "aaoff", "Ação Automática: Desligada." },
                { "staminaCheck", "Parar Quando a Estamina Acabar" },
                { "entireFarmfield", "Toda a Área da Fazenda Selecionada" },
                { "followBuildRange", "Seguir a Área de Construção" },
                { "simpleIdentify", "Identificação Simples" },
                { "off", "Desligado" },
                { "startFromCenter", "Começar pelo Centro (Apenas Quadrados)" },
                { "sameFarmfieldOnly", "Colher Apenas na Mesma Área da Fazenda" },
            }
        }
    };
}
