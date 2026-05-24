using Assets.Scripts.Weapons.Hit;
using UnityEngine;

namespace Assets.Scripts.Weapons.Attack
{
    public sealed class MeleeAttackModule : IAttackModule
    {
        public MeleeAttackModule(
            AnimationModule animation,
            IHitModule hit,
            WeaponConfigSO config)
        {
            _animation = animation;
            _hit = hit;
            _config = config;
        }

        private readonly AnimationModule _animation;
        private readonly IHitModule _hit;
        private readonly WeaponConfigSO _config;

        private float _cooldown;
        private bool _isHeld;


        public void StartAttack()
        {
            _isHeld = true;

            Attack();
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
                Attack();
            }
        }

        private void Attack()
        {
            if (_cooldown > 0)
                return;

            _cooldown = _config.meleeCooldown;

            _animation.PlayAttack();
            _hit.ProcessHit();
        }
    }

}
