using Game;
using UnityEngine;

namespace Assets.Scripts.Weapons.Hit
{
    public sealed class SingleHitModule : IHitModule
    {
        public SingleHitModule(WeaponContext context)
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
                int damage = _context.Config.damage;

                if (hit.collider.TryGetComponent(out Health health))
                {
                    health.TakeDamage(damage);
                }
            }
        }
    }
}
