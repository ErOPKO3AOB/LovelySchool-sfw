using Assets.Scripts.Input;
using Game;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using YG;

namespace Player
{
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private Transform _hand;
        [SerializeField] private TMP_Text _ammoText;
        [SerializeField] private PlayerFacade _playerFacade;

        private IInputReader _inputReader;
        private RandomPointItemsSpawner _itemsSpawner;
        private readonly List<Weapon> _weapons = new();
        private int _currentWeaponIndex = 0;
        private Camera _camera;

        public List<Weapon> Weapons => _weapons;
        public int CurrentWeaponIndex => _currentWeaponIndex;

        public bool HasNotMelee { get; private set; }

        public UnityAction<Weapon> OnWeaponPickUp;
        public UnityAction<Weapon> OnWeaponSwap;


        private void Start()
        {
            _playerFacade.Health.OnDead += OnDead;
        }

        private void OnDead(GameObject obj)
        {
            _hand.gameObject.SetActive(false);
        }

        public void Initialize(IInputReader inputReader, Camera camera, RandomPointItemsSpawner itemsSpawner)
        {
            _inputReader = inputReader;
            _itemsSpawner = itemsSpawner;
            _inputReader.OnUseStarted += HandleUseStarted;
            _inputReader.OnUseCanceled += HandleUseCanceled;
            _inputReader.OnReloadPerformed += HandleReload;
            _inputReader.OnChangeWeaponPerformed += HandleScroll;
            _inputReader.OnSelectInventorySlotPerformed += HandleSelectSlot;

            _camera = camera;
            _hand.SetParent(_camera.transform);

            _itemsSpawner.OnWeaponsSpawned += () =>
            {
                LoadWeaponsState();
            };
        }

        private void HandleScroll(float delta)
        {
            if (_weapons.Count == 0) return;
            int direction = delta > 0 ? -1 : 1;
            SwapWeapon(direction);
        }

        private void HandleSelectSlot(int slotIndex)
        {
            if (_weapons.Count == 0)
                return;

            if (slotIndex < 0 || slotIndex >= _weapons.Count)
                return;

            if (slotIndex == _currentWeaponIndex)
                return;

            // Остановить текущее оружие и спрятать
            _weapons[_currentWeaponIndex].StopShoot();
            _weapons[_currentWeaponIndex].AbortReload();
            _weapons[_currentWeaponIndex].gameObject.SetActive(false);

            // Переключить
            _currentWeaponIndex = slotIndex;
            _weapons[_currentWeaponIndex].gameObject.SetActive(true);
            RefreshAmmoDisplay();

            OnWeaponSwap?.Invoke(Weapons[_currentWeaponIndex]);
        }

        private void SwapWeapon(int direction)
        {
            if (_weapons.Count < 2) return;

            // Прерываем перезарядку и стрельбу у текущего оружия
            _weapons[_currentWeaponIndex].StopShoot();
            _weapons[_currentWeaponIndex].AbortReload();
            _weapons[_currentWeaponIndex].gameObject.SetActive(false);

            _currentWeaponIndex += direction;
            if (_currentWeaponIndex >= _weapons.Count)
                _currentWeaponIndex = 0;
            else if (_currentWeaponIndex < 0)
                _currentWeaponIndex = _weapons.Count - 1;

            _weapons[_currentWeaponIndex].gameObject.SetActive(true);
            RefreshAmmoDisplay();
            OnWeaponSwap?.Invoke(Weapons[_currentWeaponIndex]);
        }

        private void HandleUseStarted()
        {
            Ray ray = new(_camera.transform.position, _camera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 5f) && hit.collider != null)
            {
                GameObject hitObject = hit.collider.gameObject;

                if (_weapons.Count != 0 && hitObject.layer == LayerMask.NameToLayer("AmmoOutline"))
                {
                    PickupAmmo(hitObject);
                    return;
                }

                else if (hitObject.TryGetComponent<EnemyOutliner>(out var enemyOutliner))
                {
                    enemyOutliner.SetOutlined();
                    return;
                }

                else if (_weapons.Count != 0 && _weapons[_currentWeaponIndex].IsReloading) return;

                else if (hitObject.TryGetComponent<Weapon>(out var weapon))
                {
                    PickupWeapon(weapon);
                    return;
                }
            }

