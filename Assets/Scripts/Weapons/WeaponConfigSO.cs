using UnityEngine;

namespace Assets.Scripts.Weapons
{
    [CreateAssetMenu(menuName = "Weapons/Weapon")]
    public class WeaponConfigSO : ScriptableObject
    {
        public string weaponName;
        public GameObject weaponPrefab;

        [Header("Damage")]
        public int damage;
        public float range;

        [Header("Fire")]
        public float fireRate;
        public bool automatic;

        [Header("Animations")]
        public string[] attackAnimations;

        [Space(20)]

        [Header("Ammo")]
        public int maxAmmo;

        [Header("Reload")]
        public float reloadTime;

        [Header("Projectile")]
        public GameObject projectilePrefab;
        public float projectileLifetime;

        [Header("Sniper")]
        public bool penetrateTargets;

        private void OnValidate()
        {
            weaponName = weaponPrefab != null ? weaponPrefab.name : "???";
        }
    }
}
