using Data;
using Player;
using System;
using UnityEngine;
using VContainer;

namespace Game
{
    public class RandomPointItemsSpawner : MonoBehaviour
    {
        [Inject]
        public void Construct(Func<Transform, GameObject, Weapon> weaponFactory, WeaponSettingsSO weaponSettings, PlayerFacade playerFacade)
        {
            _weaponFactory = weaponFactory;
            _weaponSettings = weaponSettings;
            _playerFacade = playerFacade;
        }

        private Func<Transform, GameObject, Weapon> _weaponFactory;
        private WeaponSettingsSO _weaponSettings;
        private PlayerFacade _playerFacade;

        [Serializable]
        public class ObjectSettings
        {
            public int objectIndex;
            public Transform[] spawnPoints;
        }


        [SerializeField] private ObjectSettings[] _settings;

        private void Start()
        {
            for (int i = 0; i < _settings.Length; i++)
            {
                //for (int j = 0; j < _playerFacade.Inventory.Weapons.Count; j++)
                //{
                //if (_playerFacade.Inventory.Weapons[j].weaponName != _weaponSettings.weaponPrefabs[_settings[i].objectIndex].weaponName)
                _weaponFactory.Invoke(_settings[i]
                    .spawnPoints[UnityEngine.Random.Range(0, _settings[i].spawnPoints.Length)],
                    _weaponSettings.weaponPrefabs[_settings[i].objectIndex]
                );
                //}
            }
        }
    }
}