﻿using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using TilesWalk.Audio;
using TilesWalk.Building.Level;
using TilesWalk.Extensions;
using TilesWalk.General;
using UniRx;
using UnityEngine;
using Zenject;

namespace TilesWalk.Tile.Level
{
    /// <summary>
    /// This partial contains all the logic related to tile removal for the
    /// <see cref="LevelTileView"/>
    /// </summary>
    public partial class LevelTileView
    {
        [Inject] private GameAudioCollection _audioCollection;

        /// <summary>
        /// This method moves all the tiles to the position of their neighbor in the given path
        /// It also returns a backup array with their previous transforms, useful for animating.
        /// </summary>
        /// <param name="shufflePath">A backup array with their previous transforms</param>
        /// <returns></returns>
        private List<Tuple<Vector3, Quaternion>> ShufflePath(IReadOnlyList<LevelTileView> shufflePath)
        {
            if (shufflePath == null || shufflePath.Count <= 0) return null;

            // this structure with backup the origin position and rotations
            var backup = new List<Tuple<Vector3, Quaternion>>();

            for (var i = 0; i < shufflePath.Count - 1; i++)
            {
                var source = shufflePath[i];
                var nextTo = shufflePath[i + 1];
                // backup info
                backup.Add(new Tuple<Vector3, Quaternion>(source.transform.position, source.transform.rotation));
                // copy transform
                source.transform.position = nextTo.transform.position;
                source.transform.rotation = nextTo.transform.rotation;
            }

            return backup;
        }

        /// <summary>
        /// Handles removal of a tile
        /// </summary>
        [Button]
        private void Remove()
        {
            if (_tileLevelMap.IsMovementLocked())
            {
                Debug.LogWarning("Tile movement is currently locked, wait for unlock " +
                                 "for removal to be available again");
                return;
            }

            _tileLevelMap.State = TileLevelMapState.RemovingTile;
            var isPowerUpTile = _controller.Tile.PowerUp != TilePowerUp.None;

            _controller.Remove();
            var shufflePath = _controller.Tile.ShortestPathToLeaf;

            if (shufflePath == null || shufflePath.Count <= 0) return;

            // play removal fx
            ParticleSystems["Remove"].Play();

            var tiles = shufflePath.Select(x => _tileLevelMap.GetTileView(x)).ToList();

            // this structure with backup the origin position and rotations
            var backup = ShufflePath(tiles);

            // since the last tile has no other to exchange positions with, reduce its
            // scale to hide it before showing its new color
            var lastTile = _tileLevelMap.GetTileView(shufflePath[shufflePath.Count - 1]);
            var scale = lastTile.transform.localScale;
            lastTile.transform.localScale = Vector3.zero;

            _audioCollection.Play(GameAudioType.Sound, "Shuffle");
            MainThreadDispatcher.StartEndOfFrameMicroCoroutine(ShuffleMoveAnimation(tiles, backup));
            Observable.Timer(TimeSpan.FromSeconds(_animationSettings.ShuffleMoveTime))
                .DelayFrame(1)
                .Subscribe(_ => { }, () =>
                {
                    lastTile.ParticleSystems["PopIn"].Play();
                    MainThreadDispatcher.StartEndOfFrameMicroCoroutine(lastTile.ScalePopInAnimation(scale));
                    Observable.Timer(TimeSpan.FromSeconds(_animationSettings.ScalePopInTime))
                        .DelayFrame(1)
                        .Subscribe(_ => { }, () =>
                        {
                            // finally the remove animation is done, check for power-ups
                            if (isPowerUpTile)
                            {
                                HandlePowerUp(() => { Trigger.OnTileRemoved?.OnNext(shufflePath); });
                            }
                            else
                            {
                                _tileLevelMap.State = TileLevelMapState.FreeMove;
                                Trigger.OnTileRemoved?.OnNext(shufflePath);
                            }
                        }).AddTo(this);
                }).AddTo(this);
        }

