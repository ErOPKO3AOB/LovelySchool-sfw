using Data;
using Player;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace Game
{
    public class Enemy : MonoBehaviour
    {
        [Inject]
        public void Construct(WeaponSettingsSO weaponSettings, PlayerFacade playerFacade, PooledSpawnService pooledSpawnService, SoundEmitterService soundEmitterService)
        {
            _weaponSettings = weaponSettings;
            _playerFacade = playerFacade;
            _pooledSpawnService = pooledSpawnService;
            _soundEmitterService = soundEmitterService;
        }

        private WeaponSettingsSO _weaponSettings;
        private PooledSpawnService _pooledSpawnService;
        private SoundEmitterService _soundEmitterService;

        [Header("Health")]
        [SerializeField] private Health _health;
        public Health Health => _health;

        [Header("Animator")]
        [SerializeField] private Material _outlineMaterial;
        private Material _defaultMaterial;
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _ragdoll;
        [SerializeField] private Rigidbody _ragdollRb;
        [SerializeField] private GameObject _objectsToClean;
        [SerializeField] private SkinnedMeshRenderer _shkilaRenderer;
        [SerializeField] private SkinnedMeshRenderer _ragdollRenderer;
        [SerializeField] private Collider _collider;

        [Header("Movement")]
        [SerializeField] private NavMeshAgent _agent;
        public Vector3[] patrolPoints;                         // точки патруля (могут быть заданы извне)
        [SerializeField] private int _patrolPointsCount = 5;   // количество точек для автогенерации
        [SerializeField] private float _patrolRadius = 10f;    // радиус генерации
        private int _currentPatrolIndex;

        [Header("Stuck Detection")]
        [SerializeField] private float _stuckCheckInterval = 0.5f;   // как часто проверять застревание
        [SerializeField] private float _stuckThreshold = 1.5f;       // время без движения для застревания
        [SerializeField] private int _maxStuckRetries = 3;           // сколько раз переходить к след. точке перед перегенерацией (только для автогенерированных точек)
        private float _stuckTimer;
        private Vector3 _lastAgentPosition;
        private float _stuckDuration;
        private int _stuckRetryCount;
        private bool _patrolPointsGenerated;                          // true, если точки созданы автоматически

        [Header("Vision")]
        [SerializeField] private float _detectionRange = 15f;
        [SerializeField] private float _fieldOfView = 90f;
        [SerializeField] private float _rearDetectionRange = 2f;   // дистанция, на которой враг видит даже за спиной
        [SerializeField] private float _visionCheckInterval = 0.3f;
        [SerializeField] private LayerMask _obstacleMask;

        [Header("Memory")]
        [SerializeField] private float _memoryTime = 5f;
        private float _forgetTimer;

        [Header("Sounds")]
        [SerializeField] private float _roarIntervalMin = 5f;
        [SerializeField] private float _roarIntervalMax = 12f;

        [Header("Attack")]
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _attackCooldown = 1f;
        [SerializeField] private float _attackStartDelay = 0.25f;
        [SerializeField] private int _attackDamage = 10;
        private float _attackTimer;
        private float _attackReadyTimer;

        [Header("Loot")]
        [SerializeField, UnityEngine.Range(0, 100)] private int _ammoSpawnChance = 30;

        private PlayerFacade _playerFacade;
        private float _visionTimer;
        private int _playerLayer;

        private enum State { Patrol, Chase }
        private State _currentState = State.Patrol;

        private bool _dead;
        private float _roarTimer;

        private float _destinationUpdateTimer;
        private const float DESTINATION_UPDATE_INTERVAL = 0.25f;

        public NavMeshAgent Agent => _agent;

        private void OnValidate()
        {
            gameObject.layer = LayerMask.NameToLayer("Enemy");
        }

        private void Start()
        {
            _defaultMaterial = _shkilaRenderer.material;

            _health.ConstructManually(_pooledSpawnService);
            _dead = false;
            _health.OnDead += OnDead;

            _playerLayer = LayerMask.NameToLayer("Player");

            if (_agent == null)
                _agent = GetComponent<NavMeshAgent>();

            // Если точки патруля не заданы – генерируем
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                GeneratePatrolPoints();
                _patrolPointsGenerated = true;
            }
            else
            {
                _patrolPointsGenerated = false;
            }

            GoToNextPatrolPoint();

            _roarTimer = Random.Range(_roarIntervalMin, _roarIntervalMax);
            _forgetTimer = 0f;
            _lastAgentPosition = transform.position;
            _stuckTimer = 0f;
            _stuckDuration = 0f;
            _stuckRetryCount = 0;
        }

        public void SetOutlined(bool value)
        {
            Transform[] fullObjects = GetComponentsInChildren<Transform>();

            if (value)
            {
                _shkilaRenderer.material = _outlineMaterial;

                foreach (var item in fullObjects)
                {
                    item.gameObject.layer = LayerMask.NameToLayer("OutlinedEnemy");
                }
            }

            else
            {
                _shkilaRenderer.material = _defaultMaterial;
                foreach (var item in fullObjects)
                {
                    item.gameObject.layer = LayerMask.NameToLayer("Enemy");
                }
            }
        }

        private void GeneratePatrolPoints()
        {
            patrolPoints = new Vector3[_patrolPointsCount];
            for (int i = 0; i < _patrolPointsCount; i++)
            {
                Vector3 randomDirection = Random.insideUnitSphere * _patrolRadius;
                randomDirection += transform.position;
                if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, _patrolRadius, NavMesh.AllAreas))
                {
                    patrolPoints[i] = hit.position;
                }
                else
                {
                    patrolPoints[i] = transform.position;
                }
            }
        }

        private void Update()
        {
            if (_playerFacade == null || _dead) return;

            // Проверка застревания с интервалом
            _stuckTimer -= Time.deltaTime;
            if (_stuckTimer <= 0f)
            {
                _stuckTimer = _stuckCheckInterval;
                CheckStuck();
            }

            // Таймер забывания
            if (_forgetTimer > 0f)
                _forgetTimer -= Time.deltaTime;

            // Проверка зрения
            _visionTimer -= Time.deltaTime;
            if (_visionTimer <= 0f)
            {
                _visionTimer = _visionCheckInterval;
                CheckVision();
            }

            State previousState = _currentState;
            _currentState = (_forgetTimer > 0f) ? State.Chase : State.Patrol;

            if (previousState != _currentState && _currentState == State.Chase)
            {
                _attackReadyTimer = _attackStartDelay;
                // Здесь можно добавить звук обнаружения
            }

            switch (_currentState)
            {
                case State.Patrol:
                    if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                        GoToNextPatrolPoint();
                    break;
                case State.Chase:
                    if (_playerFacade != null)
                    {
                        _destinationUpdateTimer -= Time.deltaTime;
                        if (_destinationUpdateTimer <= 0f)
                        {
                            _agent.SetDestination(_playerFacade.transform.position);
                            _destinationUpdateTimer = DESTINATION_UPDATE_INTERVAL;
                        }
                    }
                    break;
            }

            // Случайный рык
            _roarTimer -= Time.deltaTime;
            if (_roarTimer <= 0f)
            {
                PlayRandomRoar();
                _roarTimer = Random.Range(_roarIntervalMin, _roarIntervalMax);
            }

            // Атака
            if (_currentState == State.Chase)
            {
                if (_attackReadyTimer > 0f)
                    _attackReadyTimer -= Time.deltaTime;

                _attackTimer -= Time.deltaTime;
                float distanceToPlayer = Vector3.Distance(transform.position, _playerFacade.transform.position);
                if (distanceToPlayer <= _attackRange && _attackReadyTimer <= 0f && _attackTimer <= 0f)
                {
                    Vector3 directionToPlayer = (_playerFacade.transform.position - transform.position).normalized;
                    if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, _attackRange, _obstacleMask))
                    {
                        if (hit.collider.gameObject.layer == _playerLayer)
                        {
                            if (_playerFacade.TryGetComponent<Health>(out var playerHealth))
                            {
                                playerHealth.TakeDamage(_attackDamage);
                                _attackTimer = _attackCooldown;
                                _animator.SetTrigger("Attack");
                            }
                        }
                    }
                }
            }
        }

        private void CheckVision()
        {
            if (_playerFacade == null) return;

            float distance = Vector3.Distance(transform.position, _playerFacade.transform.position);
            if (distance > _detectionRange)
                return;

            Vector3 directionToPlayer = (_playerFacade.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            // Если игрок далеко и не в поле зрения – не видим
            if (distance > _rearDetectionRange && angle > _fieldOfView * 0.5f)
                return;

            Debug.DrawRay(transform.position, directionToPlayer * _detectionRange, Color.red, 1f);

            if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, _detectionRange, _obstacleMask))
            {
                if (hit.collider.gameObject.layer == _playerLayer)
                {
                    _forgetTimer = _memoryTime;
                }
            }
        }

        private void CheckStuck()
        {
            if (_agent == null || !_agent.isActiveAndEnabled || _agent.pathPending)
                return;

            // Если достиг цели – сброс
            if (_agent.remainingDistance <= _agent.stoppingDistance)
            {
                _stuckDuration = 0f;
                _stuckRetryCount = 0;
                _lastAgentPosition = transform.position;
                return;
            }

            float distanceMoved = Vector3.Distance(transform.position, _lastAgentPosition);
            if (distanceMoved < 0.1f) // практически не двигался
            {
                _stuckDuration += _stuckCheckInterval;
                if (_stuckDuration >= _stuckThreshold)
                {
                    HandleStuck();
                }
            }
            else
            {
                _stuckDuration = 0f;
                _stuckRetryCount = 0;
            }

            _lastAgentPosition = transform.position;
        }

        private void HandleStuck()
        {
            Debug.Log($"{name} is stuck! State: {_currentState}");

            if (_currentState == State.Patrol)
            {
                // Если точки заданы извне – просто переходим к следующей
                if (!_patrolPointsGenerated)
                {
                    GoToNextPatrolPoint();
                }
                else
                {
                    _stuckRetryCount++;
                    if (_stuckRetryCount >= _maxStuckRetries)
                    {
                        // Перегенерация точек
                        GeneratePatrolPoints();
                        _stuckRetryCount = 0;
                        Debug.Log("Regenerating patrol points due to repeated stuck.");
                    }
                    GoToNextPatrolPoint();
                }
            }
            // В состоянии Chase ничего не делаем – цель движется

            _stuckDuration = 0f;
        }

        private void GoToNextPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;
            _agent.SetDestination(patrolPoints[_currentPatrolIndex]);
            _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
        }

        private void PlayRandomRoar()
        {
            string[] roars = new string[] { "ShkilaRoar1", "ShkilaRoar2", "ShkilaRoar3", "ShkilaRoar4", "ShkilaRoar5" };
            string selected = roars[Random.Range(0, roars.Length)];

            _soundEmitterService?.PlaySound(selected, transform, 6);
        }

        private void OnDead(GameObject obj)
        {
            _dead = true;
            Destroy(_collider);
            Destroy(_agent);
            if (_animator != null)
            {
                _animator.enabled = false;
                Destroy(_animator);
            }

            StartCoroutine(DelayedRagdollForce());

            _soundEmitterService?.PlaySound("Death", transform, 5);

            var inventory = _playerFacade.Inventory;
            if (inventory != null && inventory.Weapons != null && inventory.Weapons.Count > 0)
            {
                int currentWeaponIndex = inventory.CurrentWeapon;
                if (currentWeaponIndex >= 0 && currentWeaponIndex < inventory.Weapons.Count)
                {
                    var currentWeapon = inventory.Weapons[currentWeaponIndex];
                    if (currentWeapon.weaponName == "SniperRifle")
                    {
                        string extraDeathSound = "GotSniped";
                        if (!_playerFacade.Jump.IsGrounded)
                            extraDeathSound = "GotSnipedInAir";

                        _soundEmitterService?.PlaySound(extraDeathSound, transform, 5);
                    }
                }
            }

            Transform[] fullObject = GetComponentsInChildren<Transform>();
            foreach (var item in fullObject)
            {
                item.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            }

            if (inventory != null && inventory.Weapons.Count > 0)
            {
                Weapon currentWeapon = inventory.Weapons[inventory.CurrentWeapon];
                // Спавним патроны только если оружие огнестрельное, резерв меньше максимума*2 и сработал шанс
                if (currentWeapon != null && inventory.HasNotMelee && ((!currentWeapon.IsMelee && currentWeapon.Reserve < currentWeapon.MaxAmmo * 2) || currentWeapon.IsMelee))
                {
                    int random = Random.Range(0, 100);
                    if (random <= _ammoSpawnChance)
                    {
                        var ammoRigidbody = _pooledSpawnService.SpawnObject(_weaponSettings.ammoPrefab, "Ammo", transform.position, transform.eulerAngles).GetComponent<Rigidbody>();
                        ammoRigidbody.MovePosition(transform.position);
                    }
                }
            }

            Invoke(nameof(FinalDestruction), 5f);
        }

        private IEnumerator DelayedRagdollForce()
        {
            yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
            
            Destroy(_objectsToClean);

            _ragdoll.SetActive(true);
            _ragdollRb.AddExplosionForce(20, Vector3.up, 10);

            _ragdollRenderer.material = _shkilaRenderer.material;
        }

        private void FinalDestruction()
        {
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);

            Vector3 leftBoundary = Quaternion.Euler(0, -_fieldOfView * 0.5f, 0) * transform.forward * _detectionRange;
            Vector3 rightBoundary = Quaternion.Euler(0, _fieldOfView * 0.5f, 0) * transform.forward * _detectionRange;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

            // Визуализация точек патруля
            if (patrolPoints != null)
            {
                Gizmos.color = Color.green;
                foreach (Vector3 point in patrolPoints)
                {
                    Gizmos.DrawSphere(point, 0.3f);
                }
            }
        }
    }
}