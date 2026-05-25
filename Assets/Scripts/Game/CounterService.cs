using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;
using YG;

namespace Assets.Scripts.Game
{
    public class CounterService : IInitializable, IDisposable
    {
        public int KillCount { get; private set; } = 0;
        public string LastCompletedRoom { get; private set; } = "NONE";

        public void Initialize()
        {
            KillCount = 0;
         
            SceneManager.sceneUnloaded += SceneUnloaded;
        }

        private void SceneUnloaded(Scene scene)
        {
#if !UNITY_EDITOR
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
#else
            Debug.Log($"Scene unloaded. KillCount: {KillCount}, LastCompletedRoom: {LastCompletedRoom}, time spent: {Time.timeSinceLevelLoad} seconds");
#endif
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
            SceneManager.sceneUnloaded -= SceneUnloaded;
        }
    }
}