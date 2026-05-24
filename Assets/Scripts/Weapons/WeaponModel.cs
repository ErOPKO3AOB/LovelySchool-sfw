using Assets.Scripts.Data;
using Game;
using Player;
using UnityEngine;
using VContainer.Unity;

namespace Assets.Scripts.Weapons
{
    public abstract class WeaponModel : IInitializable, ITickable
    {
        public WeaponModel(WeaponView view,
            PlayerFacade playerFacade,
            GameSettingsSO gameSettings,
            WeaponConfigSO weaponConfig,
            PooledSpawnService pooledSpawnService)
        {
            this.view = view;
            this.playerFacade = playerFacade;
            this.gameSettings = gameSettings;
            this.weaponConfig = weaponConfig;
            this.pooledSpawnService = pooledSpawnService;
        }

        protected readonly WeaponView view;
        protected readonly PlayerFacade playerFacade;
        protected readonly GameSettingsSO gameSettings;
        protected readonly WeaponConfigSO weaponConfig;
        protected readonly PooledSpawnService pooledSpawnService;

        protected float cooldownTimer = 0;
        protected bool isAttackHeld;

        public virtual void Initialize()
        {
            cooldownTimer = 0;
        }

        public virtual void StartAttack()
        {
            // TODO: Перенести эту ответственность в инвентарь
            //if (_gameController.IsPaused || _playerFacade.LegController.IsKicking) return;

            // TODO: Инкапсулировать в классы Firearm и Melee
            if (weaponConfig.automatic)
            {
                isAttackHeld = true;
            }
        }

        public virtual void StopShoot()
        {
            isAttackHeld = false;
        }


        //private void PerformMelee()
        //{
        //    PlayShootAnimation();
        //    if (animator == null)
        //        ShootMelee();
        //}

        //public void ShootMelee()
        //{
        //    PhysicCast();
        //}

        public virtual void Tick()
        {
            if (cooldownTimer > 0f)
                cooldownTimer -= Time.deltaTime;

            if (isAttackHeld && !playerFacade.LegController.IsKicking)
            {
                if (view.WeaponConfig.automatic && cooldownTimer <= 0)
                {
                    cooldownTimer = weaponConfig.fireRate;
                    StartAttack();
                }
            }
        }

        public RaycastHit PhysicCast(Transform point, bool penetrateTargets)
        {
            Transform startPoint = point != null ? point : playerFacade.CameraFacade.transform;
            if (!penetrateTargets &&
                Physics.SphereCast(startPoint.position,
                0.25f,
                startPoint.forward,
                out RaycastHit hit,
                view.WeaponConfig.range,
                 gameSettings.targetsLayer))
            {
                return hit;
            }

            else
            {
                int maxHits = 10;
                RaycastHit[] hits = new RaycastHit[maxHits + 20];
                Physics.SphereCastNonAlloc(Camera.transform.position, 0.25f, Camera.transform.forward, hits, range, targetLayers);

                int currentHits = 0;
                float finalDamage = damage;
                if (!playerFacade.Jump.IsGrounded)
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

            return false;
        }
    }
}