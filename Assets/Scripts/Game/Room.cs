using Game;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class Room : MonoBehaviour
{
    [SerializeField] private string _cleanedMessage;
    [SerializeField] private List<Enemy> _enemiesToKill = new();
    [SerializeField] private List<Door> _doorsToOpen = new();
    [SerializeField] private List<GameObject> _miscObjectsToActivate;
    [SerializeField] private Room _nextRoom;

    private Collider _trigger;
    public Collider Trigger => _trigger;

    public UnityAction<string> OnCleaned;

#if UNITY_EDITOR
    [ContextMenu(nameof(KillAllEnemies))]
    private async Task KillAllEnemies()
    {
        var enemiesCopy = new List<Enemy>(_enemiesToKill);
        for (int i = 0; i < enemiesCopy.Count; i++)
        {
            if (enemiesCopy[i] != null)
                enemiesCopy[i].Health.Kill();

            await Task.Delay(100);
        }

        _enemiesToKill.Clear();
    }
#endif

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
                Debug.Log($"SUCCESFULLY KILLED!");

                if (_nextRoom != null && _nextRoom.Trigger != null)
                    _nextRoom.Trigger.enabled = true;

                if (_doorsToOpen.Count > 0)
                    _doorsToOpen.ForEach(door => { door.Open(); });

                if (_miscObjectsToActivate.Count > 0)
                    _miscObjectsToActivate.ForEach(obj => { obj.SetActive(true); });

                OnCleaned?.Invoke(_cleanedMessage);

                CancelInvoke(nameof(DisbaleTrigger));
                DisbaleTrigger();

                Destroy(this);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<Enemy>(out var enemy) && !enemy.Health.IsDead && !_enemiesToKill.Contains(enemy))
        {
            _enemiesToKill.Add(enemy);
            enemy.Health.OnDead += OnEnemyDead;
            CancelInvoke(nameof(DisbaleTrigger));
            Invoke(nameof(DisbaleTrigger), 1);
        }
    }

    private void DisbaleTrigger()
    {
        _trigger.enabled = false;
    }
}
