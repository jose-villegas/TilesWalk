﻿using System;
using System.Collections.Generic;
using TilesWalk.Building.Level;
using TilesWalk.Gameplay.Persistence;
using TilesWalk.General;
using TilesWalk.Map.General;
using UniRx;
using UniRx.Triggers;
using Zenject;

namespace TilesWalk.Gameplay.Score
{
	public class LevelScorePointsTracker : ObservableTriggerBase
	{
		[Inject] private ScorePointsConfiguration _scorePointsConfiguration;
		[Inject] private TileViewLevelMap _tileLevelMap;
		[Inject(Optional = true)] private MapProviderSolver _solver;

		private Dictionary<string, int> _scoreTracking = new Dictionary<string, int>();

		protected Subject<LevelScore> _onScoreUpdated;
		protected Subject<LevelScore> _onScoresLoaded;
		protected LevelScore _currentScore;

		public LevelScore LevelScore
		{
			get
			{
				if (_solver != null)
				{
					if (!_solver.Provider.Records.TryGetValue(_tileLevelMap.LevelMap.Id, out var score))
					{
						_solver.Provider.Records[_tileLevelMap.LevelMap.Id] = new LevelScore(_tileLevelMap.LevelMap.Id);
					}

					return _currentScore = _solver.Provider.Records[_tileLevelMap.LevelMap.Id];
				}
				else
				{
					if (_currentScore == null)
					{
						_currentScore = new LevelScore(Constants.CustomLevelName);
					}

					return _currentScore;
				}
			}
		}

		private void Start()
		{
			if (_solver != null) _solver.InstanceProvider(gameObject);

			_tileLevelMap.OnTileRemovedAsObservable().Subscribe(OnTileRemoved).AddTo(this);
			_tileLevelMap.OnComboRemovalAsObservable().Subscribe(OnComboRemoval).AddTo(this);
			_tileLevelMap.OnLevelMapLoadedAsObservable().Subscribe(OnLevelMapLoaded).AddTo(this);
		}

		private void OnLevelMapLoaded(LevelMap map)
		{
			AddPoints(0);
			_onScoresLoaded?.OnNext(LevelScore);
		}

		public void ResetTrack()
		{
			var mapName = _tileLevelMap.LevelMap.Id;

			if (_scoreTracking.TryGetValue(LevelScore.Id, out var track))
			{
				_scoreTracking[LevelScore.Id] = 0;
				_onScoreUpdated?.OnNext(LevelScore);
			}
		}

		public void AddPoints(int points)
		{
			var mapName = _tileLevelMap.LevelMap.Id;

			if (!_scoreTracking.TryGetValue(LevelScore.Id, out var track))
			{
				_scoreTracking[LevelScore.Id] = 0;
			}

			_scoreTracking[LevelScore.Id] += points;
			LevelScore.Points.Update(_scoreTracking[LevelScore.Id]);
			_onScoreUpdated?.OnNext(LevelScore);
		}

		private void OnTileRemoved(List<Tile.Tile> tile)
		{
			AddPoints(_scorePointsConfiguration.PointsPerTile);
		}

		private void OnComboRemoval(List<Tile.Tile> tile)
		{
			AddPoints(_scorePointsConfiguration.PointsPerTile *
			          _scorePointsConfiguration.ComboMultiplier * tile.Count);
		}

		protected override void RaiseOnCompletedOnDestroy()
		{
			_onScoreUpdated?.OnCompleted();
			_onScoresLoaded?.OnCompleted();
		}

		public IObservable<LevelScore> OnScorePointsUpdatedAsObservable()
		{
			return _onScoreUpdated = _onScoreUpdated ?? new Subject<LevelScore>();
		}

		public IObservable<LevelScore> OnScoresLoadedAsObservable()
		{
			return _onScoresLoaded = _onScoresLoaded ?? new Subject<LevelScore>();
		}
	}
}