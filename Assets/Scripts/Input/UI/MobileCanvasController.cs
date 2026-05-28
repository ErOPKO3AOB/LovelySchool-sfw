using Data;
using Game;
using Player;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VContainer;

namespace Assets.Scripts.Input.UI
{
    public class MobileCanvasController : MonoBehaviour, IDisposable
    {
        [Inject]
        public void Construct(GameSettingsSO gameSettings)
        {
            _gameSettings = gameSettings;
        }

        private GameSettingsSO _gameSettings;
        private Inventory _inventory;

        public Transform inventoryHandler;
        public TouchControl lookControl;
        public VariableJoystick joystick;
        public HoldControl attackHoldButton;
        public Button reloadButton;
        public Button jumpButton;
        public Button kickButton;
        public Button dashButton;
        public Button pauseButton;

        private readonly List<Button> _inventorySlots = new();
        public UnityAction<int> OnSlotChoose;

        private void Start()
        {
            Invoke(nameof(DelayedStart), 0.25f);
        }

        private void DelayedStart()
        {
            _inventory = FindAnyObjectByType<PlayerFacade>().Inventory;
            _inventory.OnWeaponPickUp += OnWeaponPickUp;
            _inventory.OnWeaponSwap += OnWeaponSwap;
            reloadButton.gameObject.SetActive(false);
        }

        private void OnWeaponPickUp(Weapon weapon)
        {
            _inventorySlots.Add(
                Instantiate(_gameSettings.inventorySlotPrefab, inventoryHandler)
                .GetComponent<Button>()
            );
            int currIndex = _inventorySlots.Count - 1;

            _inventorySlots[currIndex].onClick.AddListener(() => 
            {
                OnSlotChoose?.Invoke(currIndex);
            });
        }

        private void OnWeaponSwap(Weapon weapon)
        {
            reloadButton.gameObject.SetActive(!weapon.IsMelee);
        }

        public void Dispose()
        {
            lookControl.Input = null;
            attackHoldButton.holding = null;
            reloadButton.onClick.RemoveAllListeners();
            jumpButton.onClick.RemoveAllListeners();
            kickButton.onClick.RemoveAllListeners();
            dashButton.onClick.RemoveAllListeners();
            pauseButton.onClick.RemoveAllListeners();
            _inventory.OnWeaponPickUp -= OnWeaponPickUp;
            _inventory.OnWeaponSwap -= OnWeaponSwap;
            OnSlotChoose = null;
        }
    }
}