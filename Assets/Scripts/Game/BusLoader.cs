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
        UniWebBridge.Player.Set("currentCheckPoint", 0);
        UniWebBridge.Player.Set("weaponDatas", "");

        UniWebBridge.Player.Save();

        OnBusGet?.Invoke();
    }
}
