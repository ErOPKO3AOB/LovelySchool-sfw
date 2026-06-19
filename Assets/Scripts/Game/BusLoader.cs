using UnityEngine;
using UnityEngine.Events;
using YG;

public class BusLoader : MonoBehaviour
{
    [SerializeField] private LayerMask _playerLayerMask;
    public UnityEvent OnBusGet;

    public void GetInBus()
    {
        YG2.saves.currentCheckPoint = 0;
        YG2.saves.weaponDatas = new();
        YG2.saves.gameCompletedTimes++;

        YG2.SaveProgress();

        OnBusGet?.Invoke();
    }
}
