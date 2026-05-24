using Game;
using UnityEngine;

namespace Assets.Scripts.Weapons.Hit
{
    public sealed class PenetratingHitModule : IHitModule
    {
        public PenetratingHitModule(WeaponContext context)
        {
            _context = context;
        }

        private readonly WeaponContext _context;

        public void ProcessHit()
        {
            const int maxHits = 10;

            RaycastHit[] hits = new RaycastHit[maxHits + 20];

            Physics.SphereCastNonAlloc(
                _context.Camera.transform.position,
                0.25f,
                _context.Camera.transform.forward,
                hits,
                _context.Config.range,
                _context.TargetMask);

            int currentHits = 0;

            foreach (var hit in hits)
            {
                if (currentHits >= maxHits)
                    break;

                if (hit.collider == null)
                    continue;

                if (hit.collider.TryGetComponent(out Health health))
                {
                    health.TakeDamage(_context.Config.damage);
                    currentHits++;
                }
            }
        }
    }

}
