using System;
using System.Collections.Generic;
using UnityEngine;
namespace Assets.Scripts.Input
{
    public interface IInputReader
    {
        event Action OnUseStarted, OnUseCanceled, OnReloadPerformed;
        event Action<float> OnChangeWeaponPerformed;
        event Action<int> OnSelectInventorySlotPerformed;
    }
    public sealed class TestInputReader : IInputReader
    {
        public event Action OnUseStarted, OnUseCanceled, OnReloadPerformed;
        public event Action<float> OnChangeWeaponPerformed;
        public event Action<int> OnSelectInventorySlotPerformed;
    }
}
namespace Player
{
    public class PlayerFacade : MonoBehaviour { public TestHealth Health = new TestHealth(); }
    public class TestHealth { public event Action<GameObject> OnDead; }
}
namespace TMPro { public class TMP_Text : MonoBehaviour { public string text; } }
namespace Game
{
    public class RandomPointItemsSpawner : MonoBehaviour { public bool IsReady; public List<Weapon> Weapons = new List<Weapon>(); }
    public class EnemyOutliner : MonoBehaviour { public void SetOutlined() {} }
    public class Weapon : MonoBehaviour
    {
        public string weaponName;
        public int CurrentAmmo, Reserve;
        public int MaxAmmo = 30;
        public bool IsMelee, IsReloading;
        public Light Light;
        public GameObject[] arms = new GameObject[0];
        public Camera Camera;
        public event Action<int,int,int> AmmoChanged;
        public Rigidbody Rigidbody => GetComponent<Rigidbody>();
        public Animator Animator => GetComponent<Animator>();
        public void StopShoot() {} public void AbortReload() {} public void PlayPickupAnimation() {}
        public void StartShoot() {} public void Reload() {} public void AddReserve(int value) => Reserve += value;
    }
}
public class BusLoader : MonoBehaviour { public void GetInBus() {} }
