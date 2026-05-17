using Unity.AI.Navigation;
using UnityEngine;

namespace Game
{
    public class ObstacleLogic : MonoBehaviour
    {
        [SerializeField] private Collider[] _colliders;

        private void OnValidate()
        {
            _colliders = GetComponents<Collider>();

            EnableColliders(false);
        }

        private void Start()
        {
            EnableColliders(true);

            Destroy(this);
        }

        private void EnableColliders(bool value)
        {
            if (_colliders == null) return;

            foreach (Collider collider in _colliders)
            {
                collider.enabled = value;
            }
        }
    }
}