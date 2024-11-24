using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace AutoAct
{
	[HarmonyPatch(typeof(TaskBuild), "OnProgressComplete")]
	static class TaskBuild_OnProgressComplete_Patch
	{
		[HarmonyPostfix]
		static void Postfix(TaskBuild __instance)
		{
			AutoAct.UpdateStateInstant(__instance);

			if (!AutoAct.active || EClass.pc.held == null)
			{
				return;
			}

			Card held = EClass.pc.held;
			Point lastPoint = __instance.pos;
			if (!AutoAct.curtFarmfield.Contains(lastPoint))
			{
				AutoAct.InitFarmfield(lastPoint, lastPoint.IsWater);
			}

			if (held.category.id == "seed")
			{
				ContinueBuild(p => !p.HasThing && !p.HasBlock && !p.HasObj && p.growth == null);
			}
			else if (held.category.id == "fertilizer")
			{
				ContinueBuild(ShouldFertilize);
			}
		}

		static void ContinueBuild(Func<Point, bool> filter)
		{
			Point targetPoint = GetNextTarget(filter);
			if (targetPoint == null)
			{
				return;
			}

			TaskBuild task = new TaskBuild
			{
				recipe = HotItemHeld.recipe,
				held = EClass.pc.held,
				pos = targetPoint,
			};

			AutoAct.SetNextTask(task);
		}

		static Point GetNextTarget(Func<Point, bool> filter)
		{
			List<(Point, int, int, int)> list = new List<(Point, int, int, int)>();
			foreach (Point p in AutoAct.curtFarmfield)
			{
				if (!filter(p))
				{
					continue;
				}

				if (AutoAct.startPoint == null)
				{
					Debug.LogWarning("AutoAct startPoint: null");
					break;
				}

				int dx = Math.Abs(AutoAct.startPoint.x - p.x);
				int dz = Math.Abs(AutoAct.startPoint.z - p.z);
				int max = Math.Max(dx, dz);

				int dist2 = Utils.Dist2(EClass.pc.pos, p);
				if (dist2 <= 2)
				{
					list.Add((p, max, dist2 - 2, dist2));
					continue;
				}

				PathProgress path = EClass.pc.path;
				path.RequestPathImmediate(EClass.pc.pos, p, 1, false, -1);
				if (path.state == PathProgress.State.Fail)
				{
					continue;
				}

				list.Add((p, max, path.nodes.Count, dist2));
			}

			(Point targetPoint, int _, int _, int _) = list
				.OrderBy(tuple => tuple.Item2)
				.ThenBy(tuple => tuple.Item3)
				.ThenBy(tuple => tuple.Item4)
				.FirstOrDefault();
			return targetPoint;
		}

		static bool ShouldFertilize(Point p)
		{
			if (p.HasBlock)
			{
				return false;
			}

			if (!p.HasThing)
			{
				return p.growth != null;
			}

			bool fert = false;
			bool seed = false;
			p.Things.ForEach(t =>
			{
				if (t.trait is TraitFertilizer)
				{
					fert = true;
				}
				else if (t.trait is TraitSeed)
				{
					seed = true;
				}
			});

			return (seed || p.growth != null) && !fert;
		}
	}
}