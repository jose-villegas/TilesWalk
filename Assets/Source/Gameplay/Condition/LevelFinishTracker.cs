﻿using System;
using System.Collections.Generic;
using TilesWalk.Building.Level;
using TilesWalk.Gameplay.Score;
using TilesWalk.Map.General;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Zenject;

namespace TilesWalk.Gameplay.Condition
{
	public class LevelFinishTracker : ObservableTriggerBase
	{
		[Inject] private LevelScorePointsTracker _levelScorePointsTracker;
		[Inject] private TileViewLevelMap _tileLevelMap;
		[Inject] private MapProviderSolver _solver;

		private TargetScorePointsCondition _targetPointsCondition;
		private MovesFinishCondition _movesFinishCondition;
		private TimeFinishCondition _timeFinishCondition;

		private Subject<LevelScore> _onLevelFinish;
		private Subject<LevelScore> _onTrackersSetupFinish;
		private Subject<LevelScore> _onScorePointsTargetReached;

		public MovesFinishCondition MovesFinishCondition => _movesFinishCondition;
		public TimeFinishCondition TimeFinishCondition => _timeFinishCondition;

		private void Awake()
		{
			_solver.InstanceProvider(gameObject);
			_tileLevelMap.OnLevelMapLoadedAsObservable().Subscribe(OnLevelMapLoaded).AddTo(this);
		}

		private void OnLevelMapLoaded(LevelMap levelMap)
		{
			_targetPointsCondition = new TargetScorePointsCondition(levelMap.Id, levelMap.Target);

			// find finish conditions
			_movesFinishCondition = _solver.Provider.Collection.MovesFinishConditions.Find(x => x.Id == levelMap.Id);
			_timeFinishCondition = _solver.Provider.Collection.TimeFinishConditions.Find(x => x.Id == levelMap.Id);

			if (_movesFinishCondition == null)
			{
				_movesFinishCondition = new MovesFinishCondition(levelMap.Id, int.MaxValue);
			}

			if (_timeFinishCondition == null)
			{
				_timeFinishCondition = new TimeFinishCondition(levelMap.Id, float.MaxValue);
			}

			_movesFinishCondition.Reset(0);
			_timeFinishCondition.Reset(0);

			// track score points
			_levelScorePointsTracker
				.OnScorePointsUpdatedAsObservable().Subscribe(score =>
				{
					_targetPointsCondition.Update(score.Points.Last);
				})
				.AddTo(this);

			// track target points completion
			_targetPointsCondition.IsConditionMeet.Subscribe(meet =>
			{
				if (!meet) return;

				_levelScorePointsTracker.LevelScore.Moves.Update(_movesFinishCondition.Tracker.Value);
				_levelScorePointsTracker.LevelScore.Time.Update(_timeFinishCondition.Tracker.Value);
				_onScorePointsTargetReached?.OnNext(_levelScorePointsTracker.LevelScore);
			}).AddTo(this);

			// track time
			TimeTracking(levelMap);

			// track moves
			MovesTracking();

			_onTrackersSetupFinish?.OnNext(_levelScorePointsTracker.LevelScore);
		}

		private void MovesTracking()
		{
			_tileLevelMap.OnTileRemovedAsObservable().Subscribe(_ => { _movesFinishCondition?.Update(1); })
				.AddTo(this);

			if (_tileLevelMap.LevelMap.FinishCondition == FinishCondition.MovesLimit)
			{
				_movesFinishCondition.IsConditionMeet.Subscribe(meet =>
				{
					if (!meet) return;

					if (!_targetPointsCondition.IsConditionMeet.Value)
					{
						_levelScorePointsTracker.LevelScore.Moves.Update(_movesFinishCondition.Limit);
						_levelScorePointsTracker.LevelScore.Time.Update(_timeFinishCondition.Tracker.Value);
					}

					_onLevelFinish?.OnNext(_levelScorePointsTracker.LevelScore);
				}).AddTo(this);
			}
		}

		private void TimeTracking(LevelMap levelMap)
		{
			transform.UpdateAsObservable().Subscribe(_ =>
				{
					if (!_timeFinishCondition.IsConditionMeet.Value)
					{
						_timeFinishCondition?.Update(Time.deltaTime);
					}
				})
				.AddTo(this);

			if (levelMap.FinishCondition == FinishCondition.TimeLimit)
			{
				_timeFinishCondition.IsConditionMeet.Subscribe(meet =>
				{
					if (!meet) return;

					if (!_targetPointsCondition.IsConditionMeet.Value)
					{
						_levelScorePointsTracker.LevelScore.Moves.Update(_movesFinishCondition.Tracker.Value);
						_levelScorePointsTracker.LevelScore.Time.Update(_timeFinishCondition.Limit);
					}

					_onLevelFinish?.OnNext(_levelScorePointsTracker.LevelScore);
				}).AddTo(this);
			}
		}

		protected override void RaiseOnCompletedOnDestroy()
		{
			_onLevelFinish?.OnCompleted();
			_onScorePointsTargetReached?.OnCompleted();
			_onTrackersSetupFinish?.OnCompleted();
		}

		public IObservable<LevelScore> OnTrackersSetupFinishAsObservable()
		{
			return _onTrackersSetupFinish = _onTrackersSetupFinish ?? new Subject<LevelScore>();
		}

		public IObservable<LevelScore> OnLevelFinishAsObservable()
		{
			return _onLevelFinish = _onLevelFinish ?? new Subject<LevelScore>();
		}

		public IObservable<LevelScore> OnScorePointsTargetReachedAsObservable()
		{
			return _onScorePointsTargetReached = _onScorePointsTargetReached ?? new Subject<LevelScore>();
		}
	}
}