using Assets.Scripts.Player;
using Data;
using Game;
using Player;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UniWebKit;
using VContainer;
using YG;
using Button = UnityEngine.UI.Button;

namespace Assets.Scripts.Input.UI
{
    public class ButtonsCanvasController : MonoBehaviour, IDisposable
    {
        [Inject]
        public void Construct(GameSettingsSO gameSettings, WeaponSettingsSO weaponSettings, RandomPointItemsSpawner itemsSpawner)
        {
            _gameSettings = gameSettings;
            _weaponSettings = weaponSettings;
            _itemsSpawner = itemsSpawner;
        }

        private readonly WaitForSeconds _checkForCanGrabDelay = new(0.2f);

        private GameSettingsSO _gameSettings;
        private WeaponSettingsSO _weaponSettings;
        private RandomPointItemsSpawner _itemsSpawner;

        private Inventory _inventory;

        public Transform inventoryHandler;
        public VariableJoystick joystick;
        public HoldControl attackHoldButton;
        public Button reloadButton;
        public Button jumpButton;
        public Button kickButton;
        public Button dashButton;
        public Button pauseButton;
        public Button mobileSecondUseButton;
        [SerializeField] private Image _attackButtonImageTarget;
        [SerializeField] private Sprite _handSprite;
        [SerializeField] private Sprite _shootSprite;
        [SerializeField] private List<GameObject> _hintTexts = new();
        [SerializeField] private GameObject _armHint;
        [SerializeField] private TMP_Text _armHintText;


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

        private IEnumerator Start()
        {
            _armHint.SetActive(false);

            bool isDesktop = UniWebBridge.Envir.IsDesktop;
            if (!isDesktop)
            {
                dashButton.gameObject.SetActive(false);
                _hintsActive = true;
            }

            joystick.gameObject.SetActive(!isDesktop);
            foreach (var hint in _hintTexts)
                hint.SetActive(isDesktop && _hintsActive);

            reloadButton.gameObject.SetActive(false);
            StartCoroutine(CheckForCanGrabRoutine());
            // The spawner and player can start in either order. Bind and catch up with existing weapons.
            while (_inventory == null || !_inventory.IsInitialized || !_itemsSpawner.IsReady)
            {
                var player = FindAnyObjectByType<PlayerFacade>();
                if (player != null) _inventory = player.Inventory;
                yield return null;
            }
            DelayedStart();
        }

        private void DelayedStart()
        {
            _inventory = FindAnyObjectByType<PlayerFacade>().Inventory;
            _inventory.OnWeaponPickUp += OnWeaponPickUp;
            _inventory.OnWeaponSwap += OnWeaponSwap;
            if (mobileSecondUseButton != null)
                mobileSecondUseButton.onClick.AddListener(_inventory.UseFocusedItem);
            foreach (var weapon in _inventory.Weapons) OnWeaponPickUp(weapon);
            if (_inventory.Weapons.Count > 0) OnWeaponSwap(_inventory.Weapons[_inventory.CurrentWeaponIndex]);
            else reloadButton.gameObject.SetActive(false);
        }

        private IEnumerator CheckForCanGrabRoutine()
        {
            var seconds = _checkForCanGrabDelay;
            while (gameObject.activeSelf)
            {
                if (_inventory == null)
                {
                    yield return seconds;
                    continue;
                }

                yield return seconds;
                var hitTuple = _inventory.CheckForHitTarget();
                _attackButtonImageTarget.sprite = hitTuple.Item1
                ? _handSprite : _shootSprite;
                _armHint.SetActive(hitTuple.Item1);

                if (mobileSecondUseButton != null)
                    mobileSecondUseButton.gameObject.SetActive(UniWebBridge.Envir.IsMobile && hitTuple.Item1);
                
                if (hitTuple.Item1)
                {
                    if (hitTuple.Item2 != null)
                    {
                        _armHintText.text = UniWebBridge.Localization.Language == "ru" ?
                            $"Подобрать {hitTuple.Item2.ru}" : $"Pick up {hitTuple.Item2.en}";
                    }

                    else
                    {
                        _armHintText.text = UniWebBridge.Localization.Language == "ru" ?
                            $"Использовать" : $"Use";
                    }
                }
            }
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
                    hintText.gameObject.SetActive(UniWebBridge.Envir.IsDesktop);
                }
            // Костыль кончлся

            _inventorySlots.Add(
                instance
            );
            int currIndex = _inventorySlots.Count - 1;
            if (hintText != null) hintText.text = (currIndex + 1).ToString();
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
            if (!UniWebBridge.Envir.IsDesktop) return;

            _hintsActive = value;

            foreach (var hints in _hintTexts)
                hints.SetActive(value);

            attackHoldButton.gameObject.SetActive(value);
            reloadButton.gameObject.SetActive(value);
            jumpButton.gameObject.SetActive(value);
            kickButton.gameObject.SetActive(value);
            dashButton.gameObject.SetActive(value);
            pauseButton.gameObject.SetActive(value);

            if (_hintsActive)
            {
                StopCoroutine(CheckForCanGrabRoutine());
                StartCoroutine(CheckForCanGrabRoutine());
            }
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
            if (_inventory != null)
            {
                _inventory.OnWeaponPickUp -= OnWeaponPickUp;
                _inventory.OnWeaponSwap -= OnWeaponSwap;
                if (mobileSecondUseButton != null)
                    mobileSecondUseButton.onClick.RemoveListener(_inventory.UseFocusedItem);
            }
            OnSlotChoose = null;
            joystick.OnDash.RemoveAllListeners();
        }
    }
}