        /// <summary>
        /// Handles power up logic and color changes
        /// </summary>
        /// <param name="onFinish"></param>
        private void HandlePowerUp(Action onFinish)
        {
            _tileLevelMap.State = TileLevelMapState.OnPowerUpRemoval;

            List<Tile> path = null;
            var audioToPlay = "";
            var audioPerTileToPlay = "";
            var powerUp = _controller.Tile.PowerUp;
            var particlePerTile = "";

            switch (_controller.Tile.PowerUp)
            {
                case TilePowerUp.None:
                    break;
                case TilePowerUp.NorthSouthLine:
                    path = _controller.Tile.GetStraightPath(CardinalDirection.North, CardinalDirection.South);
                    audioToPlay = "LinePower";
                    particlePerTile = "SwooshNS";
                    audioPerTileToPlay = "Clank";
                    break;
                case TilePowerUp.EastWestLine:
                    path = _controller.Tile.GetStraightPath(CardinalDirection.East, CardinalDirection.West);
                    audioToPlay = "LinePower";
                    particlePerTile = "SwooshEW";
                    audioPerTileToPlay = "Clank";
                    break;
                case TilePowerUp.ColorMatch:
                    path = _tileLevelMap.HashToTile.Select(x => x.Value.Controller.Tile)
                        .GetAllOfColor(_controller.Tile.TileColor);
                    audioToPlay = "ColorPower";
                    audioPerTileToPlay = "Wind";
                    particlePerTile = "ColorPop";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (path != null)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    var index = i;
                    var tileView = _tileLevelMap.GetTileView(path[i]);
                    var sourceScale = tileView.transform.localScale;

                    _audioCollection.Play(GameAudioType.Sound, audioToPlay);

                    MainThreadDispatcher.StartEndOfFrameMicroCoroutine(
                        tileView.ScalePopInAnimation(Vector3.zero, i * 0.1f));

                    Observable.Timer(TimeSpan.FromSeconds(_animationSettings.ScalePopInTime + i * 0.1f))
                        .DelayFrame(1)
                        .Subscribe(_ => { }, () =>
                        {
                            tileView.ParticleSystems["PopIn"].Play();
                            tileView.ParticleSystems[particlePerTile].Play();
                            _audioCollection.Play(GameAudioType.Sound, audioPerTileToPlay);

                            MainThreadDispatcher.StartEndOfFrameMicroCoroutine(
                                tileView.ScalePopInAnimation(sourceScale));

                            // callback for power up execution, remove power up
                            if (index == 0)
                            {
                                Trigger.OnPowerUpRemoval?.OnNext(new Tuple<List<Tile>, TilePowerUp>(path, powerUp));
                                Controller.Tile.PowerUp = TilePowerUp.None;
                            }

                            // change the color after it appears
                            tileView.Controller.Tile.ShuffleColor();

                            Observable.Timer(TimeSpan.FromSeconds(_animationSettings.ScalePopInTime))
                                .DelayFrame(1)
                                .Subscribe(_ => { },
                                    () =>
                                    {
                                        if (index == path.Count - 1)
                                        {
                                            // update paths as the colors have changed
                                            foreach (var rootTile in _tileLevelMap.Map.Roots)
                                            {
                                                var view = _tileLevelMap.HashToTile[rootTile.Key];
                                                view.Controller.Tile.ChainRefreshPaths(updateShortestPath: false);
                                            }

                                            _tileLevelMap.State = TileLevelMapState.FreeMove;
                                            onFinish?.Invoke();
                                        }
                                    }).AddTo(this);
                        }).AddTo(this);
                }
            }
        }

        /// <summary>
        /// This handles the logic of a combo removal
        /// </summary>
        [Button]
        private void RemoveCombo()
        {
            if (_tileLevelMap.IsMovementLocked())
            {
                Debug.LogWarning(
                    "Tile movement is currently locked, wait for unlock for removal to be available again");
                return;
            }

            // combo removals require at least three of the same color in the matching path
            if (_controller.Tile.MatchingColorPatch == null || _controller.Tile.MatchingColorPatch.Count <= 2)
            {
                Debug.LogWarning("A combo requires at least three matching color tiles together");
                return;
            }

            _tileLevelMap.State = TileLevelMapState.OnComboRemoval;
            var shufflePath = _controller.Tile.MatchingColorPatch;

            for (int i = 0; i < shufflePath.Count; i++)
            {
                var index = i;
                var tileView = _tileLevelMap.GetTileView(shufflePath[i]);
                var sourceScale = tileView.transform.localScale;

                _audioCollection.Play(GameAudioType.Sound, "Combo");
                MainThreadDispatcher.StartEndOfFrameMicroCoroutine(tileView.ScalePopInAnimation(Vector3.zero));
                Observable.Timer(TimeSpan.FromSeconds(_animationSettings.ScalePopInTime))
                    .DelayFrame(1)
                    .Subscribe(_ => { }, () =>
                    {
                        if (index == shufflePath.Count - 1)
                        {
                            tileView.Controller.RemoveCombo();
                        }

                        tileView.ParticleSystems["PopIn"].Play();
                        MainThreadDispatcher.StartEndOfFrameMicroCoroutine(tileView.ScalePopInAnimation(sourceScale));
                        Observable
                            .Timer(TimeSpan.FromSeconds(_animationSettings.ScalePopInTime))
                            .DelayFrame(1)
                            .Subscribe(_ => { },
                                () =>
                                {
                                    if (index == shufflePath.Count - 1)
                                    {
                                        _tileLevelMap.State = TileLevelMapState.FreeMove;
                                        Trigger.OnComboRemoval?.OnNext(shufflePath);
                                    }
                                }).AddTo(this);
                    }).AddTo(this);
            }
        }
    }
}