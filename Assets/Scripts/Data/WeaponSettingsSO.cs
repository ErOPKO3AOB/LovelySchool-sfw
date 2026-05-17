using UnityEngine;

namespace Data
{
    [CreateAssetMenu(menuName = "Settings/Weapon Settings")]
    public class WeaponSettingsSO : ScriptableObject
    {
        public GameObject ammoPrefab;
        public GameObject[] weaponPrefabs;
        public GameObject[] projectiles;
        public float projectileLifetime = 4f;
    }
}