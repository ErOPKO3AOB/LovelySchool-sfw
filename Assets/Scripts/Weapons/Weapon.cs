using Data;
using Player;
using System;
using UnityEngine;
using VContainer;

namespace Game
{
    [RequireComponent(typeof(Rigidbody))]
    public class Weapon : MonoBehaviour
    {
        [Inject]
        public void Construct(PooledSpawnService pooledSpawnService,
            WeaponSettingsSO weaponSettings,
            PlayerFacade playerFacade, 
            GameController gameController)
        {
            _pooledSpawnService = pooledSpawnService;
            _weaponSettingsSO = weaponSettings;
            _playerFacade = playerFacade;
            _gameController = gameController;
        }

        private PooledSpawnService _pooledSpawnService;
        private WeaponSettingsSO _weaponSettingsSO;
        private PlayerFacade _playerFacade;
        private GameController _gameController;

        public string weaponName;

        [Header("Аниматор")]
        public GameObject[] arms;
        [SerializeField] protected Animator animator;
        [SerializeField] protected string[] shootAnimNames;
        [SerializeField] protected string reloadAnimName = "Reload";
        [SerializeField] protected string pickupAnimName = "Pickup";  // анимация подбора
        [SerializeField] private Light _light;

        [Header("Боезапас")]
        [SerializeField] private int currentAmmoCapacity; // стартовый резерв
        [SerializeField] protected int maxAmmo = 10;
        [SerializeField] protected int ammo;              // текущие в магазине
        private int reserve;                               // патроны в запасе

        [Header("Стрельба")]
        [SerializeField] protected float damage = 10f;
        [SerializeField] protected float range = 100f;
        [SerializeField] protected float fireRate = 0f;
        [SerializeField] protected bool canHold;
        [SerializeField] protected float reloadTime = 1.5f;

        [Header("Снаряды")]
        [SerializeField] private Transform _projectileSpawnPoint;

        [Header("Ближний бой")]
        [SerializeField] protected bool isMelee;
        [SerializeField] protected float meleeRange = 2f;
        [SerializeField] protected float meleeCooldown = 0.8f;
        [SerializeField] protected bool meleeCanHold;

        [Header("Цели")]
        [SerializeField] protected LayerMask targetLayers = -1;

        private float shootCooldownTimer;
        private float meleeCooldownTimer;
        private float reloadTimer;
        protected bool isReloading;
        protected bool isActionHeld;

        public event Action<int, int, int> AmmoChanged; // текущие в магазине, максимум, резерв

        public Animator Animator => animator;
        public int CurrentAmmo => ammo;
        public int MaxAmmo => maxAmmo;
        public int Reserve => reserve;
        public bool IsMelee => isMelee;
        public bool IsReloading => isReloading;
        public Camera Camera { get; set; }

        private Rigidbody _rigidbody;
        public Rigidbody Rigidbody
        {
            get
            {
                if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
                return _rigidbody;
            }
        }
        
        public Light Light => _light;


        private void OnValidate()
        {
            weaponName = gameObject.name;
        }

        private void Start()
        {
            foreach (var item in arms)
            {
                item.SetActive(false);
            }

            reserve = currentAmmoCapacity > 0 ? currentAmmoCapacity : maxAmmo * 2;

            if (Rigidbody != null)
            {
                Rigidbody.isKinematic = false;
                Rigidbody.useGravity = true;
                Rigidbody.WakeUp();
                Rigidbody.constraints = RigidbodyConstraints.None;
            }
            else
            {
                Debug.LogError($"Weapon {name} has no Rigidbody!", this);
            }

            ammo = maxAmmo;
            shootCooldownTimer = 0f;
            meleeCooldownTimer = 0f;

            AmmoChanged?.Invoke(ammo, maxAmmo, reserve);

            Transform[] weaponChildren = GetComponentsInChildren<Transform>();
            foreach (var item in weaponChildren)
            {
                item.gameObject.layer = LayerMask.NameToLayer("ItemOutline");
            }
        }

        public void StartShoot()
        {
            if (_gameController.IsPaused || _playerFacade.LegController.IsKicking) return;

            if (isMelee)
            {
                if (meleeCanHold)
                {
                    isActionHeld = true;
                }
                else
                {
                    if (meleeCooldownTimer <= 0f)
                    {
                        meleeCooldownTimer = meleeCooldown;
                        PerformMelee();
                    }
                }
                return;
            }

            if (canHold)
                isActionHeld = true;
            else
                Shoot();
        }

        public void StopShoot()
        {
            isActionHeld = false;
        }

