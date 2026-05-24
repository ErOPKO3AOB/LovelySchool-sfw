using Assets.Scripts.Game;
using Data;
using Game;
using Input;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Installers
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameSettingsSO _playerSettings;
        [SerializeField] private WeaponSettingsSO _weaponSettings;
        [SerializeField] private RandomPointItemsSpawner _randomPointItemsSpawner;
        [SerializeField] private EnemySpawnPoint[] _enemySpawnPoints;
        [SerializeField] private GameController _gameController;
        [SerializeField] private EnemyOutliner _enemyOutliner;
        [SerializeField] private MusicPlayer _musicPlayer;

        private void OnValidate()
        {
            _enemySpawnPoints = FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None);
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SoundEmitterService>(Lifetime.Singleton).AsSelf();

            builder.RegisterInstance(_playerSettings);
            builder.RegisterInstance(_weaponSettings);

            builder.Register<InputReader>(Lifetime.Singleton)
                .AsSelf();

            var player = Instantiate(_playerSettings.playerPrefab, new Vector3(-48.5f, 1, -18.5f), Quaternion.identity);
            builder.RegisterComponent(player)
                .AsSelf();

            builder.Register<PooledSpawnService>(Lifetime.Singleton)
                .AsSelf();

            builder.RegisterFactory<Transform, GameObject, Weapon>((resolver) =>
            {
                return (spawnPoint, prefab) =>
                {
                    Weapon weapon = resolver.Instantiate(prefab, spawnPoint.position, spawnPoint.rotation).GetComponent<Weapon>();
                    return weapon;
                };
            },
            Lifetime.Singleton)
            .AsSelf();

            builder.RegisterComponent(_randomPointItemsSpawner);

            builder.RegisterFactory<Transform, GameObject, Enemy>((resolver) =>
            {
                return (spawnPoint, prefab) =>
                {
                    Enemy enemy = resolver.Instantiate(prefab, spawnPoint.position, spawnPoint.rotation).GetComponent<Enemy>();

                    return enemy;
                };
            }, Lifetime.Singleton)
                .AsSelf();

            builder.RegisterComponent(_gameController);

            for (int i = 0; i < _enemySpawnPoints.Length; i++)
            {
                _enemySpawnPoints[i].Construct(_gameController);
            }

            builder.RegisterComponent(_enemyOutliner);

            builder.RegisterComponent(_musicPlayer);
        }
    }
}