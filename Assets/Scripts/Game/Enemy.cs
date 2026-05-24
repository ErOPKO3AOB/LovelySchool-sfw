using Data;
using Game;
using Player;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace Assets.Scripts.Game
{
    public class Enemy : MonoBehaviour
    {
        [Inject]
        public void Construct(
            WeaponSettingsSO weaponSettings,
            PlayerFacade playerFacade,
            PooledSpawnService pooledSpawnService,
            SoundEmitterService soundEmitterService)
        {
            _weaponSettings = weaponSettings;
            _playerFacade = playerFacade;
            _pooledSpawnService = pooledSpawnService;
            _soundEmitterService = soundEmitterService;
        }

        private WeaponSettingsSO _weaponSettings;
        private PlayerFacade _playerFacade;
        private PooledSpawnService _pooledSpawnService;
        private SoundEmitterService _soundEmitterService;

        [Header("Health")]
        [SerializeField] private Health _health;
        public Health Health => _health;

        [Header("Visual")]
        [SerializeField] private Material _outlineMaterial;
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _ragdoll;
        [SerializeField] private Rigidbody _ragdollRb;
        [SerializeField] private GameObject _objectsToClean;
        [SerializeField] private SkinnedMeshRenderer _shkilaRenderer;
        [SerializeField] private SkinnedMeshRenderer _ragdollRenderer;
        [SerializeField] private Collider _collider;

        [Header("Movement")]
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private int _patrolPointsCount = 5;
        [SerializeField] private float _patrolRadius = 10f;

        [Header("Vision")]
        [SerializeField] private float _detectionRange = 15f;
        [SerializeField] private float _fieldOfView = 90f;
        [SerializeField] private float _rearDetectionRange = 2f;
        [SerializeField] private float _visionCheckInterval = 0.35f;
        [SerializeField] private LayerMask _obstacleMask;

        [Header("AI Optimization")]
        [SerializeField] private float _aiActiveDistance = 45f;
        [SerializeField] private float _destinationUpdateInterval = 0.5f;
        [SerializeField] private float _animDisableDistance = 35f;

        [Header("Memory")]
        [SerializeField] private float _memoryTime = 5f;

        [Header("Attack")]
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _attackCooldown = 1f;
        [SerializeField] private float _attackStartDelay = 0.25f;
        [SerializeField] private int _attackDamage = 10;

        [Header("Sounds")]
        [SerializeField] private float _roarIntervalMin = 5f;
        [SerializeField] private float _roarIntervalMax = 12f;

        [Header("Loot")]
        [SerializeField, Range(0, 100)]
        private int _ammoSpawnChance = 30;

        public Vector3[] patrolPoints;
        public PlayerFacade PlayerFacade => _playerFacade;

        private enum State
        {
            Patrol,
            Chase
        }

        private State _currentState;

        private static readonly string[] ROARS =
        {
            "ShkilaRoar1",
            "ShkilaRoar2",
            "ShkilaRoar3",
            "ShkilaRoar4",
            "ShkilaRoar5"
        };

        private static int _globalTick;

        private Transform[] _cachedTransforms;
        private Material _defaultMaterial;

        private int _currentPatrolIndex;
        private int _playerLayer;
        private int _myTick;

        private bool _dead;

        private float _forgetTimer;
        private float _attackTimer;
        private float _attackReadyTimer;
        private float _visionTimer;
        private float _roarTimer;
        private float _destinationTimer;

        private Vector3 _lastPlayerPosition;

        private float DetectionRangeSqr => _detectionRange * _detectionRange;
        private float AttackRangeSqr => _attackRange * _attackRange;
        private float AiDistanceSqr => _aiActiveDistance * _aiActiveDistance;
        private float AnimDistanceSqr => _animDisableDistance * _animDisableDistance;

        public NavMeshAgent Agent => _agent;

        private void OnValidate()
        {
            gameObject.layer = LayerMask.NameToLayer("Enemy");
        }

        private void Start()
        {
            _myTick = _globalTick++;

            _defaultMaterial = _shkilaRenderer.material;

            _cachedTransforms = GetComponentsInChildren<Transform>(true);

            _playerLayer = LayerMask.NameToLayer("Player");

            _health.ConstructManually(_pooledSpawnService);
            _health.OnDead += OnDead;

            if (_agent == null)
                _agent = GetComponent<NavMeshAgent>();

            SetupAgent();

            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                GeneratePatrolPoints();
            }

            GoToNextPatrolPoint();

            _roarTimer = Random.Range(_roarIntervalMin, _roarIntervalMax);

            _currentState = State.Patrol;
        }

        private void SetupAgent()
        {
            _agent.autoBraking = false;
            _agent.autoRepath = false;
            _agent.obstacleAvoidanceType =
                ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        }

        private void Update()
        {
            if (_dead || _playerFacade == null)
                return;

            Vector3 playerPos = _playerFacade.transform.position;

            Vector3 toPlayer = playerPos - transform.position;
            float sqrDistanceToPlayer = toPlayer.sqrMagnitude;

            // Distance Culling
            if (sqrDistanceToPlayer > AiDistanceSqr)
            {
                if (_animator.enabled)
                    _animator.enabled = false;

                return;
            }

            // Animator LOD
            bool shouldAnimate = sqrDistanceToPlayer <= AnimDistanceSqr;

            if (_animator.enabled != shouldAnimate)
            {
                _animator.enabled = shouldAnimate;
            }

            float dt = Time.deltaTime;

            _forgetTimer -= dt;
            _attackTimer -= dt;
            _visionTimer -= dt;
            _destinationTimer -= dt;
            _roarTimer -= dt;

            if (_attackReadyTimer > 0f)
                _attackReadyTimer -= dt;

            // Tick AI
            if (Time.frameCount % 5 == _myTick % 5)
            {
                if (_visionTimer <= 0f)
                {
                    _visionTimer = _visionCheckInterval;
                    CheckVision(playerPos, toPlayer, sqrDistanceToPlayer);
                }
            }

            State previousState = _currentState;

            _currentState =
                _forgetTimer > 0f
                ? State.Chase
                : State.Patrol;

            if (previousState != _currentState &&
                _currentState == State.Chase)
            {
                _attackReadyTimer = _attackStartDelay;
            }

            switch (_currentState)
            {
                case State.Patrol:
                    UpdatePatrol();
                    break;

                case State.Chase:
                    UpdateChase(playerPos);
                    break;
            }

            if (_roarTimer <= 0f)
            {
                PlayRandomRoar();

                _roarTimer =
                    Random.Range(_roarIntervalMin, _roarIntervalMax);
            }

            if (_currentState == State.Chase)
            {
                TryAttack(playerPos, sqrDistanceToPlayer);
            }
        }

        private void UpdatePatrol()
        {
            if (_agent.pathPending)
                return;

            if (_agent.remainingDistance <= _agent.stoppingDistance)
            {
                GoToNextPatrolPoint();
            }
        }

        private void UpdateChase(Vector3 playerPos)
        {
            if (_destinationTimer > 0f)
                return;

            _destinationTimer = _destinationUpdateInterval;

            if ((playerPos - _lastPlayerPosition).sqrMagnitude < 1f)
                return;

            _lastPlayerPosition = playerPos;

            _agent.SetDestination(playerPos);
        }

        private void CheckVision(
            Vector3 playerPos,
            Vector3 toPlayer,
            float sqrDistance)
        {
            if (sqrDistance > DetectionRangeSqr)
                return;

            Vector3 dir = toPlayer.normalized;

            float dot = Vector3.Dot(transform.forward, dir);

            float minDot =
                Mathf.Cos((_fieldOfView * 0.5f) * Mathf.Deg2Rad);

            if (sqrDistance >
                _rearDetectionRange * _rearDetectionRange)
            {
                if (dot < minDot)
                    return;
            }

            if (Physics.Raycast(
                transform.position + Vector3.up,
                dir,
                out RaycastHit hit,
                _detectionRange,
                _obstacleMask,
                QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject.layer == _playerLayer)
                {
                    _forgetTimer = _memoryTime;
                }
            }
        }

        private void TryAttack(
            Vector3 playerPos,
            float sqrDistance)
        {
            if (_attackReadyTimer > 0f)
                return;

            if (_attackTimer > 0f)
                return;

            if (sqrDistance > AttackRangeSqr)
                return;

            Vector3 dir =
                (playerPos - transform.position).normalized;

            if (Physics.Raycast(
                transform.position + Vector3.up,
                dir,
                out RaycastHit hit,
                _attackRange,
                _obstacleMask,
                QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject.layer == _playerLayer)
                {
                    if (_playerFacade.TryGetComponent<Health>(
                        out var playerHealth))
                    {
                        playerHealth.TakeDamage(_attackDamage);

                        _attackTimer = _attackCooldown;

                        if (_animator.enabled)
                        {
                            _animator.SetTrigger("Attack");
                        }
                    }
                }
            }
        }

        private void GeneratePatrolPoints()
        {
            patrolPoints = new Vector3[_patrolPointsCount];

            for (int i = 0; i < _patrolPointsCount; i++)
            {
                Vector3 randomDirection =
                    Random.insideUnitSphere * _patrolRadius;

                randomDirection += transform.position;

                if (NavMesh.SamplePosition(
                    randomDirection,
                    out NavMeshHit hit,
                    _patrolRadius,
                    NavMesh.AllAreas))
                {
                    patrolPoints[i] = hit.position;
                }
                else
                {
                    patrolPoints[i] = transform.position;
                }
            }
        }

        private void GoToNextPatrolPoint()
        {
            if (patrolPoints == null ||
                patrolPoints.Length == 0)
                return;

            _agent.SetDestination(
                patrolPoints[_currentPatrolIndex]);

            _currentPatrolIndex++;

            if (_currentPatrolIndex >= patrolPoints.Length)
            {
                _currentPatrolIndex = 0;
            }
        }

        public void SetOutlined(bool value)
        {
            _shkilaRenderer.material =
                value ? _outlineMaterial : _defaultMaterial;

            int layer =
                LayerMask.NameToLayer(
                    value ? "OutlinedEnemy" : "Enemy");

            for (int i = 0; i < _cachedTransforms.Length; i++)
            {
                _cachedTransforms[i].gameObject.layer = layer;
            }
        }

        private void PlayRandomRoar()
        {
            string selected =
                ROARS[Random.Range(0, ROARS.Length)];

            _soundEmitterService?.PlaySound(
                selected,
                transform,
                6);
        }

        private void OnDead(GameObject obj)
        {
            if (_dead)
                return;

            _dead = true;

            _agent.enabled = false;

            if (_animator != null)
            {
                _animator.enabled = false;
            }

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            for (int i = 0; i < _cachedTransforms.Length; i++)
            {
                _cachedTransforms[i].gameObject.layer =
                    LayerMask.NameToLayer("Ignore Raycast");
            }

            StartCoroutine(DelayedRagdollForce());

            _soundEmitterService?.PlaySound(
                "Death",
                transform,
                5);

            TrySpawnAmmo();

            Destroy(gameObject, 5f);
        }

        private void TrySpawnAmmo()
        {
            var inventory = _playerFacade.Inventory;

            if (inventory == null)
                return;

            if (inventory.Weapons == null ||
                inventory.Weapons.Count == 0)
                return;

            Weapon currentWeapon =
                inventory.Weapons[inventory.CurrentWeapon];

            if (currentWeapon == null)
                return;

            bool shouldSpawn =
                inventory.HasNotMelee &&
                ((!currentWeapon.IsMelee &&
                 currentWeapon.Reserve <
                 currentWeapon.MaxAmmo * 2)
                 || currentWeapon.IsMelee);

            if (!shouldSpawn)
                return;

            int random = Random.Range(0, 100);

            if (random > _ammoSpawnChance)
                return;

            var ammoRb =
                _pooledSpawnService
                .SpawnObject(
                    _weaponSettings.ammoPrefab,
                    "Ammo",
                    transform.position,
                    transform.eulerAngles)
                .GetComponent<Rigidbody>();

            ammoRb.MovePosition(transform.position);
        }

        private IEnumerator DelayedRagdollForce()
        {
            yield return new WaitForSeconds(
                Random.Range(0.05f, 0.2f));

            if (_objectsToClean != null)
            {
                Destroy(_objectsToClean);
            }

            _ragdoll.SetActive(true);

            _ragdollRenderer.material =
                _shkilaRenderer.material;

            _ragdollRb.AddExplosionForce(
                20,
                Vector3.up,
                10);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(
                transform.position,
                _detectionRange);

            Vector3 leftBoundary =
                Quaternion.Euler(
                    0,
                    -_fieldOfView * 0.5f,
                    0)
                * transform.forward
                * _detectionRange;

            Vector3 rightBoundary =
                Quaternion.Euler(
                    0,
                    _fieldOfView * 0.5f,
                    0)
                * transform.forward
                * _detectionRange;

            Gizmos.color = Color.cyan;

            Gizmos.DrawLine(
                transform.position,
                transform.position + leftBoundary);

            Gizmos.DrawLine(
                transform.position,
                transform.position + rightBoundary);

            if (patrolPoints == null)
                return;

            Gizmos.color = Color.green;

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Gizmos.DrawSphere(patrolPoints[i], 0.3f);
            }
        }
#endif
    }
}