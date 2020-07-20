﻿using System;
using Hellmade.Sound;
using UnityEngine;

namespace TilesWalk.Audio
{
	[Serializable]
	public class GameAudio
	{
		[SerializeField] private string _identifier;
		[SerializeField] private AudioClip _clip;
		[SerializeField, Range(0f, 1f)] private float volume;
		[SerializeField] private Hellmade.Sound.Audio _audio;

		public string Identifier => _identifier;

        public AudioClip Clip => _clip;

        public void PlayMusic(bool loop = false, bool persist = false, float fadeInSeconds = 1f,
			float fadeOutSeconds = 1f, float currentMusicfadeOutSeconds = -1f, Transform sourceTransform = null)
		{
			int audioID = EazySoundManager.PlayMusic(_clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds,
				currentMusicfadeOutSeconds, sourceTransform);
			_audio = EazySoundManager.GetAudio(audioID);
		}

		public void PlaySound(bool loop = false, Transform sourceTransform = null)
		{
			int audioID = EazySoundManager.PlaySound(_clip, volume, loop, sourceTransform);
			_audio = EazySoundManager.GetAudio(audioID);
		}

		public void PlayUISound()
		{
			int audioID = EazySoundManager.PlayUISound(_clip, volume);
			_audio = EazySoundManager.GetAudio(audioID);
		}
	}
}