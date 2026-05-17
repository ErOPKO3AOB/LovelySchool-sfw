using System;
using UnityEngine;

namespace Game
{
    public class Health : MonoBehaviour
    {
        public void ConstructManually(PooledSpawnService pooledSpawnService)
        {
            _pooledSpawnService = pooledSpawnService;
        }

        private PooledSpawnService _pooledSpawnService;

        [SerializeField] private int _health;
        [SerializeField] private int _maxHealth;
        [SerializeField] private float _healSpeed;       // сколько здоровья восстанавливается в секунду
        [SerializeField] private float _healDelayTime;   // задержка перед началом восстановления после получения урона
        [SerializeField] private GameObject _bloodPrefab;

        private float _healDelayTimer;
        private float _healAccumulator; // накопленное дробное лечение

        public event Action<int> HealthChanged;  // передаёт текущее здоровье
        public event Action<GameObject> OnDead;  // вызывается при смерти (передаёт сам объект)

        public int CurrentHealth => _health;
        public int MaxHealth => _maxHealth;
        public bool IsDead => _health <= 0;

        private void Start()
        {
            if (_health <= 0) _health = _maxHealth;
            _health = Mathf.Clamp(_health, 0, _maxHealth);
            _healDelayTimer = 0f;
            _healAccumulator = 0f;
        }

        private void Update()
        {
            if (IsDead) return; // ⬅️ новая строка – не восстанавливаем здоровье после смерти

            // Уменьшаем таймер задержки восстановления
            if (_healDelayTimer > 0f)
            {
                _healDelayTimer -= Time.deltaTime;
                return; // во время задержки не лечим
            }

            // Если здоровье полное или скорость лечения 0 - ничего не делаем
            if (_health >= _maxHealth || _healSpeed <= 0f)
                return;

            // Накопление лечения
            _healAccumulator += _healSpeed * Time.deltaTime;
            int healAmount = Mathf.FloorToInt(_healAccumulator);
            if (healAmount > 0)
            {
                _health = Mathf.Min(_health + healAmount, _maxHealth);
                _healAccumulator -= healAmount;
                HealthChanged?.Invoke(_health);
            }
        }

        /// <summary>
        /// Нанести урон.
        /// </summary>
        /// <param name="damage">Количество урона.</param>
        public void TakeDamage(int damage)
        {
            if (_health <= 0) return; // уже мёртв

            _health -= damage;
            _health = Mathf.Max(_health, 0);

            // Сбрасываем таймер восстановления и накопленное лечение
            _healDelayTimer = _healDelayTime;
            _healAccumulator = 0f;

            HealthChanged?.Invoke(_health);

            if (_health <= 0)
            {
                OnDead?.Invoke(gameObject);
            }

            if (_bloodPrefab != null && _pooledSpawnService != null)
            {
                _pooledSpawnService.SpawnObject(_bloodPrefab, "Blood", transform.position, transform.eulerAngles, 2);
            }
        }

        /// <summary>
        /// Восстановить здоровье (например, от аптечки).
        /// </summary>
        public void Heal(int amount)
        {
            if (_health <= 0) return;
            _health = Mathf.Min(_health + amount, _maxHealth);
            _healAccumulator = 0f; // можно и не сбрасывать, но для чистоты
            HealthChanged?.Invoke(_health);
        }

        public void Kill()
        {
            if (_health <= 0) return; // уже мёртв – ничего не делаем

            _health = 0;
            _healDelayTimer = 0f;      // сбрасываем таймеры восстановления
            _healAccumulator = 0f;

            HealthChanged?.Invoke(_health);
            OnDead?.Invoke(gameObject);
        }

        /// <summary>
        /// Установить максимальное здоровье и восстановить до максимума (для ресета).
        /// </summary>
        public void ResetHealth()
        {
            _health = _maxHealth;
            _healDelayTimer = 0f;
            _healAccumulator = 0f;
            HealthChanged?.Invoke(_health);
        }
    }
}