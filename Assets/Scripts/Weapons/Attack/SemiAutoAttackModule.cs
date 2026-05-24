using Assets.Scripts.Weapons.Hit;
using UnityEngine;

namespace Assets.Scripts.Weapons.Attack
{
    public sealed class SemiAutoAttackModule : IAttackModule
    {
        public SemiAutoAttackModule(
            AmmoModule ammo,
            AnimationModule animation,
            IHitModule hit,
            ProjectileModule projectile,
            WeaponConfigSO config)
        {
            _ammo = ammo;
            _animation = animation;
            _hit = hit;
            _projectile = projectile;
            _config = config;
        }

        private readonly AmmoModule _ammo;
        private readonly AnimationModule _animation;
        private readonly IHitModule _hit;
        private readonly ProjectileModule _projectile;
        private readonly WeaponConfigSO _config;

        private float _cooldown;

        public void StartAttack()
        {
            Shoot();
        }

        public void StopAttack()
        {
        }

        public void Tick()
        {
            if (_cooldown > 0)
            {
                _cooldown -= Time.deltaTime;
            }
        }

        private void Shoot()
        {
            if (_cooldown > 0)
                return;

            if (!_ammo.TryConsume())
                return;

            _cooldown = 1f / _config.fireRate;

            _animation.PlayAttack();
            _hit.ProcessHit();
            _projectile.SpawnProjectile();
        }
    }

}
