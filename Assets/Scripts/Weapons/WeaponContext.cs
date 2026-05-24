using Game;
using Player;
using UnityEngine;

namespace Assets.Scripts.Weapons
{
    public sealed class WeaponContext
    {
        public readonly WeaponView View;
        public readonly WeaponConfigSO Config;
        public readonly PlayerFacade Player;
        public readonly Camera Camera;
        public readonly LayerMask TargetMask;
        public readonly PooledSpawnService Pool;

        public WeaponContext(
            WeaponView view,
            WeaponConfigSO config,
            PlayerFacade player,
            Camera camera,
            LayerMask targetMask,
            PooledSpawnService pool)
        {
            View = view;
            Config = config;
            Player = player;
            Camera = camera;
            TargetMask = targetMask;
            Pool = pool;
        }
    }

}
