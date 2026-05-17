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

        private void OnValidate()
        {
            _patrolPoints = null;
            List<Transform> allPoints = GetComponentsInChildren<Transform>().ToList();
            allPoints.RemoveAt(0);
            _patrolPoints = allPoints.ToArray();
        }

        private void Start()
        {
            Invoke(nameof(SpawnRequest), 0.2f);
        }

        private void SpawnRequest()
        {
            if (_gameController == null) _gameController = FindAnyObjectByType<GameController>();

            var enemy = _gameController.SpawnRequest(this);

            Array.Resize(ref enemy.patrolPoints, _patrolPoints.Length);

            for (int i = 0; i < _patrolPoints.Length; i++)
            {
                enemy.patrolPoints[i] = _patrolPoints[i].position;
            }

            Destroy(gameObject);
        }
    }
}