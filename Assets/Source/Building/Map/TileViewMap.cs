﻿using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Newtonsoft.Json;
using TilesWalk.General;
using TilesWalk.Tile;
using TilesWalk.Tile.Rules;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Zenject;

namespace TilesWalk.Building.Map
{
	public class TileViewMap : TileViewTrigger
	{
		[SerializeField] private bool _buildFromInstructionsAtStart;
		[TextArea, SerializeField] private string _instructions;
		[SerializeField] TileMap _tileMap = new TileMap();

		[Inject] private TileViewFactory _viewFactory;

		private Dictionary<Tile.Tile, TileView> _tileView = new Dictionary<Tile.Tile, TileView>();

		public Dictionary<TileView, int> TileToHash { get; } = new Dictionary<TileView, int>();
		public Dictionary<int, TileView> HashToTile { get; } = new Dictionary<int, TileView>();

		public Dictionary<int, List<InsertionInstruction>> Insertions { get; } =
			new Dictionary<int, List<InsertionInstruction>>();

		public TileMap TileMap => _tileMap;

		private void Start()
		{
			_viewFactory.OnNewInstanceAsObservable().Subscribe(OnNewTileInstance);

			if (_buildFromInstructionsAtStart)
			{
				_viewFactory.IsAssetLoaded.Subscribe(ready =>
				{
					if (ready) BuildFromInstructions();
				});
			}
		}

		private void OnNewTileInstance(TileView tile)
		{
			RegisterTile(tile);
			tile.OnComboRemovalAsObservable()
				.Subscribe(path => _onComboRemoval?.OnNext(path)).AddTo(this);
			tile.OnTileRemovedAsObservable()
				.Subscribe(path => _onTileRemoved?.OnNext(path)).AddTo(this);
		}

		public void RegisterTile(TileView tile, int? hash = null)
		{
			if (TileToHash.ContainsKey(tile))
			{
				var h = TileToHash[tile];
				TileToHash.Remove(tile);
				HashToTile.Remove(h);
			}

			var id = hash ?? tile.GetHashCode();
			TileToHash[tile] = id;
			HashToTile[id] = tile;
			_tileView[tile.Controller.Tile] = tile;
			// register tile to the tile map
			_tileMap.Tiles.Add(id, tile.Controller.Tile.Index);
		}

		public void RemoveTile(TileView tile)
		{
			if (!TileToHash.TryGetValue(tile, out var hash)) return;

			TileToHash.Remove(tile);
			HashToTile.Remove(hash);
			// remove from map
			_tileMap.Instructions.RemoveAll(x => x.tile == hash);
			_tileMap.Instructions.RemoveAll(x => x.root == hash);
			_tileMap.Tiles.Remove(hash);
			// remove all instructions that refer to this tile

			if (!Insertions.TryGetValue(hash, out var instructions)) return;

			Insertions.Remove(hash);

			foreach (var instruction in instructions)
			{
				Destroy(HashToTile[instruction.tile].gameObject);
			}
		}

		public TileView GetTileView(Tile.Tile tile)
		{
			return _tileView[tile];
		}

		[Button]
		public void RefreshAllPaths()
		{
			foreach (var viewKey in _tileView.Keys)
			{
				viewKey.RefreshShortestLeafPath();
				viewKey.RefreshMatchingColorPatch();
			}
		}

		[Button]
		public void GenerateInstructions()
		{
			var instr = Insertions.Values.SelectMany(x => x).ToList();
			var hashes = TileToHash.Values.ToList();
			var allTiles = new Dictionary<int, Vector3>();

			foreach (var hash in hashes)
			{
				allTiles[hash] = HashToTile[hash].Controller.Tile.Index;
			}

			var map = new TileMap()
			{
				Instructions = instr,
				Tiles = allTiles
			};

			_instructions = JsonConvert.SerializeObject(map);
		}

		[Button]
		public void BuildFromInstructions()
		{
			// reset data structures
			HashToTile.Clear();
			TileToHash.Clear();
			Insertions.Clear();
			// first instance all the needed tiles
			var map = JsonConvert.DeserializeObject<TileMap>(_instructions);

			foreach (var mapTile in map.Tiles)
			{
				var tile = _viewFactory.NewInstance();
				// register with the source hash
				RegisterTile(tile, mapTile.Key);
			}

			// Now execute neighbor insertion logic
			foreach (var instruction in map.Instructions)
			{
				var rootTile = HashToTile[instruction.root];
				var insert = HashToTile[instruction.tile];
				// adjust neighbor insertion
				rootTile.Controller.AddNeighbor(instruction.direction, instruction.rule, insert.Controller.Tile,
					rootTile.transform, insert.transform);
				UpdateInstructions(rootTile, insert, instruction.direction, instruction.rule);
			}
		}

		public void UpdateInstructions(TileView root, TileView tile, CardinalDirection d, NeighborWalkRule r)
		{
			if (!TileToHash.TryGetValue(root, out var rootId) ||
			    !TileToHash.TryGetValue(tile, out var tileId)) return;

			if (!Insertions.TryGetValue(rootId, out var insertions))
			{
				Insertions[rootId] = insertions = new List<InsertionInstruction>();
			}

			insertions.Add(new InsertionInstruction()
			{
				tile = tileId,
				root = rootId,
				direction = d,
				rule = r
			});

			_tileMap.Instructions.Add(insertions.Last());
		}
	}
}