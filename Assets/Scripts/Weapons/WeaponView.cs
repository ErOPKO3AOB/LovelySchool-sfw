using UnityEngine;

namespace Assets.Scripts.Weapons
{
    public class WeaponView : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Transform _attackPoint;
        [SerializeField] private MeshRenderer[] _armRenderers;

        public Animator Animator => _animator;
        public Rigidbody Rigidbody => _rigidbody;
        public Transform AttackPoint => _attackPoint;

        private void Start()
        {
            InitializePhysics();
        }

        public void InitializePhysics()
        {
            if (_rigidbody == null)
                return;

            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;
            _rigidbody.WakeUp();
            _rigidbody.constraints = RigidbodyConstraints.None;
        }

        public void EnableArms(bool value)
        {
            foreach (var renderer in _armRenderers)
            {
                renderer.enabled = value;
            }
        }
    }
}
