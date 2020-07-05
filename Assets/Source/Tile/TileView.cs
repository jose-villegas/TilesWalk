﻿using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using TilesWalk.BaseInterfaces;
using TilesWalk.Building;
using TilesWalk.Building.Level;
using TilesWalk.Extensions;
using TilesWalk.Gameplay.Condition;
using TilesWalk.Gameplay.Display;
using TilesWalk.Gameplay.Input;
using TilesWalk.Gameplay.Score;
using TilesWalk.General;
using TilesWalk.General.FX;
using TilesWalk.Tile.Rules;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Zenject;

namespace TilesWalk.Tile
{
	public partial class TileView : TileViewTrigger, IView
	{
		[SerializeField] protected TileController _controller;

		[Inject] protected DiContainer _container;
		[Inject] protected TileViewFactory _tileFactory;
		[Inject] protected TileViewLevelMap _tileLevelMap;
		[Inject] protected GameTileColorsConfiguration _colorsConfiguration;
		[Inject] protected TileColorMaterialColorMatchHandler _colorHandler;
		[Inject] protected GameEventsHandler _gameEvents;
		[Inject(Optional = true)] protected LevelFinishTracker _levelFinishTracker;

		private TileViewLevelMapState _backupState;

		private MeshRenderer _meshRenderer;
		private BoxCollider _collider;
		private ParticleSystemsCollector _particleSystems;

		public BoxCollider Collider
		{
			get
			{
				if (_collider == null)
				{
					_collider = GetComponentInChildren<BoxCollider>();
				}

				return _collider;
			}
		}

		protected MeshRenderer Renderer
		{
			get
			{
				if (_meshRenderer == null)
				{
					_meshRenderer = GetComponentInChildren<MeshRenderer>();
				}

				return _meshRenderer;
			}
		}

		public TileController Controller
		{
			get => _controller;
			set => _controller = value;
		}

		public TileView()
		{
			_controller = new TileController();
		}

		private void OnDestroy()
		{
			_tileLevelMap.State = TileViewLevelMapState.FreeMove;
			StopAllCoroutines();
		}

		protected virtual void Start()
		{
			// Fetch FX particle system in children
			_particleSystems = gameObject.AddComponent<ParticleSystemsCollector>();

			// This small optimization enables us to share the material per color
			// instead of creating a new instance per every tile that tries to
			// change its color
			Renderer.material = _colorHandler.GetMaterial(_controller.Tile.TileColor);

			// update material on color update
			_controller.Tile.OnTileColorChangedAsObservable().Subscribe(UpdateColor).AddTo(this);

			// update particles on power up
			_controller.Tile.OnTilePowerUpChangedAsObservable().Subscribe(UpdatePowerUpFX).AddTo(this);

			_gameEvents.OnGamePausedAsObservable().Subscribe(OnGamePaused);
			_gameEvents.OnGameResumedAsObservable().Subscribe(OnGameResumed);

			// on level finish stop interactions
			if (_levelFinishTracker != null)
			{
				_levelFinishTracker.OnLevelFinishAsObservable().Subscribe(OnLevelFinish).AddTo(this);
			}
		}

		private void UpdatePowerUpFX(Tuple<Tile, TilePowerUp> power)
		{
			if (_particleSystems.ParticleFX == null || _particleSystems.ParticleFX.Count == 0) return;

			switch (power.Item1.PowerUp)
			{
				case TilePowerUp.None:
					_particleSystems["North"].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
					_particleSystems["South"].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
					_particleSystems["East"].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
					_particleSystems["West"].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
					_particleSystems["Color"].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
					break;
				case TilePowerUp.NorthSouthLine:
					_particleSystems["North"].Play();
					_particleSystems["South"].Play();
					break;
				case TilePowerUp.EastWestLine:
					_particleSystems["East"].Play();
					_particleSystems["West"].Play();
					break;
				case TilePowerUp.ColorMatch:
					_particleSystems["Color"].Play();
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(power), power, null);
			}
		}

