using Data;
using Game;
using Player;
using System.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace Assets.Scripts.Game
{
    public class Enemy : MonoBehaviour
    {
        #region Inject

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

        #endregion

        #region Dependencies

        private WeaponSettingsSO _weaponSettings;
        private PlayerFacade _playerFacade;
        public PlayerFacade PlayerFacade => _playerFacade;
        private PooledSpawnService _pooledSpawnService;
        private SoundEmitterService _soundEmitterService;

        #endregion

        #region Components

        [Header("Health")]
        [SerializeField] private Health _health;
        public Health Health => _health;

        [Header("Visual")]
        [SerializeField] private Animator _animator;
        [SerializeField] private Material _outlineMaterial;
        [SerializeField] private SkinnedMeshRenderer _renderer;
        [SerializeField] private Collider _collider;

        [Header("Ragdoll")]
        [SerializeField] private GameObject _ragdoll;
        [SerializeField] private Rigidbody _ragdollRb;
        [SerializeField] private GameObject _objectsToClean;
        [SerializeField] private SkinnedMeshRenderer _ragdollRenderer;

        [Header("Movement")]
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private float _rotationSpeed = 10f;
        public NavMeshAgent Agent => _agent;

        [Header("Death FX")]
        [SerializeField] private float _destroyDelay = 3f;
        [SerializeField] private float _shrinkDuration = 1.25f;
        [SerializeField]
        private AnimationCurve _shrinkCurve =
            AnimationCurve.EaseInOut(0, 1, 1, 0);

        [SerializeField] private float _randomTorque = 12f;
        [SerializeField] private float _upForce = 2f;

        #endregion

        #region AI

        [Header("Patrol")]
        [SerializeField] private int _patrolPointsCount = 5;
        [SerializeField] private float _patrolRadius = 12f;

        [Header("Vision")]
        [SerializeField] private float _detectionRange = 15f;
        [SerializeField] private float _fieldOfView = 90f;
        [SerializeField] private float _rearDetectionRange = 3f;
        [SerializeField] private float _visionCheckInterval = 0.25f;
        [SerializeField] private LayerMask _visionMask;
        [SerializeField] private float _lookAtPlayerDistance = 4f;

        private float _lookAtPlayerDistanceSqr;

        [Header("Attack")]
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _attackCooldown = 1f;
        [SerializeField] private int _attackDamage = 10;
        [SerializeField] private float _attackDelayTime = 0.2f;
        private float _attackDelayTimer = 0;
        private bool _canAttack = false;

        [Header("Optimization")]
        [SerializeField] private float _aiDistance = 40f;
        [SerializeField] private float _animDistance = 30f;
        [SerializeField] private float _destinationUpdateInterval = 0.5f;

        [Header("Memory")]
        [SerializeField] private float _memoryTime = 4f;

        #endregion

        #region Sound

        [Header("Roar")]
        [SerializeField] private float _roarMin = 5f;
        [SerializeField] private float _roarMax = 10f;

        private static readonly string[] ROARS =
        {
            "ShkilaRoar1",
            "ShkilaRoar2",
            "ShkilaRoar3",
            "ShkilaRoar4",
            "ShkilaRoar5"
        };

        #endregion

        #region Loot

        [Header("Loot")]
        [SerializeField, Range(0, 100)]
        private int _ammoSpawnChance = 30;

        #endregion

        #region State

        private enum State
        {
            Patrol,
            Chase
        }

        private State _state;

        private bool _dead;

        private float _forgetTimer;
        private float _visionTimer;
        private float _attackTimer;
        private float _destinationTimer;
        private float _roarTimer;

        private int _playerLayer;

        private Material _defaultMaterial;

        private Transform[] _cachedTransforms;

        public Vector3[] patrolPoints;
        private int _patrolIndex;

        private Vector3 _lastPlayerPosition;

        #endregion

        #region Cached Sqr

        private float _detectionRangeSqr;
        private float _attackRangeSqr;
        private float _aiDistanceSqr;
        private float _animDistanceSqr;
        private float _rearDetectionRangeSqr;

        #endregion

        #region Burst Job

        [BurstCompile]
        private struct VisionJob : IJob
        {
            public float3 EnemyPosition;
            public float3 EnemyForward;
            public float3 PlayerPosition;

            public float DetectionRangeSqr;
            public float RearDetectionRangeSqr;
            public float MinDot;

            public NativeArray<byte> Result;

            public void Execute()
            {
                float3 toPlayer =
                    PlayerPosition - EnemyPosition;

                float sqrDist =
                    math.lengthsq(toPlayer);

                // Слишком далеко
                if (sqrDist > DetectionRangeSqr)
                {
                    Result[0] = 0;
                    return;
                }

                // Игрок очень близко —
                // враг чувствует его даже со спины
                if (sqrDist <= RearDetectionRangeSqr)
                {
                    Result[0] = 1;
                    return;
                }

                float3 dir =
                    math.normalize(toPlayer);

                float dot =
                    math.dot(
                        EnemyForward,
                        dir);

                // Проверка угла обзора
                if (dot < MinDot)
                {
                    Result[0] = 0;
                    return;
                }

                Result[0] = 1;
            }
        }

        #endregion

        #region Unity

        private void Awake()
        {
            _lookAtPlayerDistanceSqr =
                _lookAtPlayerDistance * _lookAtPlayerDistance;

            _defaultMaterial =
                _renderer.material;

            _cachedTransforms =
                GetComponentsInChildren<Transform>(true);

            _playerLayer =
                LayerMask.NameToLayer("Player");

            _detectionRangeSqr =
                _detectionRange * _detectionRange;

            _attackRangeSqr =
                _attackRange * _attackRange;

            _aiDistanceSqr =
                _aiDistance * _aiDistance;

            _animDistanceSqr =
                _animDistance * _animDistance;

            _rearDetectionRangeSqr =
                _rearDetectionRange * _rearDetectionRange;
        }

        private void Start()
        {
            _health.ConstructManually(
                _pooledSpawnService);

            _health.OnDead += OnDead;

            SetupAgent();

            // Генерируем точки только если
            // вручную они не заданы
            if (patrolPoints == null ||
                patrolPoints.Length == 0)
            {
                GeneratePatrolPoints();
            }

            GoToNextPatrolPoint();

            _roarTimer =
                UnityEngine.Random.Range(
                    _roarMin,
                    _roarMax);
        }

        private void Update()
        {
            if (_dead)
                return;

            if (_playerFacade == null)
                return;

            Vector3 playerPos =
                _playerFacade.transform.position;

            Vector3 toPlayer =
                playerPos - transform.position;

            float sqrDistance =
                toPlayer.sqrMagnitude;

            // AI Distance Culling
            if (sqrDistance > _aiDistanceSqr)
            {
                DisableHeavySystems();
                return;
            }

            EnableHeavySystems();

            float dt = Time.deltaTime;

            _forgetTimer -= dt;
            _visionTimer -= dt;
            _attackTimer -= dt;
            _destinationTimer -= dt;
            _roarTimer -= dt;

            if (_attackDelayTimer > 0f)
                _attackDelayTimer -= dt;
            _canAttack = _attackDelayTimer <= 0f;

            // Animator LOD
            bool shouldAnimate =
                sqrDistance <= _animDistanceSqr;

            if (_animator.enabled != shouldAnimate)
            {
                _animator.enabled =
                    shouldAnimate;
            }

            // Vision
            if (_visionTimer <= 0f)
            {
                _visionTimer =
                    _visionCheckInterval;

                CheckVisionBurst(playerPos);
            }

            // State
            _state =
                _forgetTimer > 0f
                ? State.Chase
                : State.Patrol;

            switch (_state)
            {
                case State.Patrol:
                    RotateTowardsMovement();
                    UpdatePatrol();
                    break;

                case State.Chase:
                    UpdateChase(playerPos);
                    TryAttack(playerPos, sqrDistance);
                    break;
            }

            // Roar
            if (_roarTimer <= 0f)
            {
                PlayRandomRoar();

                _roarTimer =
                    UnityEngine.Random.Range(
                        _roarMin,
                        _roarMax);
            }
        }

        #endregion

        #region Setup

        private void SetupAgent()
        {
            if (_agent == null)
            {
                _agent =
                    GetComponent<NavMeshAgent>();
            }

            _agent.autoBraking = false;
            _agent.autoRepath = false;

            // ВАЖНО
            _agent.updateRotation = false;

            // HUGE WebGL optimization
            _agent.obstacleAvoidanceType =
                ObstacleAvoidanceType.LowQualityObstacleAvoidance;

            _agent.updateUpAxis = false;
        }

        #endregion

        #region Vision
        private void CheckVisionBurst(Vector3 playerPos)
        {
            NativeArray<byte> result =
                new NativeArray<byte>(
                    1,
                    Allocator.TempJob);

            VisionJob job = new VisionJob
            {
                EnemyPosition = transform.position,
                EnemyForward = transform.forward,
                PlayerPosition = playerPos,

                DetectionRangeSqr =
                    _detectionRangeSqr,

                RearDetectionRangeSqr =
                    _rearDetectionRangeSqr,

                MinDot =
                    Mathf.Cos(
                        (_fieldOfView * 0.5f)
                        * Mathf.Deg2Rad),

                Result = result
            };

            JobHandle handle =
                job.Schedule();

            handle.Complete();

            if (result[0] == 1)
            {
                Vector3 dir =
                    (playerPos - transform.position)
                    .normalized;

                if (Physics.Raycast(
                    transform.position + Vector3.up,
                    dir,
                    out RaycastHit hit,
                    _detectionRange,
                    _visionMask,
                    QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.gameObject.layer ==
                        _playerLayer)
                    {
                        _forgetTimer =
                            _memoryTime;
                    }
                }
            }

            result.Dispose();
        }

        #endregion

        #region Patrol

        private void GeneratePatrolPoints()
        {
            patrolPoints =
                new Vector3[_patrolPointsCount];

            for (int i = 0;
                 i < _patrolPointsCount;
                 i++)
            {
                Vector3 random =
                    UnityEngine.Random.insideUnitSphere
                    * _patrolRadius;

                random += transform.position;

                if (NavMesh.SamplePosition(
                    random,
                    out NavMeshHit hit,
                    _patrolRadius,
                    NavMesh.AllAreas))
                {
                    patrolPoints[i] =
                        hit.position;
                }
                else
                {
                    patrolPoints[i] =
                        transform.position;
                }
            }
        }

        public void RefreshPatrol()
        {
            _patrolIndex = 0;

            if (patrolPoints != null &&
                patrolPoints.Length > 0)
            {
                _agent.SetDestination(
                    patrolPoints[0]);
            }
        }

        private void UpdatePatrol()
        {
            if (_agent.pathPending)
                return;

            if (_agent.remainingDistance <=
                _agent.stoppingDistance)
            {
                GoToNextPatrolPoint();
            }
        }

        private void GoToNextPatrolPoint()
        {
            if (patrolPoints == null)
                return;

            _agent.SetDestination(
                patrolPoints[_patrolIndex]);

            _patrolIndex++;

            if (_patrolIndex >=
                patrolPoints.Length)
            {
                _patrolIndex = 0;
            }
        }

        #endregion

        #region Chase

        private void RotateTowardsMovement()
        {
            Vector3 velocity =
                _agent.velocity;

            velocity.y = 0f;

            if (velocity.sqrMagnitude < 0.01f)
                return;

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    velocity.normalized);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    _rotationSpeed * Time.deltaTime);
        }

        private void UpdateChase(Vector3 playerPos)
        {
            Vector3 toPlayer =
                playerPos - transform.position;

            toPlayer.y = 0f;

            float sqrDistance =
                toPlayer.sqrMagnitude;

            // Смотрим на игрока
            // только если он очень близко
            if (sqrDistance <= _lookAtPlayerDistanceSqr)
            {
                if (toPlayer.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation =
                        Quaternion.LookRotation(
                            toPlayer.normalized);

                    transform.rotation =
                        Quaternion.Slerp(
                            transform.rotation,
                            targetRotation,
                            _rotationSpeed * Time.deltaTime);
                }
            }
            // Иначе смотрим по направлению движения
            else
            {
                RotateTowardsMovement();
            }

            if (_destinationTimer > 0f)
                return;

            _destinationTimer =
                _destinationUpdateInterval;

            if ((playerPos - _lastPlayerPosition)
                .sqrMagnitude < 1f)
            {
                return;
            }

            _lastPlayerPosition =
                playerPos;

            _agent.SetDestination(playerPos);
        }

        #endregion

        #region Attack

        private void TryAttack(
            Vector3 playerPos,
            float sqrDistance)
        {
            if (_attackTimer > 0f || !_canAttack)
                return;

            if (sqrDistance >
                _attackRangeSqr)
            {
                return;
            }

            Vector3 toPlayer =
                playerPos - transform.position;

            // Игнорируем высоту
            Vector3 flatDir =
                new Vector3(
                    toPlayer.x,
                    0f,
                    toPlayer.z).normalized;

            Vector3 flatForward =
                new Vector3(
                    transform.forward.x,
                    0f,
                    transform.forward.z).normalized;

            // Проверяем что игрок не сзади
            float dot =
                Vector3.Dot(
                    flatForward,
                    flatDir);

            if (dot < 0f)
                return;

            // Полное направление оставляем для рейкаста,
            // чтобы вверх/вниз продолжало работать
            Vector3 dir =
                toPlayer.normalized;

            if (Physics.Raycast(
                transform.position + Vector3.up,
                dir,
                out RaycastHit hit,
                _attackRange,
                _visionMask,
                QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject.layer ==
                    _playerLayer)
                {
                    Health health =
                        _playerFacade
                        .GetComponent<Health>();

                    if (health != null)
                    {
                        health.TakeDamage(
                            _attackDamage);

                        _attackTimer =
                            _attackCooldown;
                        _attackDelayTimer = _attackDelayTime;

                        if (_animator.enabled)
                        {
                            _animator
                                .SetTrigger("Attack");
                        }
                    }
                }
            }
        }

        #endregion

        #region Optimization

        private void DisableHeavySystems()
        {
            if (_animator.enabled)
            {
                _animator.enabled = false;
            }

            if (_agent.enabled)
            {
                _agent.enabled = false;
            }
        }

        private void EnableHeavySystems()
        {
            if (!_animator.enabled)
            {
                _animator.enabled = true;
            }

            if (!_agent.enabled)
            {
                _agent.enabled = true;
            }
        }

        #endregion

        #region Visual

        public void SetOutlined(bool value)
        {
            _renderer.material =
                value
                ? _outlineMaterial
                : _defaultMaterial;

            int layer =
                LayerMask.NameToLayer(
                    value
                    ? "OutlinedEnemy"
                    : "Enemy");

            for (int i = 0;
                 i < _cachedTransforms.Length;
                 i++)
            {
                _cachedTransforms[i]
                    .gameObject.layer = layer;
            }
        }

        #endregion

        #region Sound

        private void PlayRandomRoar()
        {
            string clip =
                ROARS[
                    UnityEngine.Random.Range(
                        0,
                        ROARS.Length)];

            _soundEmitterService?.PlaySound(
                clip,
                transform,
                6);
        }

        #endregion

        #region Death

        private void OnDead(GameObject obj)
        {
            if (_dead)
                return;

            _dead = true;

            if (_agent != null)
            {
                _agent.enabled = false;
            }

            if (_animator != null)
            {
                _animator.enabled = false;
            }

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            for (int i = 0;
                 i < _cachedTransforms.Length;
                 i++)
            {
                _cachedTransforms[i]
                    .gameObject.layer =
                    LayerMask.NameToLayer(
                        "Ignore Raycast");
            }

            StartCoroutine(
                DelayedRagdoll());

            _soundEmitterService?.PlaySound(
                "Death",
                transform,
                5);

            TrySpawnAmmo();
        }

        private IEnumerator DelayedRagdoll()
        {
            yield return new WaitForSeconds(
                UnityEngine.Random.Range(
                    0.05f,
                    0.15f));

            if (_objectsToClean != null)
            {
                Destroy(_objectsToClean);
            }

            _ragdoll.SetActive(true);

            _ragdollRenderer.material =
                _renderer.material;

            // Рандомный поворот
            _ragdoll.transform.rotation =
                UnityEngine.Random.rotation;

            // Случайное вращение
            Vector3 randomTorque =
                UnityEngine.Random.insideUnitSphere
                * _randomTorque;

            _ragdollRb.AddTorque(
                randomTorque,
                ForceMode.Impulse);

            // Немного подбрасываем
            _ragdollRb.AddExplosionForce(
                20f,
                transform.position +
                UnityEngine.Random.insideUnitSphere,
                10f,
                _upForce,
                ForceMode.Impulse);

            // Ждем перед исчезновением
            yield return new WaitForSeconds(
                _destroyDelay);

            yield return StartCoroutine(
                ShrinkAndDestroy());
        }

        private IEnumerator ShrinkAndDestroy()
        {
            Transform target =
                _ragdoll.transform;

            Vector3 startScale =
                target.localScale;

            float time = 0f;

            while (time < _shrinkDuration)
            {
                time += Time.deltaTime;

                float t =
                    time / _shrinkDuration;

                float scale =
                    _shrinkCurve.Evaluate(t);

                target.localScale =
                    startScale * scale;

                yield return null;
            }

            Destroy(gameObject);
        }

        #endregion

        #region Loot

        private void TrySpawnAmmo()
        {
            var inventory =
                _playerFacade.Inventory;

            if (inventory == null)
                return;

            if (inventory.Weapons == null)
                return;

            if (inventory.Weapons.Count == 0)
                return;

            Weapon weapon =
                inventory.Weapons[
                    inventory.CurrentWeapon];

            if (weapon == null)
                return;

            bool shouldSpawn =
                inventory.HasNotMelee &&
                ((!weapon.IsMelee &&
                  weapon.Reserve <
                  weapon.MaxAmmo * 2)
                 || weapon.IsMelee);

            if (!shouldSpawn)
                return;

            int rnd =
                UnityEngine.Random.Range(
                    0,
                    100);

            if (rnd > _ammoSpawnChance)
                return;

            Rigidbody ammo =
                _pooledSpawnService
                .SpawnObject(
                    _weaponSettings.ammoPrefab,
                    "Ammo",
                    new Vector3(transform.position.x,
                        transform.position.y + 0.25f,
                        transform.position.z
                        ),
                    transform.eulerAngles)
                .GetComponent<Rigidbody>();

            ammo.MovePosition(
                transform.position);
        }

        #endregion

#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(
                transform.position,
                _detectionRange);

            Vector3 left =
                Quaternion.Euler(
                    0,
                    -_fieldOfView * 0.5f,
                    0)
                * transform.forward
                * _detectionRange;

            Vector3 right =
                Quaternion.Euler(
                    0,
                    _fieldOfView * 0.5f,
                    0)
                * transform.forward
                * _detectionRange;

            Gizmos.color = Color.cyan;

            Gizmos.DrawLine(
                transform.position,
                transform.position + left);

            Gizmos.DrawLine(
                transform.position,
                transform.position + right);
        }

#endif
    }
}