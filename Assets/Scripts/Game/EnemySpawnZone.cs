using Assets.Scripts.Game;
using Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class EnemySpawnZone : MonoBehaviour
{
    [SerializeField] private string _cleanedMessage;
    [SerializeField] private List<Enemy> _enemiesToKill = new();
    [SerializeField] private List<Door> _doorsToOpen = new();
    [SerializeField] private List<GameObject> _miscObjectsToActivate;
    [SerializeField] private EnemySpawnZone _nextRoom;

    //[SerializeField] private 

    private Collider _trigger;
    public Collider Trigger => _trigger;

    public UnityAction<string, string> OnCleaned;

#if UNITY_EDITOR
    [ContextMenu(nameof(KillAllEnemies))]
    private void KillAllEnemies()
    {
        StartCoroutine(ClearEnemiesManualy());
    }
#endif

    private void OnValidate()
    {
        for (int i = 0; i < _doorsToOpen.Count; i++)
        {
            if (_doorsToOpen[i] == null)
                _doorsToOpen.RemoveAt(i);
        }
    }

    private void Start()
    {
        _trigger = GetComponent<Collider>();

        _trigger.isTrigger = true;

        if (_doorsToOpen.Count > 0)
            _doorsToOpen.ForEach(door => { door.Close(); });

        if (_miscObjectsToActivate.Count > 0)
            _miscObjectsToActivate.ForEach(obj => { obj.SetActive(false); });

        if (_nextRoom != null && _nextRoom.Trigger != null)
            _nextRoom.Trigger.enabled = false;
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

        OnCleaned?.Invoke(_cleanedMessage, gameObject.name);

        CancelInvoke(nameof(DisbaleTrigger));
        DisbaleTrigger();

        StartCoroutine(ClearEnemiesManualy());
    }

    private IEnumerator ClearEnemiesManualy()
    {
        yield return new WaitForSeconds(2);

        if (_enemiesToKill != null || _enemiesToKill.Count <= 0)
        {
            _enemiesToKill.ForEach(e => e.gameObject.SetActive(false));
        }

        for (int i = 0; i < _enemiesToKill.Count; i++)
        {
            if (_enemiesToKill[i] == null)
                continue;

            Destroy(_enemiesToKill[i]);
            yield return new WaitForSeconds(0.2f);
        }

        _enemiesToKill.Clear();
        Destroy(this);
        Debug.Log($"{gameObject.name} SUCCESFULLY CLEANED!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<Enemy>(out var enemy) && !enemy.Health.IsDead && !_enemiesToKill.Contains(enemy))
        {
            _enemiesToKill.Add(enemy);
            enemy.Health.OnDead += OnEnemyDead;
            CancelInvoke(nameof(DisbaleTrigger));
            Invoke(nameof(DisbaleTrigger), 2);
        }
    }

    private void DisbaleTrigger()
    {
        if (_trigger != null)
            _trigger.enabled = false;
    }
}
