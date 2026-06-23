using Game;
using Newtonsoft.Json;
using Player;
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
        public CounterService(QuitSessionEventHandler quitSessionEventHandler)
        {
            _quitSessionEventHandler = quitSessionEventHandler;
        }

        private readonly QuitSessionEventHandler _quitSessionEventHandler;
        private Health _playerHealth;
        private PlayerFacade _playerFacade;

        private readonly List<SessionData> _sessionDatas = new(1);
        private int _sessionIndex = 0;

        #region Init
        public void Initialize()
        {
            _quitSessionEventHandler.OnQuit += OnQuit;

            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        }

        public void Setup(GameController gameController)
        {
            _playerHealth = gameController.PlayerFacade.Health;
            _playerFacade = gameController.PlayerFacade;
            _playerHealth.OnDead += OnPlayerDead;
        }

        private void SceneManager_sceneUnloaded(Scene scene)
        {
            if (scene.name == "Game")
            {
                _playerHealth.OnDead -= OnPlayerDead;
                _playerFacade = null;
                _playerHealth = null;
            }
        }

        private void OnPlayerDead(GameObject obj)
        {
            _sessionDatas[_sessionIndex].time_spent = Time.timeSinceLevelLoad;

            for (int i = 0; i < _playerFacade.Inventory.Weapons.Count; i++)
            {
                _sessionDatas[_sessionIndex].weapons.Add(_playerFacade.Inventory.Weapons[i].weaponName);
            }

            _sessionIndex++;
            _sessionDatas.Add(new());
        }

        private void OnQuit()
        {
            SendMetrics();
        }
        #endregion

        public void SetLastCompletedRoom(string name)
        {
            if (_sessionIndex < _sessionDatas.Count - 1)
                _sessionDatas[_sessionIndex].room_completed = name;
        }

        public void SendMetrics()
        {
            string sessionsJson = JsonConvert.SerializeObject(_sessionDatas);

            YG2.MetricaSend("last_session", new Dictionary<string, object>
            {
                { "sessions", sessionsJson }
            });


            Debug.Log($"{sessionsJson}");
        }

        public void Dispose()
        {
            _quitSessionEventHandler.OnQuit -= OnQuit;
            SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;

#if UNITY_EDITOR
            SendMetrics();
#endif
        }
    }

    [System.Serializable]
    public class SessionData
    {
        public string room_completed;           // последняя зачищенная комната
        public List<string> weapons;  // все названия оружия в руках
        public float time_spent;            // длительность сессии в секундах
    }
}