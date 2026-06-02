using Player;
using UnityEngine;
using UnityEngine.Events;

public class OnEnteredTrigger : MonoBehaviour
{
    [SerializeField] private LayerMask _playerLayerMask;
    public UnityEvent OnTriggerEntered;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<PlayerFacade>() != null)
        {
            OnTriggerEntered?.Invoke();
        }
    }
}
