using System;
using BepInEx;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;

namespace AutoAct
{
    [BepInPlugin("redgeioz.plugin.AutoAct", "AutoAct", "1.0.0")]
    public class AutoAct : BaseUnityPlugin
    {
        void Awake()
        {
            Settings.detRangeSq = base.Config.Bind("Settings", "DetectionRangeSquared", 25, "Sqaure of detection range.");
            Settings.digRange = base.Config.Bind("Settings", "DiggingRange", 2, "Value x 2 + 1 = range. If the value is 2, then the range is 5x5.");
            Settings.plowRange = base.Config.Bind("Settings", "PlowingRange", 2, "Value x 2 + 1 = range. If the value is 2, then the range is 5x5.");
            Settings.sameFarmfieldOnly = base.Config.Bind("Settings", "SameFarmfieldOnly", true, "Only auto harvest the plants on the same farmfield.");
            new Harmony("AutoAct").PatchAll();
        }

        public static void UpdateState(AIAct a)
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

            if (EInput.isShiftDown)
            {
                active = true;
            }
            else
            {
                active = false;
                return;
            }

            if (a is TaskPlow)
            {
                startPoint = (a as TaskPlow).pos;
                return;
            }

            if (!(a is BaseTaskHarvest))
            {
                return;
            }

            BaseTaskHarvest t = a as BaseTaskHarvest;

            if (a is TaskDig)
            {
                targetType = t.pos.cell._floor;
                startPoint = t.pos.Copy();
                // Debug.Log($"===New start target: {t.pos}, floor id: {(int)t.pos.cell._floor}");
                return;
            }

            if (t.harvestType == BaseTaskHarvest.HarvestType.Thing)
            {
                targetThingType = t.target.Name;
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

            if (EInput.isShiftDown)
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

        public static bool active = false;

        public static AIAct autoSetAct;

        public static int targetType = -1;
        public static int targetGrowth = -1;
        public static string targetThingType = "";
        public static bool targetCanHarvest = false;

        public static Point startPoint = null;

        public static HashSet<Point> curtFarmfield = new HashSet<Point>();

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
    //         Utils.PrintStackTrace();
    //     }
    // }
}

