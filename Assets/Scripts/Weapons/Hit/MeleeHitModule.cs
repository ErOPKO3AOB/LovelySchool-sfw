using Game;
using UnityEngine;

namespace Assets.Scripts.Weapons.Hit
{
    public sealed class MeleeHitModule : IHitModule
    {
        public MeleeHitModule(WeaponContext context)
        {
            _context = context;
        }

        private readonly WeaponContext _context;

        public void ProcessHit()
        {
            if (Physics.SphereCast(
                    _context.Camera.transform.position,
                    0.25f,
                    _context.Camera.transform.forward,
                    out RaycastHit hit,
                    _context.Config.range,
                    _context.TargetMask))
            {

                if (hit.collider.TryGetComponent(out Health health))
                {
                    int damage = _context.Config.damage;
                    if (_context.Player.Dash.IsDashing)
                    {
                        damage *= 3;
                    }

                    health.TakeDamage(damage);
                }
            }
        }
    }

}
