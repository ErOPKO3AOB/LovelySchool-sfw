using Assets.Scripts.Weapons.Hit;
using UnityEngine;

namespace Assets.Scripts.Weapons.Attack
{
    public sealed class FullAutoAttackModule : IAttackModule
    {
        public FullAutoAttackModule(
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

        private bool _isHeld;
        private float _cooldown;

        public void StartAttack()
        {
            _isHeld = true;
        }

        public void StopAttack()
        {
            _isHeld = false;
        }

        public void Tick()
        {
            if (_cooldown > 0)
            {
                _cooldown -= Time.deltaTime;
            }

            if (_isHeld && _cooldown <= 0)
            {
                Shoot();
            }
        }

        private void Shoot()
        {
            if (!_ammo.TryConsume())
                return;

            _cooldown = 1f / _config.fireRate;

            _animation.PlayAttack();
            _hit.ProcessHit();
            _projectile.SpawnProjectile();
        }
    }

}
