﻿using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Newtonsoft.Json;
using TilesWalk.Building.Level;
using TilesWalk.Gameplay.Condition;
using TilesWalk.Gameplay.Persistence;
using UnityEngine;
using Zenject;

namespace TilesWalk.Gameplay.Installer
{
	[CreateAssetMenu(fileName = "GameMapsInstaller", menuName = "Installers/GameMapsInstaller")]
	public class GameMapsInstaller : ScriptableObjectInstaller<GameMapsInstaller>
	{
		[Header("Insert Setup")] [SerializeField]
		private string _name;

		[SerializeField, TextArea] private string _instructions;
		[SerializeField] private FinishCondition _condition;

		[SerializeField, ShowIf("IsTimeCondition"), Min(1)]
		private float _seconds;

		[SerializeField, ShowIf("IsMovesCondition"), Min(1)]
		private int _moves;

		[SerializeField, Range(0, 5)] private int _mapSize;
		[SerializeField] private int _starsRequired;
		[SerializeField] private int _targetPoints;

		[Header("Entries")] [SerializeField] private GameMapCollection _gameMaps = new GameMapCollection();

		[Header("Game Levels Map")] [SerializeField]
		private GameLevelsMap _gameMap;

		private bool IsTimeCondition => _condition == FinishCondition.TimeLimit;

		private bool IsMovesCondition => _condition == FinishCondition.MovesLimit;

		public GameLevelsMap GameMap
		{
			get => _gameMap;
			set => _gameMap = value;
		}

		public override void InstallBindings()
		{
			Container.Bind<GameMapCollection>().FromInstance(_gameMaps).AsSingle();
			Container.Bind<GameLevelsMap>().FromInstance(_gameMap).AsSingle();
		}

		[Button]
		public void Insert()
		{
			var map = JsonConvert.DeserializeObject<LevelMap>(_instructions);
			map.Id = _name;
			map.MapSize = _mapSize;
			map.FinishCondition = _condition;
			map.StarsRequired = _starsRequired;
			map.Target = _targetPoints;

			switch (_condition)
			{
				case FinishCondition.TimeLimit:
					var tCond = new TimeFinishCondition(map.Id, _seconds);
					_gameMaps.Insert(map, tCond);
					break;
				case FinishCondition.MovesLimit:
					var mCond = new MovesFinishCondition(map.Id, _moves);
					_gameMaps.Insert(map, mCond);
					break;
			}

		}
	}
}