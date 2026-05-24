using System;
using UnityEngine;

namespace Assets.Scripts.Weapons
{
    public sealed class AmmoModule
    {
        private readonly WeaponConfigSO _config;

        private int _ammo;
        private int _reserve;

        public int Ammo => _ammo;
        public int Reserve => _reserve;
        public int MaxAmmo => _config.maxAmmo;

        public event Action<int, int, int> AmmoChanged;

        public AmmoModule(WeaponConfigSO config)
        {
            _config = config;

            _ammo = config.maxAmmo;
            _reserve = config.startingReserveAmmo;

            Notify();
        }

        public bool TryConsume()
        {
            if (_ammo <= 0)
                return false;

            _ammo--;
            Notify();

            return true;
        }

        public bool CanReload()
        {
            return _reserve > 0 && _ammo < _config.maxAmmo;
        }

        public void Reload()
        {
            int needed = _config.maxAmmo - _ammo;
            int taken = Mathf.Min(needed, _reserve);

            _ammo += taken;
            _reserve -= taken;

            Notify();
        }

        public void AddReserve(int amount)
        {
            _reserve += amount;
            Notify();
        }

        private void Notify()
        {
            AmmoChanged?.Invoke(_ammo, _config.maxAmmo, _reserve);
        }
    }
}
