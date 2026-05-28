using Data;
using UnityEngine;
using YG;

namespace Player
{
    public class PlayerMovement
    {
        private readonly Rigidbody _rigidbody;
        private readonly GameSettingsSO _settings;
        private readonly Transform _playerTransform;

        public PlayerMovement(Rigidbody rigidbody, GameSettingsSO settings, Transform playerTransform)
        {
            _rigidbody = rigidbody;
            _settings = settings;
            _playerTransform = playerTransform;
        }

        public void Move(Vector2 input, bool isGrounded, bool isDashing)
        {
            if (isDashing) return;

            Vector3 moveDir;

            moveDir = _playerTransform.forward * input.y + _playerTransform.right * input.x;
            moveDir.y = 0;
            if (YG2.envir.isDesktop)
                moveDir.Normalize();

            float currentSpeed = _settings.walkSpeed;

            Vector3 targetVelocity = moveDir * currentSpeed;

            float control = isGrounded ? 1f : _settings.airControl;

            Vector3 velocity = _rigidbody.linearVelocity;
            Vector3 newVel = Vector3.MoveTowards(
                new Vector3(velocity.x, 0, velocity.z),
                targetVelocity,
                _settings.acceleration * control * Time.deltaTime
            );

            _rigidbody.linearVelocity = new Vector3(newVel.x, velocity.y, newVel.z);
        }
    }
}