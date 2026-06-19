using Assets.Scripts.Game;
using Assets.Scripts.Input;
using Assets.Scripts.Input.UI;
using Data;
using Game;
using Player;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using VContainer;
using VContainer.Unity;
using YG;

namespace Installers
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameSettingsSO _gameSettings;
        [SerializeField] private WeaponSettingsSO _weaponSettings;
        [SerializeField] private RandomPointItemsSpawner _randomPointItemsSpawner;
        [SerializeField] private EnemySpawnPoint[] _enemySpawnPoints;
        [SerializeField] private GameController _gameController;
        [SerializeField] private MusicPlayer _musicPlayer;

        private void OnValidate()
        {
            _enemySpawnPoints = FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None);
        }

        protected override void Configure(IContainerBuilder builder)
        {
            QualitySettings.lodBias = 2f;

            builder.RegisterFactory<Transform, EnemyOutliner>(resolver =>
            {
                return (spawnPoint) =>
                {
                    EnemyOutliner instance = resolver.Instantiate(_gameSettings.enemyOutlinerPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
                    return instance;
                };
            }, Lifetime.Singleton)
            .AsSelf();

            builder.RegisterFactory<Transform, Checkpoint>(resolver =>
            {
                return (spawnPoint) =>
                {
                    Checkpoint instance = resolver.Instantiate(_gameSettings.checkPointPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
                    return instance;
                };
            }, Lifetime.Singleton)
            .AsSelf();

            builder.RegisterEntryPoint<CounterService>(Lifetime.Singleton).AsSelf();

            builder.Register<SoundEmitterService>(Lifetime.Singleton).AsSelf();

            builder.RegisterInstance(_gameSettings);
            builder.RegisterInstance(_weaponSettings);

            // INPUT
            builder.RegisterComponent(Instantiate(_gameSettings.buttonsCanvasPrefab));
            if (YG2.envir.isMobile || YG2.envir.isTablet)
            {
                builder.RegisterEntryPoint<MobileLookService>().AsSelf();
                builder.Register<MobileInputReader>(Lifetime.Singleton)
                    .As<IInputReader>();

                EnhancedTouchSupport.Enable();
            }
            else
            {
                builder.Register<StandaloneInputReader>(Lifetime.Singleton)
                    .As<IInputReader>();
            }

            // PLAYER
            var player = Instantiate(_gameSettings.playerPrefab, new Vector3(-12.5f, -5, -39), Quaternion.identity);
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

            builder.RegisterComponent(_randomPointItemsSpawner).AsSelf();

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

            builder.RegisterComponent(_musicPlayer);
        }
    }
}