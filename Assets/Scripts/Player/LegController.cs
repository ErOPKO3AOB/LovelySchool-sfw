using Game;
using Player;
using UnityEngine;

public class LegController : MonoBehaviour
{
    [SerializeField] private PlayerFacade _playerFacade;
    [SerializeField] private Animator _animator;
    [SerializeField] private Animator _v1BodyAnimator;
    [SerializeField] private LayerMask _kickLayerMask;
    [SerializeField] private int _damage = 15;
    [SerializeField] private float _force = 32f;
    [SerializeField] private float _cooldownTime = 3f;
    [SerializeField] private float _kickRange = 6f;
    [SerializeField] private Vector3 _kickBoxSize = new Vector3(0.6f, 0.6f, 0.1f); // толщина бокаста

    private float _cooldownTimer = 0f;

    public bool IsKicking { get; private set; }

    public void StartKick()
    {
        if (_cooldownTimer > 0 || _playerFacade.Health.IsDead || IsKicking) return;

        _animator.SetTrigger("KickPerform");
        _v1BodyAnimator.SetTrigger("KickPerform");
    }

    public void KickPerform()
    {
        IsKicking = true;
        Transform player = _playerFacade.transform;
        Vector3 kickOrigin = player.position - player.up * 0.25f;
        kickOrigin = new Vector3(kickOrigin.x + 0.25f, kickOrigin.y, kickOrigin.z);
        Vector3 kickDir = player.forward;

        // BoxCast с половинным размером бокса
        if (Physics.BoxCast(kickOrigin, _kickBoxSize * 0.5f, kickDir, out var hit, Quaternion.identity, _kickRange, _kickLayerMask))
        {
            Vector3 pushDir = (kickDir + Vector3.up * 0.35f).normalized;

            float finalForce = _force;
            int finalDamage = _damage;
            if (_playerFacade.Dash.IsDashing)
            {
                finalForce *= 3;
                finalDamage *= 3;
            }

            if (hit.collider.TryGetComponent(out Enemy enemy))
            {
                enemy.Health.TakeDamage(finalDamage);
                enemy.Agent.velocity = pushDir * finalForce;
            }
            else if (hit.collider.TryGetComponent(out Rigidbody rb))
            {
                rb.AddForceAtPosition(pushDir * (finalForce * 0.8f), hit.point, ForceMode.Impulse);
            }
        }

        _animator.ResetTrigger("KickPerform");
        _cooldownTimer = _cooldownTime;
    }

    public void OnKickEnd()
    {
        IsKicking = false;
        _v1BodyAnimator.ResetTrigger("KickPerform");
    }

    private void Update()
    {
        if (_cooldownTimer > 0)
            _cooldownTimer -= Time.deltaTime;
    }
}