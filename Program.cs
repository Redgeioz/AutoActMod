using System;
using BepInEx;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace AutoAct
{
    [BepInPlugin("redgeioz.plugin.AutoAct", "AutoAct", "1.0.0")]
    public class AutoAct : BaseUnityPlugin
    {
        void Awake()
        {
            string instructionRange = "Value x 2 + 1 = range. If the value is 2, then the range is 5x5.";
            Settings.detRangeSq = base.Config.Bind("Settings", "DetectionRangeSquared", 25, "Sqaure of detection range.");
            Settings.digRange = base.Config.Bind("Settings", "DiggingRange", 2, instructionRange);
            Settings.plowRange = base.Config.Bind("Settings", "PlowingRange", 2, instructionRange);
            Settings.sowRange = base.Config.Bind("Settings", "SowingRange", 0, instructionRange);
            Settings.pourRange = base.Config.Bind("Settings", "PouringRange", 2, instructionRange);
            Settings.pourDepth = base.Config.Bind("Settings", "PouringDepth", 1, "The depth of water pouring");
            Settings.seedReapingCount = base.Config.Bind("Settings", "SeedReapingCount", 25);
            Settings.sameFarmfieldOnly = base.Config.Bind("Settings", "SameFarmfieldOnly", true, "Only auto harvest the plants on the same farmfield.");
            Settings.keyMode = base.Config.Bind("Settings", "KeyMode", false, "false = Press, true = Toggle");
            Settings.keyCode = base.Config.Bind("Settings", "KeyCode", KeyCode.LeftShift);
            new Harmony("AutoAct").PatchAll();
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
                Msg.SetColor(Msg.colors.TalkGod);
                Msg.Say(Lang.GetText(switchOn ? "on" : "off"));
            }
        }

        public static bool active = false;

        public static AIAct autoSetAct;

        public static int targetType = -1;
        public static int targetGrowth = -1;
        public static string targetTypeStr = "";
        public static bool targetCanHarvest = false;

        public static Point startPoint = null;
        public static Point drawWaterPoint = null;

        public static int pourCount = 0;

        public static int originalSeedCount = 0;

        public static int seedId = -1;

        public static HashSet<Point> curtFarmfield = new HashSet<Point>();

        public static bool switchOn = false;

        public static bool IsSwitchOn => Settings.KeyMode ? switchOn : EInput.isShiftDown;


        public static void UpdateState(AIAct a)
        {
            if (!a.owner.IsPC)
            {
                return;
            }

            // Debug.Log($"UpdateState from: {a}");
            if (a == autoSetAct)
            {
                active = true;
                return;
            }

            if (IsSwitchOn)
            {
                active = true;
            }
            else if (a is TaskDrawWater tdw2 && drawWaterPoint != null && tdw2.pos.Equals(drawWaterPoint))
            {
                // active is already true
                return;
            }
            else
            {
                active = false;
                return;
            }

            if (a is TaskDrawWater tdw)
            {
                targetTypeStr = (tdw.pos.HasBridge ? tdw.pos.matBridge : tdw.pos.matFloor).alias;
                drawWaterPoint = tdw.pos.Copy();
                // Debug.Log($"===New start target: {tdw.pos}, floor id: {targetTypeStr}");
                return;
            }
            else
            {
                drawWaterPoint = null;
            }

            if (a is TaskPourWater tpw)
            {
                targetType = tpw.pos.cell.sourceSurface.id;
                startPoint = tpw.pos.Copy();
                pourCount = 0;
                return;
            }

            if (a is TaskDig td)
            {
                targetType = td.pos.cell.sourceSurface.id;
                startPoint = td.pos.Copy();
                // Debug.Log($"===New start target: {td.pos}, floor id: {(int)td.pos.cell._floor} {td.pos.cell.sourceSurface.id}");
                return;
            }

            if (a is TaskPlow tp)
            {
                startPoint = tp.pos;
                return;
            }

            if (!(a is TaskHarvest t))
            {
                return;
            }

            if (t.harvestType == BaseTaskHarvest.HarvestType.Thing)
            {
                targetTypeStr = t.target.Name;
                // Debug.Log($"===New start target: {t.pos}, thing: {t.target.Name}");
            }
            else
            {
                targetType = t.pos.sourceObj.id;
                // Debug.Log($"===New start target: {t.pos}, id: {t.pos.sourceObj.id}, name: {t.pos.sourceObj.name}");
            }
            if (t.pos.growth == null)
            {
                return;
            }

            targetGrowth = t.pos.growth.stage.idx;
            targetCanHarvest = t.pos.growth.CanHarvest();
            curtFarmfield.Clear();
            // Debug.Log($"===New start is mature: {targetGrowth}");
            // Debug.Log($"===New start has block: {t.pos.HasBlock}, has obj: {t.pos.HasObj}");

            if (t.IsReapSeed)
            {
                seedId = t.pos.sourceObj.id;
                originalSeedCount = 0;
                EClass.pc.things.ForEach(thing =>
                {
                    if (thing.trait is TraitSeed seed && seed.row.id == seedId)
                    {
                        originalSeedCount += thing.Num;
                    }
                });
            }
        }

        public static void UpdateStateInstant(TaskBuild a)
        {
            if (!a.owner.IsPC)
            {
                return;
            }

            if (a == autoSetAct)
            {
                active = true;
                return;
            }

            if (IsSwitchOn)
            {
                active = true;
            }
            else
            {
                active = false;
                return;
            }

            startPoint = a.pos.Copy();
            curtFarmfield.Clear();
        }

        public static void SetNextTask(AIAct a)
        {
            EClass.pc.SetAIImmediate(a);
            autoSetAct = a;
        }

        public static void Cancel()
        {
            active = false;
            autoSetAct = null;
        }

        public static void InitFarmfield(Point p, bool isWater)
        {
            Func<Point, bool> IsFarmField;

            if (isWater)
            {
                IsFarmField = pt => pt.IsWater;
            }
            else
            {
                IsFarmField = pt => pt.IsFarmField;
            }

            Point left = new Point(p.x - 1, p.z);
            if (left.IsInBounds && IsFarmField(left))
            {
                if (curtFarmfield.Add(left))
                {
                    InitFarmfield(left, isWater);
                }
            }

            Point right = new Point(p.x + 1, p.z);
            if (right.IsInBounds && IsFarmField(right))
            {
                if (curtFarmfield.Add(right))
                {
                    InitFarmfield(right, isWater);
                }
            }

            Point top = new Point(p.x, p.z + 1);
            if (top.IsInBounds && IsFarmField(top))
            {
                if (curtFarmfield.Add(top))
                {
                    InitFarmfield(top, isWater);
                }
            }

            Point bottom = new Point(p.x, p.z - 1);
            if (bottom.IsInBounds && IsFarmField(bottom))
            {
                if (curtFarmfield.Add(bottom))
                {
                    InitFarmfield(bottom, isWater);
                }
            }
        }
    }

    class Utils
    {
        public static void PrintStackTrace()
        {
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
            foreach (System.Diagnostics.StackFrame frame in stackTrace.GetFrames())
            {
                MethodBase method = frame.GetMethod();
                if (method != null)
                {
                    Console.WriteLine($"{method.DeclaringType?.Name}.{method.Name}");
                }
            }
        }

        public static int Dist2(Point p1, Point p2)
        {
            int dx = p1.x - p2.x;
            int dz = p1.z - p2.z;
            return dx * dx + dz * dz;
        }

        public static int MaxDelta(Point p1, Point p2)
        {
            int dx = Math.Abs(p1.x - p2.x);
            int dz = Math.Abs(p1.z - p2.z);
            return Math.Max(dx, dz);
        }
    }

    [HarmonyPatch(typeof(AIAct), "CanManualCancel")]
    static class CanManualCancel_Patch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            AutoAct.Cancel();
        }
    }

    [HarmonyPatch(typeof(AM_Adv), "TryCancelInteraction")]
    static class TryCancelInteraction_Patch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            AutoAct.Cancel();
        }
    }

    // [HarmonyPatch(typeof(Chara), "SetAI")]
    // static class SetAI_Patch
    // {
    //     [HarmonyPrefix]
    //     static void Prefix(Chara __instance, AIAct g)
    //     {
    //         if (!__instance.IsPC)
    //         {
    //             return;
    //         }
    //         AIAct prev = __instance.ai;
    //         // if (prev is GoalIdle || prev is GoalManualMove || prev is NoGoal)
    //         // {
    //         //     return;
    //         // }
    //         Debug.Log($"===  Set AI  ===");
    //         Debug.Log($"Prev: {prev}, {prev.status}, Next: {g}");
    //         Debug.Log($"==== Set AI ====");
    //         // Utils.PrintStackTrace();
    //     }
    // }
}

