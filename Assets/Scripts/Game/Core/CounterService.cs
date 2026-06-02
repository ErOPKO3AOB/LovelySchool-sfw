using Game;
using Player;
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using YG;

namespace Assets.Scripts.Game
{
    public class CounterService : IInitializable, IDisposable
    {
        public CounterService(PlayerFacade playerFacade)
        {
            _playerHealth = playerFacade.Health;
        }

        private readonly Health _playerHealth;

        public int KillCount { get; private set; } = 0;
        public string LastCompletedRoom { get; private set; } = "NONE";

        public void Initialize()
        {
            KillCount = 0;

            _playerHealth.OnDead += OnPlayerDead;
        }

        private void OnPlayerDead(GameObject obj)
        {
            YG2.MetricaSend("enemy_kills", new Dictionary<string, object>
            {
                { "count", KillCount }
            });

            YG2.MetricaSend("room_completed", new Dictionary<string, object>
            {
                { "completed", LastCompletedRoom }
            });

            YG2.MetricaSend("time_spent", new Dictionary<string, object>
            {
                { "seconds", Time.timeSinceLevelLoad }
            });

            Debug.Log($"Player dead. KillCount: {KillCount}, LastCompletedRoom: {LastCompletedRoom}, time spent: {Time.timeSinceLevelLoad} seconds");
        }

        public void IncrementEnemyKillCount()
        {
            KillCount++;
        }

        public void SetLastCompletedRoom(string name)
        {
            LastCompletedRoom = name;
        }

        public void Dispose()
        {
            _playerHealth.OnDead -= OnPlayerDead;
        }
    }
}