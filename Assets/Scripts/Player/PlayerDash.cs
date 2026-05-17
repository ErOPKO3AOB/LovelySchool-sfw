using Data;
using Player;
using System.Collections;
using UnityEngine;

namespace Player
{
    public class PlayerDash
    {
        private readonly Rigidbody _rigidbody;
        private readonly GameSettingsSO _settings;
        private readonly PlayerStamina _stamina;
        private readonly Transform _cameraTransform;

        private bool _isDashing;
        private float _dashTimer;
        private float _cooldownTimer;

        public bool IsDashing => _isDashing;

        public PlayerDash(Rigidbody rigidbody, GameSettingsSO settings, PlayerStamina stamina, Transform cameraTransform)
        {
            _rigidbody = rigidbody;
            _settings = settings;
            _stamina = stamina;
            _cameraTransform = cameraTransform;
        }

        public void Update(float deltaTime)
        {
            if (_cooldownTimer > 0)
                _cooldownTimer -= deltaTime;

            if (_isDashing)
            {
                _dashTimer -= deltaTime;
                if (_dashTimer <= 0)
                    _isDashing = false;
            }
        }

        public bool TryStartDash(Vector2 moveInput)
        {
            if (_isDashing || _cooldownTimer > 0) return false;
            if (!_stamina.TryConsume(_settings.dashStaminaCost)) return false;

            Vector3 dashDir = GetDashDirection(moveInput);
            _rigidbody.linearVelocity = dashDir * _settings.dashSpeed;

            _isDashing = true;
            _dashTimer = _settings.dashDuration;
            _cooldownTimer = _settings.dashCooldown;
            return true;
        }

        private Vector3 GetDashDirection(Vector2 moveInput)
        {
            if (moveInput != Vector2.zero)
            {
                Vector3 dir = _cameraTransform.forward * moveInput.y + _cameraTransform.right * moveInput.x;
                dir.y = 0;
                return dir.normalized;
            }
            else
            {
                return _cameraTransform.forward; // ��� �����, ���� ��� �����
            }
        }
    }
}