		// check for combos
		private void Update()
		{
			// check for combos
			if (Controller.Tile.MatchingColorPatch != null && Controller.Tile.MatchingColorPatch.Count > 2)
			{
				RemoveCombo();
			}
		}

		private void OnGameResumed(Unit _)
		{
			if (_levelFinishTracker != null && _levelFinishTracker.IsFinished) return;

			_tileLevelMap.State = _backupState;
		}

		private void OnGamePaused(Unit _)
		{
			_backupState = _tileLevelMap.State;
			_tileLevelMap.State = TileViewLevelMapState.Locked;
		}

		private void OnLevelFinish(LevelScore _)
		{
			_tileLevelMap.State = TileViewLevelMapState.Locked;
			// StartCoroutine(LevelFinishAnimation());
		}

		protected virtual void OnMouseDown()
		{
			_onTileClicked?.OnNext(_controller.Tile);

			if (_tileLevelMap.IsMovementLocked()) return;

			if (_levelFinishTracker != null && _levelFinishTracker.IsFinished) return;

			Remove();
		}

		protected virtual void UpdateColor(Tuple<Tile, TileColor> color)
		{
			Renderer.material = _colorHandler.GetMaterial(color.Item1.TileColor);

			if (_controller.Tile.PowerUp == TilePowerUp.ColorMatch)
			{
				_particleSystems["Color"].Stop();
				var pColor = _colorsConfiguration[_controller.Tile.TileColor];
				ParticleSystem.MainModule settings = _particleSystems["Color"].main;
				settings.startColor = pColor;
				_particleSystems["Color"].Play();
			}
		}

		#region Debug

#if UNITY_EDITOR
		[Header("Editor")] private CardinalDirection direction = CardinalDirection.North;
		private NeighborWalkRule rule = NeighborWalkRule.Plain;


		[Button]
		private void AddNeighbor()
		{
			if (!_controller.Tile.IsValidInsertion(direction, rule))
			{
				Debug.LogError("Cannot insert a neighbor here, space already occupied ");
				return;
			}

			var tile = _tileFactory.NewInstance();
			this.InsertNeighbor(direction, rule, tile);

			// keep the same rule as parent, easier building
			tile.direction = direction;
			tile.rule = rule;
			// add new insertion instruction for this tile
			_tileLevelMap.UpdateInstructions(this, tile, direction, rule);
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.green;

			if (_controller.Tile.ShortestPathToLeaf != null)
			{
				foreach (var tile in _controller.Tile.ShortestPathToLeaf)
				{
					if (!_tileLevelMap.HasTileView(tile)) continue;

					var view = _tileLevelMap.GetTileView(tile);
					Gizmos.DrawCube(view.transform.position +
					                view.transform.up * 0.15f, Vector3.one * 0.15f);
				}
			}

			Gizmos.color = Color.blue;
			foreach (var hingePoint in _controller.Tile.HingePoints)
			{
				var relative = transform.rotation * hingePoint.Value;
				Gizmos.DrawSphere(transform.position + relative, 0.05f);

				if (!_tileLevelMap.HasTileView(_controller.Tile.Neighbors[hingePoint.Key])) continue;

				var view = _tileLevelMap.GetTileView(_controller.Tile.Neighbors[hingePoint.Key]);
				var joint = view.transform.rotation * view.Controller.Tile.HingePoints[hingePoint.Key.Opposite()];
				Gizmos.DrawLine(transform.position + relative, view.transform.position + joint);
			}

			Gizmos.color = Color.magenta;
			if (_controller.Tile.MatchingColorPatch != null && _controller.Tile.MatchingColorPatch.Count > 2)
			{
				foreach (var tile in _controller.Tile.MatchingColorPatch)
				{
					if (!_tileLevelMap.HasTileView(tile)) continue;

					var view = _tileLevelMap.GetTileView(tile);
					Gizmos.DrawWireCube(view.transform.position, Vector3.one);
				}
			}
		}
#endif

		#endregion
	}
}