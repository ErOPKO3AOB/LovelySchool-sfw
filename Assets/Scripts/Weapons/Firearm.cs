using Assets.Scripts.Data;
using Game;
using Player;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.Weapons
{
    public class Firearm : WeaponModel
    {
        public Firearm(WeaponView view,
            PlayerFacade playerFacade,
            GameSettingsSO gameSettings,
            WeaponConfigSO weaponConfig,
            PooledSpawnService pooledSpawnService)
            : base(view, 
                  playerFacade, 
                  gameSettings, 
                  weaponConfig, 
                  pooledSpawnService)
        {

        }


        private int _currentAmmoCapacity;
        private int _ammo;
        private int _reserve;

        private bool _isReloading;
        private float _reloadTimer;

        public UnityAction<int, int, int> AmmoChanged;

        public override void Initialize()
        {
            base.Initialize();

            _reserve = _currentAmmoCapacity > 0 ? _currentAmmoCapacity : weaponConfig.maxAmmo * 2;
            _ammo = weaponConfig.maxAmmo;
            AmmoChanged?.Invoke(_ammo, weaponConfig.maxAmmo, _reserve);
        }

        public override void Tick()
        {
            base.Tick();

            if (_isReloading)
            {
                _reloadTimer += Time.deltaTime;
                if (_reloadTimer >= weaponConfig.reloadTime)
                {
                    FinishReload();
                }
                return;
            }
        }

        public override void StartAttack()
        {
            base.StartAttack();

            Shoot();
        }

        private void Shoot()
        {
            if (_isReloading) return;
            if (_ammo <= 0)
            {
                Reload();
                return;
            }
            if (cooldownTimer > 0f) return;

            cooldownTimer = weaponConfig.fireRate > 0 ? 1f / weaponConfig.fireRate : 0.05f;
            _ammo--;
            AmmoChanged?.Invoke(_ammo, weaponConfig.maxAmmo, _reserve);

            Transform attackPoint = view.AttackPoint;

            view.PlayAttackAnimation();
            var hit = PhysicCast(attackPoint, weaponConfig.penetrateTargets);

            // TODO: Перенести этот блок в Melee
            //if (isMelee && playerFacade.Dash.IsDashing)
            //{
            //    finalDamage *= 3;
            //}

            if (hit.collider.TryGetComponent<Health>(out var health))
            {
                health.TakeDamage(weaponConfig.damage);
            }

            if (attackPoint != null)
            {
                Quaternion spawnRotation = attackPoint.rotation;
                Vector3 spawnPosition = attackPoint.position;

                GameObject projectile = weaponConfig.projectilePrefab;

                pooledSpawnService.SpawnObject(
                    projectile,
                    projectile.name,
                    spawnPosition,
                    spawnRotation.eulerAngles,
                    weaponConfig.projectileLifetime);
            }
        }

        #region Reloading
        public void Reload()
        {
            if (_isReloading || _ammo >= weaponConfig.maxAmmo || _reserve <= 0 /*|| _playerFacade.LegController.IsKicking*/) return;

            _isReloading = true;
            _reloadTimer = 0f;
            view.PlayReloadAnimation();
        }

        public void AbortReload()
        {
            if (!_isReloading) return;

            _isReloading = false;
            _reloadTimer = 0f;

            view.PlayPickupAnimation();
        }

        private void FinishReload()
        {
            if (_reserve <= 0)
            {
                _isReloading = false;
                return;
            }

            int needed = weaponConfig.maxAmmo - _ammo;
            int take = Mathf.Min(needed, _reserve);
            _ammo += take;
            _reserve -= take;
            _isReloading = false;
            AmmoChanged?.Invoke(_ammo, weaponConfig.maxAmmo, _reserve);

            cooldownTimer = 0f;
        }

        public void AddReserve(int amount)
        {
            _reserve += amount;
            AmmoChanged?.Invoke(_ammo, weaponConfig.maxAmmo, _reserve);
        }
        #endregion
    }
}