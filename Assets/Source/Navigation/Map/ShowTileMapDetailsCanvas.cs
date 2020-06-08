﻿using System.Collections.Generic;
using BayatGames.SaveGameFree.Examples;
using TilesWalk.Building.Level;
using TilesWalk.Navigation.UI;
using TMPro.EditorUtilities;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Zenject;

namespace TilesWalk.Navigation.Map
{
	public class ShowTileMapDetailsCanvas : LevelNameRequireBehaviour
	{
		[Inject] private TileMapDetailsCanvas _detailsCanvas;

		protected override void OnTileMapFound()
		{
			transform.OnMouseDownAsObservable().Subscribe(OnMapTileClick).AddTo(this);
		}

		private void OnMapTileClick(Unit u)
		{
			if (_detailsCanvas.IsVisible)
			{
				_detailsCanvas.Hide();
			}
			else
			{
				_detailsCanvas.LevelName.Value = LevelName.Value;
				_detailsCanvas.Show();
			}
		}
	}
}