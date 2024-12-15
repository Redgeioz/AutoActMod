using System;
using BepInEx;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace AutoActMod;

[BepInPlugin("redgeioz.plugin.AutoAct", "AutoAct", "1.0.0")]
public class AutoActMod : BaseUnityPlugin
{
    void Awake()
    {
        Settings.startFromCenter = base.Config.Bind("Settings", "StartFromCenter", true);
        Settings.detDistSq = base.Config.Bind("Settings", "DetectionRangeSquared", 25, "Sqaure of detection range.");
        Settings.buildRangeW = base.Config.Bind("Settings", "BuildingRangeW", 5);
        Settings.buildRangeH = base.Config.Bind("Settings", "BuildingRangeH", 5);
        Settings.sowRangeExists = base.Config.Bind("Settings", "SowingRangeExists", false);
        Settings.pourDepth = base.Config.Bind("Settings", "PouringDepth", 1, "The depth of water pouring");
        Settings.seedReapingCount = base.Config.Bind("Settings", "SeedReapingCount", 25);
        Settings.staminaCheck = base.Config.Bind("Settings", "StaminaCheck", true);
        Settings.ignoreEnemySpotted = base.Config.Bind("Settings", "IgnoreEnemySpotted", true);
        Settings.simpleIdentify = base.Config.Bind("Settings", "SimpleIdentify", false);
        Settings.sameFarmfieldOnly = base.Config.Bind("Settings", "SameFarmfieldOnly", true, "Only auto harvest the plants on the same farmfield.");
        Settings.keyMode = base.Config.Bind("Settings", "KeyMode", false, "false = Press, true = Toggle");
        Settings.keyCode = base.Config.Bind("Settings", "KeyCode", KeyCode.LeftShift);
        new Harmony("AutoActMod").PatchAll();
    }

    void Update()
    {
        if (!Settings.KeyMode)
        {
            return;
        }

        if (Input.GetKeyDown(Settings.KeyCode))
        {
            switchOn = !switchOn;
            Say(ALang.GetText(switchOn ? "on" : "off"));
        }
    }

    public static bool active = false;
    public static bool switchOn = false;
    public static bool IsSwitchOn => Settings.KeyMode ? switchOn : Input.GetKey(Settings.KeyCode);
    public static Point lastHitPoint = Point.Zero;
    public static void Say(string text)
    {
        Msg.SetColor(Msg.colors.TalkGod);
        Msg.Say(text);
    }
}

public static class Utils
{
    public static void PrintStackTrace()
    {
        var stackTrace = new System.Diagnostics.StackTrace(true);
        foreach (System.Diagnostics.StackFrame frame in stackTrace.GetFrames())
        {
            var method = frame.GetMethod();
            if (method.HasValue())
            {
                Console.WriteLine($"{method.DeclaringType?.Name}.{method.Name}");
            }
        }
    }

    public static int Dist2(Point p1, Point p2)
    {
        var dx = p1.x - p2.x;
        var dz = p1.z - p2.z;
        return dx * dx + dz * dz;
    }

    public static int MaxDelta(Point p1, Point p2)
    {
        var dx = Math.Abs(p1.x - p2.x);
        var dz = Math.Abs(p1.z - p2.z);
        return Math.Max(dx, dz);
    }

    public static int GetBit(this int n, int digit) => (n >> digit) & 1;

    public static bool HasValue(this object obj) => obj != null;
}