﻿using System.Collections.Generic;
using TilesWalk.Building.Level;
using TilesWalk.Gameplay.Persistence;
using TilesWalk.Map.General;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace TilesWalk.Gameplay.Score
{
	[RequireComponent(typeof(IMapProvider))]
	public class GameScoresHelper : MonoBehaviour
	{
		[Inject] private GameSave _gameSave;
		[Inject] private ScorePointsConfiguration _scorePointsSettings;

		[SerializeField] private MapProviderSolver _solver;

		public int GameStars { get; private set; }

		private void Start()
		{
			if (_solver == null) _solver = new MapProviderSolver(gameObject);

			_solver.InstanceProvider();

			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			CalculateAllGameStars();
		}

		public bool IsCompleted(LevelMap levelMap)
		{
			var count = GetStarCount(levelMap);
			return count == 3;
		}

		public int GetStarCount(LevelMap levelMap)
		{
			if (_gameSave.Records.TryGetValue(levelMap.Id, out var score))
			{
				var ratio = (float) score.Points.Highest / levelMap.Target;

				if (ratio >= 1)
				{
					return 3;
				}

				if (ratio >= _scorePointsSettings.TwoStarRange)
				{
					return 2;
				}

				if (ratio >= _scorePointsSettings.OneStarRange)
				{
					return 1;
				}

				return 0;
			}

			return 0;
		}

		public int GetStarCount(LevelScore score)
		{
			var tileMap = _solver.Provider.AvailableMaps.Find(x => x.Id == score.Id);

			if (tileMap != null)
			{
				var ratio = (float)score.Points.Highest / tileMap.Target;

				if (ratio >= 1)
				{
					return 3;
				}

				if (ratio >= _scorePointsSettings.TwoStarRange)
				{
					return 2;
				}

				if (ratio >= _scorePointsSettings.OneStarRange)
				{
					return 1;
				}

				return 0;
			}

			return 0;
		}

		private void CalculateAllGameStars()
		{
			if (_gameSave.Records == null || _gameSave.Records.Count == 0) return;

			GameStars = 0;

			foreach (var scoreRecord in _gameSave.Records.Values)
			{
				GameStars += GetStarCount(scoreRecord);
			}
		}
	}
}