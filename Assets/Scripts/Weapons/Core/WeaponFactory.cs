using Assets.Scripts.Data;
using Assets.Scripts.Weapons.Ammo;
using Assets.Scripts.Weapons.Attack;
using Assets.Scripts.Weapons.Hit;
using Assets.Scripts.Weapons.Visual;
using Game;
using Player;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Assets.Scripts.Weapons.Core
{
    public sealed class WeaponFactory : ITickable
    {
        public WeaponFactory(
            GameSettingsSO gameConfigs,
            PlayerData playerData,
            PooledSpawnService pool,
            IObjectResolver resolver)
        {
            _pool = pool;
            _playerData = playerData;
            _resolver = resolver;
        }

        private readonly PooledSpawnService _pool;
        private readonly PlayerData _playerData;
        private readonly LayerMask _targetMask;
        private readonly IObjectResolver _resolver;

        #region INIT
        public WeaponRuntime Create(
            WeaponConfigSO config,
            Vector3 position,
            Quaternion rotation)
        {
            WeaponView view = _resolver.Instantiate(config.weaponPrefab, position, rotation).GetComponent<WeaponView>();

            WeaponContext context = new WeaponContext(
                view,
                config,
                _playerData.playerPrefab,
                _playerData.cameraPrefab.Camera,
                _targetMask,
                _pool);

            AmmoModule ammo = new AmmoModule(config);
            AnimationModule animation = new AnimationModule(context);
            ProjectileModule projectile = new ProjectileModule(context);

            IHitModule hit = CreateHitModule(config, context);
            IAttackModule attack = CreateAttackModule(config, ammo, animation, hit, projectile);

            ReloadModule reload = new ReloadModule(ammo, animation, config);

            return new WeaponRuntime(attack, reload);
        }

        private IHitModule CreateHitModule(
            WeaponConfigSO config,
            WeaponContext context)
        {
            return config.hitType switch
            {
                HitType.Single => new SingleHitModule(context),
                HitType.Penetrating => new PenetratingHitModule(context),
                HitType.Melee => new MeleeHitModule(context),
                _ => new SingleHitModule(context)
            };
        }

        private IAttackModule CreateAttackModule(
            WeaponConfigSO config,
            AmmoModule ammo,
            AnimationModule animation,
            IHitModule hit,
            ProjectileModule projectile)
        {
            return config.attackType switch
            {
                WeaponAttackType.SemiAuto =>
                    new SemiAutoAttackModule(ammo, animation, hit, projectile, config),

                WeaponAttackType.FullAuto =>
                    new FullAutoAttackModule(ammo, animation, hit, projectile, config),

                WeaponAttackType.Melee =>
                    new MeleeAttackModule(animation, hit, config),

                _ =>
                    new SemiAutoAttackModule(ammo, animation, hit, projectile, config)
            };
        }
        #endregion

        #region CONTROL
        public void Tick()
        {

        }
        #endregion
    }

}
