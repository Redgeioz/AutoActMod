using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AutoActMod.Actions;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace AutoActMod;

[BepInPlugin("redgeioz.plugin.AutoAct", "AutoAct", "1.0.0")]
public class AutoActMod : BaseUnityPlugin
{
    internal void Awake()
    {
        Instance = this;
        Settings.detDistSq = Config.Bind("Settings", "DetectionRangeSquared", 100, "Sqaure of detection range.");
        Settings.pourDepth = Config.Bind("Settings", "PouringDepth", 1, "The depth of water pouring");
        Settings.seedReapingCount = Config.Bind("Settings", "SeedReapingCount", 25);
        Settings.staminaCheck = Config.Bind("Settings", "StaminaCheck", true);
        Settings.enemyEncounterResponse = Config.Bind("Settings", "enemyEncounterResponse", 2);
        Settings.simpleIdentify = Config.Bind("Settings", "SimpleIdentify", 0);
        Settings.sameFarmfieldOnly = Config.Bind("Settings", "SameFarmfieldOnly", true, "Only auto harvest the plants on the same farmfield.");
        Settings.keyMode = Config.Bind("Settings", "KeyMode", false, "false = Press, true = Toggle");
        Settings.keyCode = Config.Bind("Settings", "KeyCode", KeyCode.LeftShift);
        Settings.rangeSelectKeyCode = Config.Bind("Settings", "RangeSelectKeyCode", KeyCode.LeftAlt);

        AutoAct.Register(Assembly.GetExecutingAssembly());

        new Harmony("AutoActMod").PatchAll();
    }

    internal void Update()
    {
        if (!Settings.KeyMode)
        {
            return;
        }

        if (Input.GetKeyDown(Settings.KeyCode))
        {
            SwitchOn = !SwitchOn;
            Say(AALang.GetText(SwitchOn ? "aaon" : "aaoff"));
        }
    }

    internal void Start()
    {
        AutoAct.InitTryCreateMethods();
    }

    public static void Say(string text)
    {
        Msg.SetColor(Msg.colors.TalkGod);
        Msg.Say(text);
    }

    internal static void Log(object payload)
    {
#if DEBUG
        Instance.Logger.LogMessage(payload);
#else
        Instance.Logger.LogInfo(payload);
#endif
    }

    internal static void LogWarning(object payload)
    {
        Instance.Logger.LogWarning(payload);
    }

    public static bool Active => EClass.pc.ai is AutoAct;
    public static bool SwitchOn = false;
    public static bool IsSwitchOn => Settings.KeyMode ? SwitchOn : Input.GetKey(Settings.KeyCode);
    public static AutoActMod Instance { get; private set; }
}

public static class Utils
{
    public static void Trace()
    {
        var stackTrace = new System.Diagnostics.StackTrace(true);
        AutoActMod.Log($"StackTrace:");
        foreach (var frame in stackTrace.GetFrames())
        {
            var method = frame.GetMethod();
            if (method.HasValue())
            {
                AutoActMod.Log($"\t{method.DeclaringType?.Name}.{method}");
            }
        }
    }

    public static int Dist2(this Point p1, Point p2)
    {
        var dx = p1.x - p2.x;
        var dz = p1.z - p2.z;
        return dx * dx + dz * dz;
    }

    public static int MaxDelta(this Point p1, Point p2)
    {
        var dx = Math.Abs(p1.x - p2.x);
        var dz = Math.Abs(p1.z - p2.z);
        return Math.Max(dx, dz);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBit(this int n, int digit) => (n >> digit) & 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasValue(this object obj) => obj != null;

    public static IEnumerable<Thing> Flatten(this ThingContainer things)
    {
        foreach (var t1 in things)
        {
            if (t1.things.Count == 0)
            {
                yield return t1;
                continue;
            }

            foreach (var t2 in t1.things)
            {
                yield return t2;
            }
        }
    }
}