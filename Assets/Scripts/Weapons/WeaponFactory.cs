using UnityEngine;
using VContainer;
using VContainer.Unity;
using System.Collections.Generic;
using Assets.Scripts.Data;
using Game;

namespace Assets.Scripts.Weapons
{
    public class WeaponFactory : ITickable
    {
        public WeaponFactory(IObjectResolver objectResolver)
        {
            _resolver = objectResolver;
        }

        private readonly IObjectResolver _resolver;
        private readonly List<WeaponModel> _weapons = new();

        public WeaponModel Create(WeaponView weaponPrefab, bool firearm, Vector3 spawnPos, Quaternion spawnRot)
        {
            var view = _resolver.Instantiate(weaponPrefab, spawnPos, spawnRot);

            WeaponModel weaponModel = firearm ?
                new Firearm(view, view.WeaponConfig, _resolver.Resolve<GameSettingsSO>(), _resolver.Resolve<PooledSpawnService>()) :
                new Melee();

            weaponModel.Initialize();

            _weapons.Add(weaponModel);

            return weaponModel;
        }

        public void Tick()
        {
            if (_weapons != null)
                _weapons.ForEach(w => w.Tick());
        }
    }
}
