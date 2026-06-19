using Data;
using Game;
using Player;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VContainer;
using YG;
using Button = UnityEngine.UI.Button;

namespace Assets.Scripts.Input.UI
{
    public class ButtonsCanvasController : MonoBehaviour, IDisposable
    {
        [Inject]
        public void Construct(GameSettingsSO gameSettings, WeaponSettingsSO weaponSettings)
        {
            _gameSettings = gameSettings;
            _weaponSettings = weaponSettings;
        }

        private GameSettingsSO _gameSettings;
        private WeaponSettingsSO _weaponSettings;

        private Inventory _inventory;

        public Transform inventoryHandler;
        public VariableJoystick joystick;
        public HoldControl attackHoldButton;
        public Button reloadButton;
        public Button jumpButton;
        public Button kickButton;
        public Button dashButton;
        public Button pauseButton;
        [SerializeField] private List<GameObject> _hintTexts = new();

        private readonly List<Button> _inventorySlots = new();
        public UnityAction<int> OnSlotChoose;

        private const string HintTag = "HintText";
        private bool _hintsActive = true;

        private void OnValidate()
        {
            _hintTexts = new();
            var allText = GetComponentsInChildren<Transform>();
            for (int i = 0; i < allText.Length; i++)
            {
                if (allText[i].CompareTag(HintTag))
                    _hintTexts.Add(allText[i].gameObject);
            }
        }

        private void Start()
        {
            bool isDesktop = YG2.envir.isDesktop;
            if (!isDesktop)
            {
                dashButton.gameObject.SetActive(false);
                _hintsActive = true;
            }

            joystick.gameObject.SetActive(!isDesktop);
            foreach (var hint in _hintTexts)
                hint.SetActive(isDesktop);

            Invoke(nameof(DelayedStart), 0.25f);
            reloadButton.gameObject.SetActive(false);
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
            var instance = Instantiate(_gameSettings.inventorySlotPrefab, inventoryHandler)
                .GetComponent<Button>();

            TMP_Text hintText = null;

            // Костыль жопный ABSOLUTE 777 288 999
            var children = instance.GetComponentsInChildren<TMP_Text>();
            foreach (var child in children)
                if (child.gameObject.CompareTag(HintTag))
                {
                    hintText = child;
                    hintText.gameObject.SetActive(YG2.envir.isDesktop);
                }
            // Костыль кончлся

            _inventorySlots.Add(
                instance
            );
            int currIndex = _inventorySlots.Count - 1;
            hintText.text = (currIndex + 1).ToString();
            Sprite weaponSprite = null;
            for (int i = 0; i < _weaponSettings.weaponSprites.Length; i++)
            {
                if (_weaponSettings.weaponSprites[i].name == weapon.weaponName)
                {
                    weaponSprite = _weaponSettings.weaponSprites[i];
                    break;
                }
            }
            instance.gameObject.GetComponentsInChildren<Image>()[1].sprite = weaponSprite;

            _inventorySlots[currIndex].onClick.AddListener(() =>
            {
                OnSlotChoose?.Invoke(currIndex);
            });

            reloadButton.gameObject.SetActive(!weapon.IsMelee);
        }

        private void OnWeaponSwap(Weapon weapon)
        {
            bool activationValue = !weapon.IsMelee && _hintsActive;

            reloadButton.gameObject.SetActive(activationValue);
        }

        public void SetHints(bool value)
        {
            if (!YG2.envir.isDesktop) return;

            _hintsActive = value;

            foreach (var hints in _hintTexts)
                hints.SetActive(value);

            attackHoldButton.gameObject.SetActive(value);
            reloadButton.gameObject.SetActive(value);
            jumpButton.gameObject.SetActive(value);
            kickButton.gameObject.SetActive(value);
            dashButton.gameObject.SetActive(value);
            pauseButton.gameObject.SetActive(value);
        }

        public void Dispose()
        {
            attackHoldButton.Holding = null;
            attackHoldButton.Dragging = null;

            reloadButton.onClick.RemoveAllListeners();
            jumpButton.onClick.RemoveAllListeners();
            kickButton.onClick.RemoveAllListeners();
            dashButton.onClick.RemoveAllListeners();
            pauseButton.onClick.RemoveAllListeners();
            _inventory.OnWeaponPickUp -= OnWeaponPickUp;
            _inventory.OnWeaponSwap -= OnWeaponSwap;
            OnSlotChoose = null;
            joystick.OnDash.RemoveAllListeners();
        }
    }
}