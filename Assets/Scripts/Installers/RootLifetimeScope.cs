using Assets.Scripts.Game;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class RootLifetimeScope : LifetimeScope
{
    [SerializeField] private QuitSessionEventHandler _quitSessionEventHandler;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterEntryPoint<CounterService>().AsSelf();

        builder.RegisterComponentInNewPrefab(typeof(QuitSessionEventHandler), _quitSessionEventHandler, Lifetime.Singleton).AsSelf();
    }
}
