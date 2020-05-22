﻿using TilesWalk.General;
using TilesWalk.Tile;
using UnityEngine;

namespace TilesWalk.Extensions
{
	public static class TileExtension
	{
		public static Vector3[] HingePoints(this Tile.Tile tile, CardinalDirection face)
		{
			var points = new Vector3[8];

			points[0] = tile.Bounds.min;
			points[1] = tile.Bounds.max;
			points[2] = new Vector3(points[0].x, points[0].y, points[1].z);
			points[3] = new Vector3(points[0].x, points[1].y, points[0].z);
			points[4] = new Vector3(points[1].x, points[0].y, points[0].z);
			points[5] = new Vector3(points[0].x, points[1].y, points[1].z);
			points[6] = new Vector3(points[1].x, points[0].y, points[1].z);
			points[7] = new Vector3(points[1].x, points[1].y, points[0].z);

			switch (face)
			{
				case CardinalDirection.North:
					return new Vector3[] { points[1], points[2], points[5], points[6] };
				case CardinalDirection.South:
					return new Vector3[] { points[0], points[3], points[4], points[7] };
				case CardinalDirection.East:
					return new Vector3[] { points[1], points[4], points[6], points[7] };
				case CardinalDirection.West:
					return new Vector3[] { points[0], points[2], points[3], points[5] };
				default:
					break;
			}

			return points;
		}

		public static TileOrientation Orientation(NeighborWalkRule rule)
		{
			switch (rule)
			{
				case NeighborWalkRule.Up:
				case NeighborWalkRule.Down:
					return TileOrientation.Vertical;
				case NeighborWalkRule.Plain:
				default:
					// do nothing
					return TileOrientation.Horizontal;
			}
		}
	}
}
