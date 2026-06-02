using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game
{
    public class EnemySpawnPoint : MonoBehaviour
    {
        public void Construct(GameController gameController)
        {
            _gameController = gameController;
        }

        private GameController _gameController;

        public GameObject enemyTypePrefab;
        public Transform[] _patrolPoints;
        private EnemySpawnZone _parentZone;

        private void OnValidate()
        {
            _patrolPoints = null;
            List<Transform> allPoints = GetComponentsInChildren<Transform>().ToList();
            allPoints.RemoveAt(0);
            _patrolPoints = allPoints.ToArray();
        }

        private void Start()
        {
            if (_gameController == null) _gameController = FindAnyObjectByType<GameController>();
            _parentZone = GetComponentInParent<EnemySpawnZone>(); // ищем зону-родителя
            Invoke(nameof(SpawnRequest), UnityEngine.Random.Range(0.25f, 1f));
        }

        private void SpawnRequest()
        {
            var enemy = _gameController.SpawnRequest(this);
            enemy.SpawnZone = _parentZone;   // <--- запоминаем, из какой зоны враг

            Array.Resize(ref enemy.patrolPoints, _patrolPoints.Length);
            for (int i = 0; i < _patrolPoints.Length; i++)
                enemy.patrolPoints[i] = _patrolPoints[i].position;

            enemy.RefreshPatrol();
            Destroy(gameObject);
        }
    }
}