using Assets.Scripts.Data;
using UnityEngine;

namespace Player
{
    public class PlayerMovement
    {
        private readonly Rigidbody _rigidbody;
        private readonly PlayerData _playerData;
        private readonly Transform _playerTransform;

        public PlayerMovement(Rigidbody rigidbody, PlayerData playerData, Transform playerTransform)
        {
            _rigidbody = rigidbody;
            _playerData = playerData;
            _playerTransform = playerTransform;
        }

        public void Move(Vector2 input, bool isGrounded, bool isDashing)
        {
            if (isDashing) return;

            Vector3 moveDir;

            // Обычное движение относительно направления игрока (не камеры, чтобы не ломать управление)
            moveDir = _playerTransform.forward * input.y + _playerTransform.right * input.x;
            moveDir.y = 0;
            moveDir.Normalize();

            float currentSpeed = _playerData.walkSpeed;

            Vector3 targetVelocity = moveDir * currentSpeed;

            float control = isGrounded ? 1f : _playerData.airControl;

            Vector3 velocity = _rigidbody.linearVelocity;
            Vector3 newVel = Vector3.MoveTowards(
                new Vector3(velocity.x, 0, velocity.z),
                targetVelocity,
                _playerData.acceleration * control * Time.deltaTime
            );

            _rigidbody.linearVelocity = new Vector3(newVel.x, velocity.y, newVel.z);
        }
    }
}