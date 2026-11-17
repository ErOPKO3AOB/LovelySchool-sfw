using Game;
using UnityEngine;
using UnityEngine.Events;
using UniWebKit;
using YG;

public class BusLoader : MonoBehaviour
{
    [SerializeField] private LayerMask _playerLayerMask;
    public UnityEvent OnBusGet;

    public void GetInBus()
    {
        if (!GameSaveSession.Set("currentCheckPoint", 0)) return;
        if (!GameSaveSession.Set(InventorySaveData.Key, new InventorySaveData())) return;
        UniWebBridge.Player.DeleteKey(InventorySaveData.LegacyKey);
        GameSaveSession.Save();

        OnBusGet?.Invoke();
    }
}
