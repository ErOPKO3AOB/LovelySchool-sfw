using UnityEngine;
using VContainer.Unity;

namespace Assets.Scripts.Weapons
{
    public sealed class ReloadModule : ITickable
    {
        private readonly AmmoModule _ammo;
        private readonly AnimationModule _animation;
        private readonly WeaponConfigSO _config;

        private bool _isReloading;
        private float _timer;

        public bool IsReloading => _isReloading;

        public ReloadModule(
            AmmoModule ammo,
            AnimationModule animation,
            WeaponConfigSO config)
        {
            _ammo = ammo;
            _animation = animation;
            _config = config;
        }

        public void StartReload()
        {
            if (_isReloading)
                return;

            if (!_ammo.CanReload())
                return;

            _isReloading = true;
            _timer = 0f;

            _animation.PlayReload();
        }

        public void AbortReload()
        {
            if (!_isReloading)
                return;

            _isReloading = false;
            _timer = 0f;

            _animation.PlayPickup();
        }

        public void Tick()
        {
            if (!_isReloading)
                return;

            _timer += Time.deltaTime;

            if (_timer >= _config.reloadTime)
            {
                _ammo.Reload();
                _isReloading = false;
            }
        }
    }

}