        private void Shoot()
        {
            if (isReloading) return;
            if (ammo <= 0)
            {
                Reload();
                return;
            }
            if (shootCooldownTimer > 0f) return;

            shootCooldownTimer = fireRate > 0 ? 1f / fireRate : 0.05f;
            ammo--;
            AmmoChanged?.Invoke(ammo, maxAmmo, reserve);

            PlayShootAnimation();
            PhysicCast();

            if (_projectileSpawnPoint != null)
            {
                Quaternion spawnRotation = _projectileSpawnPoint.rotation;
                Vector3 spawnPosition = _projectileSpawnPoint.position;

                GameObject projectile = _weaponSettingsSO.projectiles[0];
                if (weaponName == "SniperRifle") projectile = _weaponSettingsSO.projectiles[1];

                _pooledSpawnService.SpawnObject(
                    projectile,
                    projectile.name,
                    spawnPosition,
                    spawnRotation.eulerAngles,
                    _weaponSettingsSO.projectileLifetime);
            }
        }

        private void PerformMelee()
        {
            PlayShootAnimation();
            if (animator == null)
                ShootMelee();
        }

        public void ShootMelee()
        {
            PhysicCast();
        }

        private void PlayShootAnimation()
        {
            if (animator == null || shootAnimNames.Length == 0) return;
            if (!animator.isActiveAndEnabled)
                animator.enabled = true;

            string animName = shootAnimNames[UnityEngine.Random.Range(0, shootAnimNames.Length)];
            animator.CrossFade(animName, 0f, 0, 0f);
        }

        public void PlayPickupAnimation()
        {
            if (animator != null && !string.IsNullOrEmpty(pickupAnimName))
            {
                animator.CrossFade(pickupAnimName, 0f, 0, 0f);
            }
        }

        public void Reload()
        {
            if (isReloading || ammo >= maxAmmo || isMelee || reserve <= 0 || _playerFacade.LegController.IsKicking) return;

            isReloading = true;
            reloadTimer = 0f;
            if (animator != null && !string.IsNullOrEmpty(reloadAnimName))
                animator.Play(reloadAnimName);
        }

        /// <summary>
        /// Прерывает перезарядку, не меняя количества патронов. Сбрасывает анимацию в Idle.
        /// </summary>
        public void AbortReload()
        {
            if (!isReloading) return;
            isReloading = false;
            reloadTimer = 0f;
            if (animator != null && animator.isActiveAndEnabled && !string.IsNullOrEmpty(pickupAnimName))
            {
                animator.Play(pickupAnimName, 0, 0f);
            }
        }

        private void FinishReload()
        {
            if (reserve <= 0)
            {
                isReloading = false;
                return;
            }

            int needed = maxAmmo - ammo;
            int take = Mathf.Min(needed, reserve);
            ammo += take;
            reserve -= take;
            isReloading = false;
            AmmoChanged?.Invoke(ammo, maxAmmo, reserve);

            shootCooldownTimer = 0f;
            meleeCooldownTimer = 0f;
        }

        private void Update()
        {
            if (shootCooldownTimer > 0f)
                shootCooldownTimer -= Time.deltaTime;

            if (meleeCooldownTimer > 0f)
            {
                meleeCooldownTimer -= Time.deltaTime;
                if (meleeCooldownTimer < 0f) meleeCooldownTimer = 0f;
            }

            if (isReloading)
            {
                reloadTimer += Time.deltaTime;
                if (reloadTimer >= reloadTime)
                {
                    FinishReload();
                }
                return;
            }

            if (isActionHeld && !_playerFacade.LegController.IsKicking)
            {
                if (isMelee && meleeCanHold)
                {
                    if (meleeCooldownTimer <= 0f)
                    {
                        meleeCooldownTimer = meleeCooldown;
                        PerformMelee();
                    }
                }
                else if (!isMelee && canHold)
                {
                    Shoot();
                }
            }
        }

        public void AddReserve(int amount)
        {
            if (isMelee) return;
            reserve += amount;
            AmmoChanged?.Invoke(ammo, maxAmmo, reserve);
        }

        public void PhysicCast()
        {
            if (weaponName != "SniperRifle" && Physics.SphereCast(Camera.transform.position, 0.25f, Camera.transform.forward, out RaycastHit hit, range, targetLayers))
            {
                int finalDamage = Mathf.RoundToInt(damage);

                if (isMelee && _playerFacade.Dash.IsDashing)
                {
                    finalDamage *= 3;
                }

                if (hit.collider.TryGetComponent<Health>(out var health))
                {
                    health.TakeDamage(finalDamage);
                }
            }

            else
            {
                int maxHits = 10;
                RaycastHit[] hits = new RaycastHit[maxHits + 20];
                Physics.SphereCastNonAlloc(Camera.transform.position, 0.25f, Camera.transform.forward, hits, range, targetLayers);

                int currentHits = 0;
                float finalDamage = damage;
                if (!_playerFacade.Jump.IsGrounded)
                    finalDamage = damage * 4;
                foreach (var enemiesHit in hits)
                {
                    if (currentHits >= maxHits) break;

                    if (enemiesHit.collider != null && enemiesHit.collider.TryGetComponent<Health>(out var health))
                    {
                        health.TakeDamage(Mathf.RoundToInt(finalDamage));
                        currentHits++;
                    }
                }
            }
        }
    }
}