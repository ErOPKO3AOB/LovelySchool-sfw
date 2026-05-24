using Assets.Scripts.Data;
using Assets.Scripts.Weapons;
using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

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
            public int objectIndex;
            public Transform[] spawnPoints;
        }


        [SerializeField] private ObjectSettings[] _settings;

        private void Start()
        {
            for (int i = 0; i < _settings.Length; i++)
            {
                //_weaponFactory.Invoke(_settings[i]
                //    .spawnPoints[UnityEngine.Random.Range(0, _settings[i].spawnPoints.Length)],
                //    _weaponSettings.weaponPrefabs[_settings[i].objectIndex]
                //);
            }
        }
    }
}