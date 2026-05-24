using Assets.Scripts.Weapons.Core;
using System;
using UnityEngine;
using VContainer;

namespace Game
{
    public class RandomPointItemsSpawner : MonoBehaviour
    {
        [Inject]
        public void Construct(WeaponFactory weaponFactory)
        {
            _weaponFactory = weaponFactory;
        }

        private WeaponFactory _weaponFactory;

        [Serializable]
        public class ObjectSettings
        {
            public WeaponConfigSO weapon;
            public int objectIndex;
            public Transform[] spawnPoints;
        }


        [SerializeField] private ObjectSettings[] _settings;

        private void Start()
        {
            for (int i = 0; i < _settings.Length; i++)
            {
                Transform spawnPoint = _settings[i].spawnPoints[UnityEngine.Random.Range(0, _settings[i].spawnPoints.Length)];
                _weaponFactory.Create(
                    _settings[i].weapon,
                    spawnPoint.position,
                    spawnPoint.rotation
                );
            }
        }
    }
}