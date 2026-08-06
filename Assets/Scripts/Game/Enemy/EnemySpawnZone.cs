using Assets.Scripts.Game;
using Game;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class EnemySpawnZone : MonoBehaviour
{
    [SerializeField] private List<Enemy> _enemiesToKill = new();
    [SerializeField] private List<Door> _doorsToOpen = new();
    [SerializeField] private List<GameObject> _miscObjectsToActivate;
    [SerializeField] private EnemySpawnZone _nextRoom;

    //[SerializeField] private 

    private Collider _trigger;
    public Collider Trigger => _trigger;

    public UnityAction<int, string> OnCleaned;

#if UNITY_EDITOR
    [ContextMenu(nameof(KillAllEnemies))]
    private void KillAllEnemies()
    {
        var enemies = _gameController.GetEnemiesOfZone(this);
        StartCoroutine(DelayedDestroyEnemies(enemies));
    }
#endif

    // EnemySpawnZone.cs

    private int _roomIndex = -1;
    private GameController _gameController;
    private bool _isClearedOnStart;
    private bool _deactivated;

    public void SetRoomIndex(int index, GameController gc)
    {
        _roomIndex = index;
        _gameController = gc;
        // Если индекс комнаты меньше индекса текущего чекпоинта, она уже пройдена
        _isClearedOnStart = index < gc.CurrCheckPointIndex;
    }

    private void OnValidate()
    {
        for (int i = 0; i < _doorsToOpen.Count; i++)
        {
            if (_doorsToOpen[i] == null)
                _doorsToOpen.RemoveAt(i);
        }
    }

    private bool _initialized;

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        _trigger = GetComponent<Collider>();
        _trigger.isTrigger = true;

        if (_isClearedOnStart)
        {
            DeactivateForCheckpoint(_gameController);
            return;
        }

        // Обычная инициализация новой комнаты
        if (_doorsToOpen.Count > 0)
            _doorsToOpen.ForEach(door => door.Close());

        if (_miscObjectsToActivate.Count > 0)
            _miscObjectsToActivate.ForEach(obj => obj.SetActive(false));

        if (_nextRoom != null && _nextRoom.Trigger != null)
            _nextRoom.Trigger.enabled = false;
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null || _enemiesToKill.Contains(enemy))
            return;

        _enemiesToKill.Add(enemy);
        enemy.Health.OnDead += OnEnemyDead;
    }

    private void OnEnemyDead(GameObject obj)
    {
        if (obj.TryGetComponent<Enemy>(out var enemy))
        {
            _enemiesToKill.Remove(enemy);
            enemy.Health.OnDead -= OnEnemyDead;

            _enemiesToKill.Remove(_enemiesToKill.Find(e => e == null));

            if (_enemiesToKill.Count == 0)
            {
                CleanZone();
            }
        }
    }

    public void CleanZone()
    {
        if (_nextRoom != null && _nextRoom.Trigger != null)
            _nextRoom.Trigger.enabled = true;

        if (_doorsToOpen.Count > 0)
            _doorsToOpen.ForEach(door => { door.Open(); });

        if (_miscObjectsToActivate.Count > 0)
            _miscObjectsToActivate.ForEach(obj => { obj.SetActive(true); });

        OnCleaned?.Invoke(_roomIndex, gameObject.name);

        CancelInvoke(nameof(DisbaleTrigger));
        DisbaleTrigger();

        StartCoroutine(ClearEnemiesManualy());

        if (!_gameController.Checkpoints[_roomIndex].reached)
        {
            _gameController.Checkpoints[_roomIndex].ActivateCheckPoint();
        }

        for (int i = 0; i < _gameController.Checkpoints.Length; i++)
        {
            if (_gameController.CurrCheckPointIndex + 1 <= _gameController.Checkpoints.Length - 1)
            {
                _gameController.Checkpoints[_gameController.CurrCheckPointIndex + 1].gameObject.SetActive(true);
                break;
            }
        }
    }

    private IEnumerator ClearEnemiesManualy()
    {
        yield return new WaitForSeconds(2);

        if (_enemiesToKill != null || _enemiesToKill.Count <= 0)
        {
            _enemiesToKill.ForEach(e => e.gameObject.SetActive(false));
        }

        else StopCoroutine(ClearEnemiesManualy());

        for (int i = 0; i < _enemiesToKill.Count; i++)
        {
            if (_enemiesToKill[i] == null)
                continue;

            Destroy(_enemiesToKill[i]);
            yield return new WaitForSeconds(0.2f);
        }

        _enemiesToKill.Clear();
        Destroy(this);
    }

    public void DeactivateForCheckpoint(GameController gameController)
    {
        if (_deactivated) return;
        _deactivated = true;

        // 1. Останавливаем и отключаем спавн-точки
        var spawnPoints = GetComponentsInChildren<EnemySpawnPoint>();
        foreach (var sp in spawnPoints)
        {
            sp.CancelInvoke();
            sp.gameObject.SetActive(false);
        }

        // 2. Ищем врагов через GameController (а не через иерархию)
        var enemies = gameController.GetEnemiesOfZone(this);
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            enemy.gameObject.SetActive(false);            // скрыть мгновенно
            gameController.RemoveEnemyFromList(enemy);    // убрать из счётчика
        }

        // 3. Открываем двери, активируем объекты (уже не спавн-точки)
        foreach (var door in _doorsToOpen)
            if (door != null) door.Open();
        foreach (var obj in _miscObjectsToActivate)
            if (obj != null) obj.SetActive(true);

        if (_nextRoom != null && _nextRoom.Trigger != null)
            _nextRoom.Trigger.enabled = true;

        if (_trigger != null) _trigger.enabled = false;

        // 4. Удаляем с задержкой
        if (enemies.Count > 0)
            StartCoroutine(DelayedDestroyEnemies(enemies));
    }

    private IEnumerator DelayedDestroyEnemies(List<Enemy> enemies)
    {
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
                yield return new WaitForSeconds(0.2f);
            }
        }
        Debug.Log($"{gameObject.name} enemies destroyed with delay.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<Enemy>(out var enemy) && !enemy.Health.IsDead)
        {
            RegisterEnemy(enemy);
            CancelInvoke(nameof(DisbaleTrigger));
            Invoke(nameof(DisbaleTrigger), 3);
        }
    }

    private void DisbaleTrigger()
    {
        if (_trigger != null)
            _trigger.enabled = false;
    }
}
