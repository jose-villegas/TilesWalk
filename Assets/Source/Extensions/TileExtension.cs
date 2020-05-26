﻿using System.Collections.Generic;
using System.Linq;
using TilesWalk.General;
using TilesWalk.Tile.Rules;

namespace TilesWalk.Extensions
{
	public static class TileExtension
	{
		public static bool IsValidInsertion(this Tile.Tile source, CardinalDirection direction, NeighborWalkRule rule)
		{
			bool result = false;

			// first check if direction is already occupied
			if (source.Neighbors.ContainsKey(direction))
			{
				return source.Neighbors[direction] == null;
			}
			else
			{
				return true;
			}

			// todo: add rules here
		}

		/// <summary>
		/// This method finds the shortest path possible following all the paths
		/// available by walking through the neighbors
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public static List<Tile.Tile> GetShortestLeafPath(this Tile.Tile source, List<Tile.Tile> walk = null,
			CardinalDirection direction = CardinalDirection.None)
		{
			var keys = source.Neighbors.Keys;

			// check if we are on a leaf
			if (keys.Count == 1)
			{
				// return concatenated path
				if (direction != CardinalDirection.None && keys.First() == direction.Opposite()) return walk;
			}

			var count = int.MaxValue;
			List<Tile.Tile> result = null;
			var backup = walk?.ToList();

			foreach (var key in keys)
			{
				// avoid infinite loop
				if (direction != CardinalDirection.None && key == direction.Opposite()) continue;

				var value = source.Neighbors[key];

				if (value == null) continue;

				if (walk == null) walk = new List<Tile.Tile>();

				walk.Add(value);
				
				// concatenation of whole paths
				var path = value.GetShortestLeafPath(walk, key);

				if (path != null && path.Count < count)
				{
					walk = backup;
					result = path;
					count = path.Count;
				}
			}

			return result;
		}
	}
}