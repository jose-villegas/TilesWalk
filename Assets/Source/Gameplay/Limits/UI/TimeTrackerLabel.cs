﻿using System;
using TilesWalk.Building.Level;
using TilesWalk.Extensions;
using TilesWalk.Gameplay.Condition;
using TilesWalk.Gameplay.Input;
using TilesWalk.Gameplay.Score;
using TilesWalk.General.Patterns;
using TMPro;
using UniRx;
using UnityEngine;
using Zenject;

namespace TilesWalk.Gameplay.Limits.UI
{
	/// <summary>
	/// A text label that tracks the playing time left for a level
	/// </summary>
	[RequireComponent(typeof(TextMeshProUGUI))]
	public class TimeTrackerLabel : ObligatoryComponentBehaviour<TextMeshProUGUI>
	{
		[Inject] private TileViewLevelMap _tileLevelMap;
		[Inject] private LevelFinishTracker _levelFinishTracker;
		[Inject] private GameEventsHandler _gameEvents;
		private bool _gamePaused;

		private void Awake()
		{
			_gameEvents.OnGamePausedAsObservable().Subscribe(OnGamePaused).AddTo(this);
			_gameEvents.OnGameResumedAsObservable().Subscribe(OnGameResumed).AddTo(this);
			_levelFinishTracker.OnTrackersSetupFinishAsObservable().Subscribe(OnLevelMapLoaded).AddTo(this);
		}

		private void OnGameResumed(Unit obj)
		{
			_gamePaused = false;
		}

		private void OnGamePaused(Unit obj)
		{
			_gamePaused = true;
		}

		private void OnLevelMapLoaded(LevelScore score)
		{
			var condition = _levelFinishTracker.TimeFinishCondition;

			if (condition == null) return;

			var end = TimeSpan.FromSeconds(condition.Limit);

			Component.text = string.Format("00:00/{0:mm\\:ss}", end); ;

			condition.Tracker.SubscribeToText(Component, seconds =>
			{
				var current = TimeSpan.FromSeconds(seconds);

				if (_levelFinishTracker.IsFinished) return string.Format("{0:mm\\:ss}/{0:mm\\:ss}", end);

				if (_gamePaused) return string.Format("{0:mm\\:ss}/{1:mm\\:ss}", current, end);

				seconds++;

				return seconds < condition.Limit
					? string.Format("{0:mm\\:ss}/{1:mm\\:ss}", current, end)
					: string.Format("{0:mm\\:ss}/{0:mm\\:ss}", end);

			}).AddTo(this);
		}
	}
}