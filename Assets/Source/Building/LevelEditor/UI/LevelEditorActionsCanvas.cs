﻿using TilesWalk.General.UI;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace TilesWalk.Building.LevelEditor.UI
{
	public class LevelEditorActionsCanvas : CanvasGroupBehaviour
	{
		[Inject] private LevelEditorToolSet _levelEditorToolSet;

		[SerializeField] private Button _continue;
		[SerializeField] private Button _save;

		public Button Continue => _continue;

		public Button Save => _save;

		public void Start()
		{
			_continue.onClick.AsObservable().Subscribe(_ =>
			{
				_levelEditorToolSet.SetEditorInterfaceState(LevelEditorToolSet.State.EditorInsertionTools);
			}).AddTo(this);
		}
	}
}