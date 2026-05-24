using Assets.Scripts.Weapons.Core;
using Game;
using Input;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Player
{
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private Transform _hand;
        [SerializeField] private TMP_Text _ammoText;
        [SerializeField] private PlayerFacade _playerFacade;

        private InputReader _input;
        private readonly List<Weapon> _weapons = new();
        private int _currentWeapon = 0;
        private Camera _camera;

        public List<Weapon> Weapons => _weapons;
        public int CurrentWeapon => _currentWeapon;

        private bool _armsEnabled;

        public bool HasNotMelee { get; private set; }

        private void Start()
        {
            _playerFacade.Health.OnDead += OnDead;
        }

        private void OnDead(GameObject obj)
        {
            _hand.gameObject.SetActive(false);
        }

        public void Initialize(InputReader input, Camera camera)
        {
            _input = input;
            _input.OnUseStarted += HandleUseStarted;
            _input.OnUseCanceled += HandleUseCanceled;
            _input.OnReloadPerformed += HandleReload;
            _input.OnChangeWeaponPerformed += HandleScroll;
            _input.OnSelectInventorySlotPerformed += HandleSelectSlot;

            _camera = camera;
            _hand.SetParent(_camera.transform);
        }

        private void HandleScroll(float delta)
        {
            if (_weapons.Count == 0) return;
            int direction = delta > 0 ? -1 : 1;
            SwapWeapon(direction);
        }

        // TODO: Глянуть, комменты
        private void HandleSelectSlot(int slotIndex)
        {
            if (_weapons.Count == 0)
                return;

            if (slotIndex < 0 || slotIndex >= _weapons.Count)
                return;

            if (slotIndex == _currentWeapon)
                return;

            // Остановить текущее оружие и спрятать
            _weapons[_currentWeapon].StopShoot();

            //_weapons[_currentWeapon]. AbortReload();
            //_weapons[_currentWeapon].SetActive(false);


            // Переключить
            _currentWeapon = slotIndex;
            //_weapons[_currentWeapon].gameObject.SetActive(true);
            RefreshAmmoDisplay();
        }

        private void SwapWeapon(int direction)
        {
            if (_weapons.Count < 2) return;

            // Прерываем перезарядку и стрельбу у текущего оружия
            _weapons[_currentWeapon].StopShoot();
            _weapons[_currentWeapon].AbortReload();
            _weapons[_currentWeapon].gameObject.SetActive(false);

            _currentWeapon += direction;
            if (_currentWeapon >= _weapons.Count)
                _currentWeapon = 0;
            else if (_currentWeapon < 0)
                _currentWeapon = _weapons.Count - 1;

            _weapons[_currentWeapon].gameObject.SetActive(true);
            RefreshAmmoDisplay();
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

                if (hitObject.TryGetComponent<EnemyOutliner>(out var enemyOutliner))
                {
                    enemyOutliner.SetOutlined();
                    return;
                }

                if (_weapons.Count != 0 && _weapons[_currentWeapon].IsReloading) return;

                if (hitObject.TryGetComponent<Weapon>(out var weapon))
                {
                    PickupWeapon(weapon);
                    return;
                }
            }

            if (_weapons.Count > 0)
            {
                _weapons[_currentWeapon].StartShoot();
            }
        }

        private void PickupWeapon(Weapon weapon)
        {
            if (_weapons.Count > 0)
            {
                // Прерываем перезарядку и стрельбу у текущего оружия
                _weapons[_currentWeapon].StopShoot();
                _weapons[_currentWeapon].AbortReload();
                _weapons[_currentWeapon].gameObject.SetActive(false);
            }

            List<Collider> colliders = weapon.GetComponents<Collider>().ToList();
            colliders.ForEach(c => c.enabled = false);

            weapon.Rigidbody.isKinematic = true;
            weapon.Rigidbody.useGravity = false;

            weapon.transform.SetParent(_hand);
            weapon.transform.position = _hand.position;
            weapon.transform.rotation = _hand.rotation;

            weapon.gameObject.SetActive(true);
            weapon.Camera = _camera;

            weapon.Rigidbody.position = weapon.transform.position;
            weapon.Rigidbody.rotation = weapon.transform.rotation;

            weapon.EnableArms(_playerFacade.PlayerData.enableArms);

            if (!weapon.IsMelee) HasNotMelee = true;

            foreach (var item in weapon.arms)
            {
                item.SetActive(true);
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
            _currentWeapon = _weapons.Count - 1;
            RefreshAmmoDisplay();
        }

        private void PickupAmmo(GameObject ammoPickup)
        {
            if (_weapons.Count == 0) return;

            Weapon current = _weapons[_currentWeapon];
            if (current.IsMelee) return;

            current.AddReserve(current.MaxAmmo / 2);
            Destroy(ammoPickup);
        }

        private void HandleUseCanceled()
        {
            if (_weapons.Count > 0)
                _weapons[_currentWeapon].StopShoot();
        }

        private void HandleReload()
        {
            if (_weapons.Count > 0)
                _weapons[_currentWeapon].Reload();
        }

        private void RefreshAmmoDisplay()
        {
            if (_weapons.Count == 0)
            {
                _ammoText.text = "";
                return;
            }

            Weapon w = _weapons[_currentWeapon];
            _ammoText.text = $"{w.CurrentAmmo}/{w.Reserve}";
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.OnUseStarted -= HandleUseStarted;
                _input.OnUseCanceled -= HandleUseCanceled;
                _input.OnReloadPerformed -= HandleReload;
                _input.OnChangeWeaponPerformed -= HandleScroll;
                _input.OnSelectInventorySlotPerformed -= HandleSelectSlot;
            }

            _playerFacade.Health.OnDead -= OnDead;
        }
    }
}