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
                Msg.Say(ALang.GetText(switchOn ? "on" : "off"));
            }
        }

        public static bool active = false;
        public static AIAct autoSetAct;
        public static bool retry = false;
        public static bool backToHarvest = false;

        public static int targetType = -1;
        public static int targetGrowth = -1;
        public static string targetTypeStr = "";
        public static bool targetCanHarvest = false;
        public static int startDirection = 0;
        public static Point startPoint = null;
        // Last water drawing point
        public static Point drawWaterPoint = null;
        public static int pourCount = 0;

        public static int originalSeedCount = 0;
        public static int seedId = -1;

        public static Card held = null;

        public static HashSet<Point> curtField = new HashSet<Point>();

        public static bool switchOn = false;
        public static bool IsSwitchOn => Settings.KeyMode ? switchOn : EInput.isShiftDown;


        public static void UpdateState(AIAct a)
        {
            if (!a.owner.IsPC)
            {
                return;
            }

            if (Settings.StaminaCheck && EClass.pc.stamina.value <= 0)
            {
                active = false;
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

            SayStart();

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
                SetTarget(tpw.pos.cell.sourceSurface);
                SetStartPoint(tpw.pos.Copy());
                pourCount = 0;
                return;
            }

            if (a is TaskDig td)
            {
                SetTarget(td.pos.cell.sourceSurface);
                SetStartPoint(td.pos.Copy());
                // Debug.Log($"===New start target: {td.pos}, floor id: {(int)td.pos.cell._floor} {td.pos.cell.sourceSurface.id}");
                return;
            }

            if (a is TaskPlow tp)
            {
                SetStartPoint(tp.pos.Copy());
                return;
            }

            if (!(a is BaseTaskHarvest t))
            {
                return;
            }

            if (t.harvestType == BaseTaskHarvest.HarvestType.Thing)
            {
                targetTypeStr = t.target.Name;
                // Debug.Log($"===New start target: {t.pos}, thing: {t.target.Name}");
            }
            else if ((!t.pos.HasObj || Settings.SimpleIdentify) && t.pos.HasBlock)
            {
                if (Settings.SimpleIdentify)
                {
                    targetType = -1;
                    backToHarvest = true;
                    return;
                }
                else
                {
                    SetTarget(t.pos.sourceBlock);
                }
                // Debug.Log($"===New start target: {t.pos}, block id: {t.pos.sourceBlock.id}, name: {t.pos.sourceBlock.name}");
            }
            else if (t.pos.HasObj)
            {
                if (Settings.SimpleIdentify && t.pos.sourceObj.HasGrowth)
                {
                    targetType = -2;
                    return;
                }
                else
                {
                    SetTarget(t.pos.sourceObj);
                }
                // Debug.Log($"===New start target: {t.pos}, obj id: {t.pos.sourceObj.id}, name: {t.pos.sourceObj.name}");
                // Debug.Log($"===New start target: {t.pos}, block id: {t.pos.sourceBlock.id}, name: {t.pos.sourceBlock.name}");
            }

            // Debug.Log($"===New start has block: {t.pos.HasBlock}, has obj: {t.pos.HasObj}");
            if (t.pos.growth == null)
            {
                return;
            }

            targetGrowth = t.pos.growth.stage.idx;
            targetCanHarvest = t.pos.growth.CanHarvest();
            curtField.Clear();
            // Debug.Log($"===New start is mature: {targetGrowth}");

            if (t is TaskHarvest th && th.IsReapSeed)
            {
                seedId = th.pos.sourceObj.id;
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

            SetStartPoint(a.pos.Copy());
            curtField.Clear();

            Card held = EClass.pc.held;
            if (held == null || held.Num == 1)
            {
                active = false;
                return;
            }

            AutoAct.held = held;

            if (held.category.id == "seed" || held.category.id == "fertilizer")
            {
                InitFarmfield(startPoint, startPoint.IsWater);
            }
            // else if (held.category.id == "floor")
            // {
            //     InitField(startPoint, p => !p.HasBlock);
            // }
            else
            {
                InitField(startPoint, p => !p.HasBlock);
            }
        }

        public static void SetNextTask(AIAct a)
        {
            EClass.pc.SetAIImmediate(a);
            autoSetAct = a;
            if (a is BaseTaskHarvest t)
            {
                t.SetTarget(EClass.pc);
            }
        }

        public static void SetTarget(TileRow r)
        {
            int id = r.id;
            if (id == 167)
            {
                id = 1;
            }
            targetType = id;
            targetTypeStr = r.name;
            retry = true;
        }

        public static bool IsTarget(TileRow r)
        {
            if (targetType == -1)
            {
                return r is SourceBlock.Row || (r is SourceObj.Row obj && obj.tileType.IsBlockMount);
            }
            else if (targetType == -2)
            {
                return r is SourceObj.Row obj && obj.HasGrowth;
            }

            int id = r.id;
            if (id == 167)
            {
                id = 1;
            }

            return id == targetType;
        }

        public static void SetStartPoint(Point p)
        {
            startPoint = p;
            int dx = p.x - EClass.pc.pos.x;
            int dz = p.z - EClass.pc.pos.z;

            if ((dz == -1 || dz == 0) && dx == -1)
            {
                startDirection = 3;
            }
            else if ((dx == -1 || dx == 0) && dz == 1)
            {
                startDirection = 2;
            }
            else if ((dz == 1 || dz == 0) && dx == 1)
            {
                startDirection = 1;
            }
            else if ((dx == 0 || dx == 1) && dz == -1)
            {
                startDirection = 0;
            }
            else
            {
                // dx == 0, dy == 0
                // | 0 ↓ | 1 → | 2 ↑ | 3 ← |
                startDirection = EClass.pc.dir;
            }
        }

        public static (int, int) GetDelta(Point p)
        {
            return GetDelta(p, startPoint, startDirection);
        }

        public static (int, int) GetDelta(Point p, Point refPoint, int dir)
        {
            int dx = p.x - refPoint.x;
            int dz = p.z - refPoint.z;

            int d1 = 0, d2 = 0;
            switch (dir)
            {
                case 0:
                    d1 = dz * -1;
                    d2 = dx * -1;
                    break;
                case 1:
                    d1 = dx;
                    d2 = dz * -1;
                    break;
                case 2:
                    d1 = dz;
                    d2 = dx;
                    break;
                case 3:
                    d1 = dx * -1;
                    d2 = dz;
                    break;
            }
            return (d1, d2);
        }

        public static int MaxDeltaToStartPoint(Point p)
        {
            return Utils.MaxDelta(p, startPoint);
        }

        public static void Cancel()
        {
            active = false;
            autoSetAct = null;
            SayFail();
        }

        public static void InitFarmfield(Point p, bool isWater)
        {
            Func<Point, bool> filter;

            if (isWater)
            {
                filter = pt => pt.IsWater;
            }
            else
            {
                filter = pt => pt.IsFarmField;
            }

            InitField(p, filter);
            curtField.Remove(p);
        }

        public static void InitField(Point p, Func<Point, bool> filter, int dir = 0b1111)
        {
            if ((dir & 0b0001) == 0b0001)
            {
                Point left = new Point(p.x - 1, p.z);
                if (left.IsInBounds && filter(left) && curtField.Add(left))
                {
                    InitField(left, filter, 0b1101);
                    // InitField(left, filter, 0b1101 & dir);
                }
            }

            if ((dir & 0b0010) == 0b0010)
            {
                Point right = new Point(p.x + 1, p.z);
                if (right.IsInBounds && filter(right) && curtField.Add(right))
                {
                    InitField(right, filter, 0b1110);
                    // InitField(right, filter, 0b1110 & dir);
                }
            }

            if ((dir & 0b0100) == 0b0100)
            {
                Point front = new Point(p.x, p.z - 1);
                if (front.IsInBounds && filter(front) && curtField.Add(front))
                {
                    InitField(front, filter, 0b0111);
                    // InitField(front, filter, 0b0111 & dir);
                }
            }

            if ((dir & 0b1000) == 0b1000)
            {
                Point back = new Point(p.x, p.z + 1);
                if (back.IsInBounds && filter(back) && curtField.Add(back))
                {
                    InitField(back, filter, 0b1011);
                    // InitField(back, filter, 0b1011 & dir);
                }
            }
        }

        public static void SayStart()
        {
            Msg.SetColor(Msg.colors.TalkGod);
            Msg.Say(ALang.GetText("start"));
        }

        public static void SayNoTarget()
        {
            Msg.SetColor(Msg.colors.TalkGod);
            Msg.Say(ALang.GetText("noTarget"));
        }

        public static void SayFail()
        {
            Msg.SetColor(Msg.colors.TalkGod);
            Msg.Say(ALang.GetText("fail"));
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

        public static void ForEachNeighborPoint(Point center, Action<Point> forEach)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Point p = new Point(center.x + i - 1, center.z + j - 1);
                    if (!p.Equals(center) && p.IsInBounds)
                    {
                        forEach(p);
                    }
                }
            }
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

    [HarmonyPatch(typeof(AIAct), "Cancel")]
    static class AIAct_Cancel_Patch
    {
        [HarmonyPrefix]
        static void Prefix(AIAct __instance)
        {
            // Debug.Log($"==Start Cancel {__instance} =============");
            // Utils.PrintStackTrace();
            // if (__instance is AI_Goto gt)
            // {
            //     Debug.Log($"AI_Goto: from {gt.owner.pos} to {gt.dest}, {gt.destDist}");
            //     Debug.Log($"{EClass.pc.path.state} | {EClass.pc.path.nodes.Count}");
            // }
            // Debug.Log($"===========End {__instance} =============");
        }

        [HarmonyPostfix]
        static void Postfix(AIAct __instance)
        {
            if (__instance != AutoAct.autoSetAct)
            {
                return;
            }
            // Retries are mainly used to deal with random pathfinding failures (they
            // do happen sometimes even if the player is able to get there) or animal
            // movement during shearing.
            if (AutoAct.retry)
            {
                AutoAct.retry = false;
                AutoAct.autoSetAct.Reset();
                AutoAct.SetNextTask(AutoAct.autoSetAct);
                return;
            }
            else
            {
                AutoAct.SayFail();
            }
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

    // [HarmonyPatch(typeof(Task), "OnDestroy")]
    // static class Task_OnDestroy_Patch
    // {
    //     [HarmonyPrefix]
    //     static void Prefix(Task __instance)
    //     {
    //         Debug.Log($"==Start Cancel {__instance} =============");
    //         bool f1 = !__instance.CanPerform();
    //         bool f2 = !EInput.rightMouse.pressing;
    //         bool f3 = __instance.HasProgress && !__instance.CanProgress();
    //         Debug.Log($"{f1}, {f2}, {f3}");
    //         Utils.PrintStackTrace();
    //         Debug.Log($"===========End {__instance} =============");
    //     }
    // }
}

