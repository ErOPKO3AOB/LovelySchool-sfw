using Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VContainer;

namespace Game
{
    public class RandomPointItemsSpawner : MonoBehaviour
    {
        [Inject]
        public void Construct(Func<Transform, GameObject, Weapon> weaponFactory, WeaponSettingsSO weaponSettings)
        {
            _weaponFactory = weaponFactory;
            _weaponSettings = weaponSettings;
        }

        private Func<Transform, GameObject, Weapon> _weaponFactory;
        private WeaponSettingsSO _weaponSettings;

        public List<Weapon> Weapons;

        public UnityAction OnWeaponsSpawned;

        [Serializable]
        public class ObjectSettings
        {
            public int objectIndex;
            public Transform[] spawnPoints;
        }


        [SerializeField] private ObjectSettings[] _settings;

        private void Start()
        {
            Weapons = new();
            for (int i = 0; i < _settings.Length; i++)
            {
                Weapons.Add(_weaponFactory.Invoke(_settings[i]
                    .spawnPoints[UnityEngine.Random.Range(0, _settings[i].spawnPoints.Length)],
                    _weaponSettings.weaponPrefabs[_settings[i].objectIndex]
                    )
                );

            }
            OnWeaponsSpawned?.Invoke();
        }

        private void OnDestroy()
        {
            OnWeaponsSpawned = null;
        }
    }
}