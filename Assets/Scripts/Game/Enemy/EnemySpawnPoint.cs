using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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
            // Собираем всех детей (включая себя)
            List<Transform> allPoints = GetComponentsInChildren<Transform>().ToList();
            // Удаляем самого себя (родителя)
            allPoints.RemoveAt(0);
            _patrolPoints = allPoints.ToArray();

#if UNITY_EDITOR
            // Назначаем иконки только в редакторе (и только если это объект в сцене, а не префаб)
            if (!EditorUtility.IsPersistent(gameObject)) // проверяем, что объект не в префабе
            {
                // 1. Зелёная иконка для родителя (текущего объекта)
                Texture2D greenIcon = EditorGUIUtility.FindTexture("sv_icon_dot3_pix16_gizmo");
                if (greenIcon != null)
                    EditorGUIUtility.SetIconForObject(gameObject, greenIcon);
                else
                    Debug.LogWarning("Не найдена зелёная иконка");

                // 2. Красные иконки для детей
                Texture2D redIcon = EditorGUIUtility.FindTexture("sv_icon_dot6_pix16_gizmo");
                if (redIcon != null)
                {
                    foreach (var point in _patrolPoints)
                    {
                        EditorGUIUtility.SetIconForObject(point.gameObject, redIcon);
                    }
                }
                else
                {
                    Debug.LogWarning("Не найдена красная иконка");
                }
            }
#endif
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
            _parentZone?.RegisterEnemy(enemy);

            Array.Resize(ref enemy.patrolPoints, _patrolPoints.Length);
            for (int i = 0; i < _patrolPoints.Length; i++)
                enemy.patrolPoints[i] = _patrolPoints[i].position;

            enemy.RefreshPatrol();
            Destroy(gameObject);
        }
    }
}