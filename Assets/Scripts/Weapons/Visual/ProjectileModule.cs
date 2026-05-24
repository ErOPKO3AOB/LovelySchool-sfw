using Assets.Scripts.Weapons.Core;
using UnityEngine;

namespace Assets.Scripts.Weapons.Visual
{
    public sealed class ProjectileModule
    {
        public ProjectileModule(WeaponContext context)
        {
            _context = context;
        }

        private readonly WeaponContext _context;

        public void SpawnProjectile()
        {
            if (_context.Config.projectilePrefab == null)
                return;

            Transform point = _context.View.AttackPoint;

            if (point == null)
                return;

            _context.Pool.SpawnObject(
                _context.Config.projectilePrefab,
                _context.Config.projectilePrefab.name,
                point.position,
                point.rotation.eulerAngles,
                _context.Config.projectileLifetime);
        }
    }

}
