using UnityEngine;
using UnityEngine.AI;

namespace Game
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NavMeshObstacle))]
    public class Door : MonoBehaviour
    {
        [SerializeField] private Material _closedMaterial;
        [SerializeField] private Material _openedMaterial;

        private Rigidbody _rigidbody;
        private NavMeshObstacle _navMeshObstacle;
        private MeshRenderer _meshRenderer;
        
        public Rigidbody Rigidbody => _rigidbody == null ? _rigidbody = GetComponent<Rigidbody>() : _rigidbody;
        public NavMeshObstacle NavMeshObstacle => _navMeshObstacle == null ? _navMeshObstacle = GetComponent<NavMeshObstacle>() : _navMeshObstacle;
        public MeshRenderer MeshRenderer => _meshRenderer == null ? _meshRenderer = GetComponent<MeshRenderer>() : _meshRenderer;

        public void Close()
        {
            Rigidbody.isKinematic = true;
            NavMeshObstacle.enabled = true;
            MeshRenderer.material = _closedMaterial;
        }

        public void Open()
        {
            Rigidbody.isKinematic = false;
            NavMeshObstacle.enabled = false;
            MeshRenderer.material = _openedMaterial;
        }
    }
}