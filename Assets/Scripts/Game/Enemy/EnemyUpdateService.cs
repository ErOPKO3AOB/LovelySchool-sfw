using System;
using System.Collections.Generic;
using Player;
#if UNITY_EDITOR
using Unity.Profiling;
#endif
using UnityEngine;
using VContainer.Unity;

namespace Assets.Scripts.Game
{
    public sealed class EnemyUpdateService : ITickable
    {
        private const int PeriodicCheckGroups = 4;

#if UNITY_EDITOR
        private static readonly ProfilerMarker TickMarker = new("Game.Enemy.TickAll");
#endif

        private readonly Transform _playerTransform;
        private readonly List<(Enemy Enemy, int Group)> _enemies = new();
        private int _registrationGroup;
        private int _periodicCheckGroup;
        private bool _isTicking;
        private bool _needsCompaction;

        public EnemyUpdateService(PlayerFacade player)
        {
            _playerTransform = player.transform;
        }

        internal void Register(Enemy enemy)
        {
            _enemies.Add((enemy, _registrationGroup));
            _registrationGroup = (_registrationGroup + 1) % PeriodicCheckGroups;
        }

        internal void Unregister(Enemy enemy)
        {
            int index = _enemies.FindIndex(entry => ReferenceEquals(entry.Enemy, enemy));
            if (index < 0)
                return;

            if (_isTicking)
            {
                // Урон и события могут отключить врага во время обхода списка.
                _enemies[index] = default;
                _needsCompaction = true;
            }
            else
            {
                _enemies.RemoveAt(index);
            }
        }

        public void Tick()
        {
            if (_enemies.Count == 0 || _playerTransform == null)
                return;

#if UNITY_EDITOR
            using var tickSample = TickMarker.Auto();
#endif
            Vector3 playerPosition = _playerTransform.position;
            float deltaTime = Time.deltaTime;
            int checkGroup = _periodicCheckGroup;
            _periodicCheckGroup = (_periodicCheckGroup + 1) % PeriodicCheckGroups;

            _isTicking = true;
            try
            {
                int count = _enemies.Count;
                for (int i = 0; i < count; i++)
                {
                    var (enemy, group) = _enemies[i];
                    if (enemy is null)
                        continue;

                    try
                    {
                        enemy.Tick(playerPosition, deltaTime, group == checkGroup);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, enemy);
                    }
                }
            }
            finally
            {
                _isTicking = false;
                if (_needsCompaction)
                {
                    _enemies.RemoveAll(entry => entry.Enemy is null);
                    _needsCompaction = false;
                }
            }
        }
    }
}
