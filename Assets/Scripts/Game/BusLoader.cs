using Assets.Scripts.Game.Core;
using UnityEngine;
using UnityEngine.Events;
using UniWebKit;

public class BusLoader : MonoBehaviour
{
    [SerializeField] private LayerMask _playerLayerMask;
    [SerializeField] private GameController _gameController;
    public UnityEvent OnBusGet;
    private bool _loadingVictory;

    public void GetInBus()
    {
        if (_loadingVictory || _gameController == null || _gameController.PlayerFacade == null ||
            !_gameController.PlayerFacade.Inventory.HasExitKey) return;

        if (!GameSaveSession.Set("currentCheckPoint", 0)) return;
        if (!GameSaveSession.Set(InventorySaveData.Key, new InventorySaveData())) return;
        UniWebBridge.Player.DeleteKey(InventorySaveData.LegacyKey);
        GameSaveSession.Save();

        _loadingVictory = true;
        OnBusGet?.Invoke();
    }
}
