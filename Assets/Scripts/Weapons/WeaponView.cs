using Assets.Scripts.Data;
using UnityEngine;

namespace Assets.Scripts.Weapons
{
    public class WeaponView : MonoBehaviour
    {
        public void Construct(GameSettingsSO gameSettings)
        {
            _gameSettings = gameSettings;
        }

        private GameSettingsSO _gameSettings;

        [SerializeField] private WeaponConfigSO _weaponConfig;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _attackPoint;
        [SerializeField] private MeshRenderer[] _armRenderers;

        public WeaponConfigSO WeaponConfig => _weaponConfig;
        public Rigidbody Rigidbody => _rigidbody;
        public Animator Animator => _animator;
        public Transform AttackPoint => _attackPoint;


        private void Start()
        {
            EnableArms(false);

            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = true;
                _rigidbody.WakeUp();
                _rigidbody.constraints = RigidbodyConstraints.None;
            }
            else
            {
                Debug.LogError($"Weapon {name} has no Rigidbody!", this);
            }

            Transform[] weaponChildren = GetComponentsInChildren<Transform>();
            foreach (var item in weaponChildren)
            {
                item.gameObject.layer = LayerMask.NameToLayer("ItemOutline");
            }
        }

        public void EnableArms(bool value)
        {
            foreach (var item in _armRenderers)
            {
                item.enabled = value;
            }
        }

        public void PlayPickupAnimation()
        {
            if (_animator != null && _animator.isActiveAndEnabled && !string.IsNullOrEmpty(_gameSettings.pickupAnimName))
            {
                _animator.Play(_gameSettings.pickupAnimName, 0, 0f);
            }
        }

        public void PlayAttackAnimation()
        {
            if (_animator == null || _weaponConfig.attackAnimations.Length == 0) return;
            if (!_animator.isActiveAndEnabled)
                _animator.enabled = true;

            string animName = _weaponConfig.attackAnimations[UnityEngine.Random.Range(0, _weaponConfig.attackAnimations.Length)];
            _animator.CrossFade(animName, 0f, 0, 0f);
        }

        public void PlayReloadAnimation()
        {
            if (_animator != null && !string.IsNullOrEmpty(_gameSettings.reloadAnimName))
            {
                _animator.CrossFade(_gameSettings.reloadAnimName, 0f, 0, 0f);
            }
        }
    }
}
