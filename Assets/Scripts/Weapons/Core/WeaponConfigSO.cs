using UnityEngine;

namespace Assets.Scripts.Weapons.Core
{
    public enum WeaponAttackType
    {
        SemiAuto,
        FullAuto,
        Melee
    }

    public enum HitType
    {
        Single,
        Penetrating,
        Melee
    }

    [CreateAssetMenu(menuName = "Weapons/Weapon Config")]
    public class WeaponConfigSO : ScriptableObject
    {
        [Header("Info")]
        public string weaponName;
        public GameObject weaponPrefab;

        [Header("Types")]
        public WeaponAttackType attackType;
        public HitType hitType;

        [Header("Damage")]
        public int damage = 10;
        public float range = 100f;

        [Header("Fire")]
        public float fireRate = 5f;

        [Header("Ammo")]
        public int maxAmmo = 10;
        public int startingReserveAmmo = 30;

        [Header("Reload")]
        public float reloadTime = 1.5f;

        [Header("Projectile")]
        public GameObject projectilePrefab;
        public float projectileLifetime = 5f;

        [Header("Melee")]
        public float meleeCooldown = 0.7f;

        [Header("Animations")]
        public string[] attackAnimations;
        public string reloadAnimation = "Reload";
        public string pickupAnimation = "Pickup";
    }
}
