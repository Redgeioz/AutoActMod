using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoAct;

static class OnTaskBuildComplete
{
	public static void Run(AIAct __instance, AIAct.Status __result)
	{
		Card held = EClass.pc.held;
		if (!AutoAct.active || __instance != AutoAct.runningTask || AutoAct.held != held || __result != AIAct.Status.Success)
		{
			return;
		}

		PointSetter.Reset();

		if (held.category.id == "seed")
		{
			ContinueBuild(p => !p.HasThing && (!p.HasBlock || p.HasWallOrFence) && !p.HasObj && p.growth == null && p.Installed == null, Settings.SowRangeExists);
		}
		else if (held.category.id == "fertilizer")
		{
			ContinueBuild(ShouldFertilize, false);
		}
		else if (held.category.id == "floor" || held.category.id == "foundation")
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

			int dist2 = Utils.Dist2(EClass.pc.pos, p);
			int dist2ToLastPoint = Utils.Dist2((AutoAct.runningTask as TaskPoint).pos, p);
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

				if (max <= 1)
				{
					PointSetter.TrySet(p, max, max - 1, dist2ToLastPoint);
					continue;
				}

				list.Add((p, max, dist2, dist2ToLastPoint));
			}
			else
			{
				list.Add((p, 0, dist2, dist2ToLastPoint));
			}
		}

		foreach (var item in list.OrderBy(tuple => tuple.Item2).ThenBy(tuple => tuple.Item3))
		{
			(Point p, int max, int dist2, int dist2ToLastPoint) = item;
			if (PointSetter.FinalPoint != null &&
				((Settings.StartFromCenter && max > PointSetter.Factor) ||
				(!Settings.StartFromCenter && !edgeOnly && dist2ToLastPoint > PointSetter.Factor)))
			{
				break;
			}

			PathProgress path = EClass.pc.path;
			if (Settings.StartFromCenter)
			{
				path.RequestPathImmediate(EClass.pc.pos, p, 1, true, -1);
				if (path.state == PathProgress.State.Fail)
				{
					continue;
				}

				PointSetter.TrySet(p, max, path.nodes.Count, dist2ToLastPoint);
			}
			else
			{
				(int d1, int d2) = AutoAct.GetDelta(p);
				if (hasRange && (d1 < 0 || d2 < 0 || d1 >= Settings.BuildRangeH || d2 >= Settings.BuildRangeW))
				{
					continue;
				}

				if (edgeOnly)
				{
					int f1 = d1 == 0 ? 1 : 0;
					int f2 = d2 == Settings.BuildRangeW - 1 ? 1 : 0;
					int f3 = d1 == Settings.BuildRangeH - 1 ? 1 : 0;
					int f4 = d2 == 0 ? 1 : 0;
					if (f1 == 1)
					{
						// nothing
					}
					else if (f2 == 1)
					{
						d2 = d1;
						d1 = 1;
					}
					else if (f3 == 1)
					{
						d2 = -d2;
						d1 = 2;
					}
					else if (f4 == 1)
					{
						d2 = -d1;
						d1 = 3;
					}
					else
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

					if (!HotItemHeld.taskBuild.recipe.IsWallOrFence)
					{
						PointSetter.TrySet(p, d1, d2, 0);
						continue;
					}

					int n = f1 << 3 | f2 << 2 | f3 << 1 | f4;
					int dir = AutoAct.GetBuildDirection(n);
					if (dir == 3)
					{
						continue;
					}

					if (PointSetter.TrySet(p, d1, d2, 0))
					{
						HotItemHeld.taskBuild.recipe._dir = dir;
					}
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

				if (d1 >= 0)
				{
					// hasRange == false
					(d1, d2) = AutoAct.GetDelta(p, EClass.pc.pos, AutoAct.startDirection);
					if (d1 < 0)
					{
						d1 = -d1 * 2;
					}
				}

				PointSetter.TrySet(p, dist2ToLastPoint, d1, d2);
			}
		}

		return PointSetter.FinalPoint;
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
