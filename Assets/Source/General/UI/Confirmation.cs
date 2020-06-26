﻿using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace TilesWalk.General.UI
{
	public class Confirmation  : CanvasGroupBehaviour
	{
		[SerializeField] private GameObject _messageContainer;
		[SerializeField] private TextMeshProUGUI _message;
		[SerializeField] private TextMeshProUGUI _title;
		[SerializeField] private Button _yes;
		[SerializeField] private Button _no;
		[SerializeField] private Button _close;

		private void Awake()
		{
			Hide();
		}

		public Confirmation Configure(string message, Action OnConfirm, Action OnCancel, Action OnClose)
		{
			_message.text = message;
			_title.text = "Warning";
			_messageContainer.SetActive(true);

			_yes.onClick.RemoveAllListeners();
			_no.onClick.RemoveAllListeners();
			_close.onClick.RemoveAllListeners();

			_yes.onClick.AddListener(() => OnConfirm?.Invoke());
			_no.onClick.AddListener(() => OnCancel?.Invoke());
			_close.onClick.AddListener(() => OnClose?.Invoke());

			_yes.onClick.AddListener(Hide);
			_no.onClick.AddListener(Hide);
			_close.onClick.AddListener(Hide);
			return this;
		}

		public Confirmation Configure(string message, Action OnConfirm, Action OnCancel)
		{
			_message.text = message;
			_title.text = "Warning";
			_messageContainer.SetActive(true);

			_yes.onClick.RemoveAllListeners();
			_no.onClick.RemoveAllListeners();
			_close.onClick.RemoveAllListeners();

			_yes.onClick.AddListener(() => OnConfirm?.Invoke());
			_no.onClick.AddListener(() => OnCancel?.Invoke());
			_close.onClick.AddListener(() => OnCancel?.Invoke());

			_yes.onClick.AddListener(Hide);
			_no.onClick.AddListener(Hide);
			_close.onClick.AddListener(Hide);
			return this;
		}

		public Confirmation Configure(Action OnConfirm, Action OnCancel, Action OnClose)
		{
			_title.text = "Are you sure?";
			_messageContainer.SetActive(false);

			_yes.onClick.RemoveAllListeners();
			_no.onClick.RemoveAllListeners();
			_close.onClick.RemoveAllListeners();

			_yes.onClick.AddListener(() => OnConfirm?.Invoke());
			_no.onClick.AddListener(() => OnCancel?.Invoke());
			_close.onClick.AddListener(() => OnClose?.Invoke());

			_yes.onClick.AddListener(Hide);
			_no.onClick.AddListener(Hide);
			_close.onClick.AddListener(Hide);
			return this;
		}

		public Confirmation Configure(Action OnConfirm, Action OnCancel)
		{
			_title.text = "Are you sure?";
			_messageContainer.SetActive(false);

			_yes.onClick.RemoveAllListeners();
			_no.onClick.RemoveAllListeners();
			_close.onClick.RemoveAllListeners();

			_yes.onClick.AddListener(() => OnConfirm?.Invoke());
			_no.onClick.AddListener(() => OnCancel?.Invoke());
			_close.onClick.AddListener(() => OnCancel?.Invoke());

			_yes.onClick.AddListener(Hide);
			_no.onClick.AddListener(Hide);
			_close.onClick.AddListener(Hide);
			return this;
		}

		public Confirmation Configure(Action OnConfirm)
		{
			_title.text = "Are you sure?";
			_messageContainer.SetActive(false);

			_yes.onClick.RemoveAllListeners();
			_no.onClick.RemoveAllListeners();
			_close.onClick.RemoveAllListeners();

			_yes.onClick.AddListener(() => OnConfirm?.Invoke());

			_yes.onClick.AddListener(Hide);
			_no.onClick.AddListener(Hide);
			_close.onClick.AddListener(Hide);
			return this;
		}

		public Confirmation Configure(string message, Action OnConfirm)
		{
			_message.text = message;
			_title.text = "Warning";
			_messageContainer.SetActive(true);

			_yes.onClick.RemoveAllListeners();
			_no.onClick.RemoveAllListeners();
			_close.onClick.RemoveAllListeners();

			_yes.onClick.AddListener(() => OnConfirm?.Invoke());

			_yes.onClick.AddListener(Hide);
			_no.onClick.AddListener(Hide);
			_close.onClick.AddListener(Hide);
			return this;
		}
	}
}
