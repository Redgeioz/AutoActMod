using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace AutoAct
{
	[HarmonyPatch(typeof(TaskBuild), "OnDestroy")]
	static class TaskBuild_OnDestroy_Patch
	{
		[HarmonyPostfix]
		static void Postfix(TaskBuild __instance)
		{
			AutoAct.UpdateStateInstant(__instance);

			Card held = EClass.pc.held;
			if (!AutoAct.active || AutoAct.held != held)
			{
				return;
			}

			// Debug.Log($"Try continuing {__instance}, status {__instance.status}");
			if (__instance.status != AIAct.Status.Success)
			{
				return;
			}

			if (held.category.id == "seed")
			{
				ContinueBuild(p => !p.HasThing && !p.HasBlock && !p.HasObj && p.growth == null && p.Installed == null, Settings.SowRangeExists);
			}
			else if (held.category.id == "fertilizer")
			{
				ContinueBuild(ShouldFertilize, false);
			}
			else if (held.category.id == "floor")
			{
				ContinueBuild(p => !p.HasThing && !p.HasBlock && !p.HasObj && p.cell.sourceSurface != AutoAct.startPoint.cell.sourceSurface);
			}
			else
			{
				ContinueBuild(p => !p.HasThing && !p.HasBlock && !p.HasObj, true, true);
			}
		}

		static void ContinueBuild(Func<Point, bool> filter, bool hasRange = true, bool edgeOnly = false)
		{
			Point targetPoint = GetNextTarget(filter, hasRange, edgeOnly);
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
			AutoAct.curtField.Remove(targetPoint);
		}

		static Point GetNextTarget(Func<Point, bool> filter, bool hasRange = true, bool edgeOnly = false)
		{
			List<(Point, int, int, int)> list = new List<(Point, int, int, int)>();
			foreach (Point p in AutoAct.curtField)
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

				int dist2 = Utils.Dist2(EClass.pc.pos, p);
				int dist2ToLastPoint = 0;
				PathProgress path = EClass.pc.path;
				if (Settings.StartFromCenter)
				{
					int max = AutoAct.MaxDeltaToStartPoint(p);
					if (hasRange && max > Settings.BuildRangeW / 2)
					{
						continue;
					}

					if (edgeOnly && max != Settings.BuildRangeW / 2)
					{
						continue;
					}

					dist2ToLastPoint = Utils.Dist2((AutoAct.runningTask as TaskPoint).pos, p);
					if (max <= 1)
					{
						list.Add((p, max, max - 1, dist2ToLastPoint));
						continue;
					}

					path.RequestPathImmediate(EClass.pc.pos, p, 1, true, -1);
					if (path.state == PathProgress.State.Fail)
					{
						continue;
					}

					list.Add((p, max, path.nodes.Count, dist2ToLastPoint));
					continue;
				}

				(int d1, int d2) = AutoAct.GetDelta(p);
				if (hasRange && (d1 < 0 || d2 < 0 || d1 >= Settings.BuildRangeH || d2 >= Settings.BuildRangeW))
				{
					continue;
				}

				if (dist2 > 2)
				{
					path.RequestPathImmediate(EClass.pc.pos, p, 1, true, -1);
					if (path.state == PathProgress.State.Fail)
					{
						continue;
					}
				}

				if (edgeOnly)
				{
					if (d1 == 0)
					{
						// nothing
					}
					else if (d2 == Settings.BuildRangeW - 1)
					{
						d2 = d1;
						d1 = 1;
					}
					else if (d1 == Settings.BuildRangeH - 1)
					{
						d2 = -d2;
						d1 = 2;
					}
					else if (d2 == 0)
					{
						d2 = -d1;
						d1 = 3;
					}
					else
					{
						continue;
					}

					list.Add((p, d1, d2, 0));
				}

				if (d1 >= 0)
				{
					// hasRange == false
					(d1, d2) = AutoAct.GetDelta(p, EClass.pc.pos, AutoAct.startDirection);
					if (d1 < 0)
					{
						d1 = -d1 * 2;
					}
				}

				dist2ToLastPoint = Utils.Dist2((AutoAct.runningTask as TaskPoint).pos, p);
				list.Add((p, dist2ToLastPoint, d1, d2));
			}

			(Point targetPoint, int tdl, int td1, int td2) = list
				.OrderBy(tuple => tuple.Item2)
				.ThenBy(tuple => tuple.Item3)
				.ThenBy(tuple => tuple.Item4)
				.FirstOrDefault();
			// if (targetPoint != null)
			// {
			// 	Debug.Log($"targetPoint: {targetPoint} | {tdl} | d1: {td1} | d2: {td2} | {AutoAct.startPoint}");
			// }
			return targetPoint;
		}

		static bool ShouldFertilize(Point p)
		{
			bool hasPlant = p.growth != null;
			if (!p.HasThing)
			{
				return hasPlant;
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

			return (seed || hasPlant) && !fert;
		}
	}
}