            if (_weapons.Count > 0)
            {
                _weapons[_currentWeaponIndex].StartShoot();
            }
        }

        // Bullshit method bro
        public bool CheckForHitTarget()
        {
            Ray ray = new(_camera.transform.position, _camera.transform.forward);
            return Physics.Raycast(ray, out RaycastHit hit, 5f) && hit.collider != null &&
                (_weapons.Count != 0 && hit.collider.gameObject.layer == LayerMask.NameToLayer("AmmoOutline")
                || hit.collider.gameObject.TryGetComponent<EnemyOutliner>(out var outliner)
                || hit.collider.gameObject.TryGetComponent<Weapon>(out var weapon));
        }

        private void PickupWeapon(Weapon weapon)
        {
            if (_weapons.Count > 0)
            {
                // Прерываем перезарядку и стрельбу у текущего оружия
                _weapons[_currentWeaponIndex].StopShoot();
                _weapons[_currentWeaponIndex].AbortReload();
                _weapons[_currentWeaponIndex].gameObject.SetActive(false);
            }

            if (weapon.Light != null) Destroy(weapon.Light.gameObject);

            List<Collider> colliders = weapon.GetComponents<Collider>().ToList();
            colliders.ForEach(c => c.enabled = false);

            weapon.Rigidbody.isKinematic = true;
            weapon.Rigidbody.useGravity = false;

            weapon.transform.SetParent(_hand);
            weapon.transform.SetPositionAndRotation(_hand.position, _hand.rotation);
            weapon.gameObject.SetActive(true);
            weapon.Camera = _camera;

            weapon.Rigidbody.position = weapon.transform.position;
            weapon.Rigidbody.rotation = weapon.transform.rotation;

            if (!weapon.IsMelee) HasNotMelee = true;

            for (int i = 0; i < weapon.arms.Length; i++)
            {
                weapon.arms[i].SetActive(true);
            }

            Transform[] weaponChildren = weapon.GetComponentsInChildren<Transform>();
            foreach (var item in weaponChildren)
            {
                item.gameObject.layer = LayerMask.NameToLayer("ItemInHands");
            }

            weapon.Animator.enabled = true;
            weapon.PlayPickupAnimation(); // проигрываем анимацию подбора

            weapon.AmmoChanged += (_, _, _) => RefreshAmmoDisplay();

            _weapons.Add(weapon);
            _currentWeaponIndex = _weapons.Count - 1;
            RefreshAmmoDisplay();

            OnWeaponPickUp?.Invoke(weapon);
            OnWeaponSwap?.Invoke(Weapons[_currentWeaponIndex]);
        }

        private void PickupAmmo(GameObject ammoPickup)
        {
            if (_weapons.Count == 0) return;

            Weapon current = _weapons[_currentWeaponIndex];
            if (current.IsMelee) return;

            current.AddReserve(current.MaxAmmo / 2);
            Destroy(ammoPickup);
        }

        private void HandleUseCanceled()
        {
            if (_weapons.Count > 0)
                _weapons[_currentWeaponIndex].StopShoot();
        }

        private void HandleReload()
        {
            if (_weapons.Count > 0)
                _weapons[_currentWeaponIndex].Reload();
        }

        private void RefreshAmmoDisplay()
        {
            if (_weapons.Count == 0)
            {
                _ammoText.text = "";
                return;
            }

            Weapon w = _weapons[_currentWeaponIndex];
            _ammoText.text = $"{w.CurrentAmmo}/{w.Reserve}";
        }

        public void SaveWeaponsState()
        {
            var weaponDatas = new List<WeaponData>();

            for (int i = 0; i < Weapons.Count; i++)
            {
                var data = new WeaponData
                {
                    weaponName = Weapons[i].weaponName,
                    ammo = Weapons[i].CurrentAmmo,
                    reserve = Weapons[i].Reserve
                };

                weaponDatas.Add(data);
            }

            YG2.saves.weaponDatas = weaponDatas;
        }

        public void LoadWeaponsState()
        {
            var weaponDatas = YG2.saves.weaponDatas;

            //Debug.Log("Weapon Datas count: " + weaponDatas.Count);
            //Debug.Log("item spawner weapons count: " + _itemsSpawner.Weapons.Count);


            if (weaponDatas == null || weaponDatas.Count <= 0) return;

            for (int i = 0; i < weaponDatas.Count; i++)
            {
                for (int j = 0; j < _itemsSpawner.Weapons.Count; j++)
                {
                    if (_itemsSpawner.Weapons[j].weaponName == weaponDatas[i].weaponName)
                    {
                        Weapon weapon = _itemsSpawner.Weapons[j];
                        PickupWeapon(weapon);
                        weapon.CurrentAmmo = weaponDatas[i].ammo;
                        weapon.Reserve = weaponDatas[i].reserve;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_inputReader != null)
            {
                _inputReader.OnUseStarted -= HandleUseStarted;
                _inputReader.OnUseCanceled -= HandleUseCanceled;
                _inputReader.OnReloadPerformed -= HandleReload;
                _inputReader.OnChangeWeaponPerformed -= HandleScroll;
                _inputReader.OnSelectInventorySlotPerformed -= HandleSelectSlot;
            }

            _playerFacade.Health.OnDead -= OnDead;
        }
    }
}