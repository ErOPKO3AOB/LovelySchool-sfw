using Assets.Scripts.Data;
using Assets.Scripts.Weapons.Attack;
using Assets.Scripts.Weapons.Hit;
using Game;
using Player;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Assets.Scripts.Weapons
{
    public sealed class WeaponFactory
    {
        public WeaponFactory(
            WeaponConfigSO weaponConfig,
            //GameSettingsSO gameConfigs,
            PooledSpawnService pool,
            PlayerFacade player,
            Camera camera,
            IObjectResolver resolver)
        {
            _weaponConfig = weaponConfig;
            //_gameConfigs = gameConfigs;
            _pool = pool;
            _player = player;
            _camera = camera;
            _resolver = resolver;
        }

        private readonly WeaponConfigSO _weaponConfig;
        //private readonly GameSettingsSO _gameConfigs;
        private readonly PooledSpawnService _pool;
        private readonly PlayerFacade _player;
        private readonly Camera _camera;
        private readonly LayerMask _targetMask;
        private readonly IObjectResolver _resolver;

        public WeaponRuntime Create(
            WeaponView prefab,
            Vector3 position,
            Quaternion rotation)
        {
            WeaponView view = _resolver.Instantiate(prefab, position, rotation);

            view.InitializePhysics();

            var config = _weaponConfig;

            WeaponContext context = new WeaponContext(
                view,
                config,
                _player,
                _camera,
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
    }

}
