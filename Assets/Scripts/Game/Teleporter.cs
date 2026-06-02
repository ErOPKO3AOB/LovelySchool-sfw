using UnityEngine;

public class Teleporter : MonoBehaviour
{
    private Rigidbody _rb;

    private void OnTriggerEnter(Collider other)
    {
        other.TryGetComponent<Rigidbody>(out _rb);
        if (_rb != null)
        {
            _rb.isKinematic = true;
            Invoke(nameof(ResetKinematics), 0.25f);
        }
        other.transform.position = new Vector3(
            other.transform.position.x,
            15,
            other.transform.position.z
            );
    }

    private void ResetKinematics()
    {
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb = null;
        }
    }
}
