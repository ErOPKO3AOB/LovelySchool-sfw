using Assets.Scripts.Data;
using UnityEngine;

namespace Player
{
    public class PlayerJump
    {
        private readonly Rigidbody _rigidbody;
        private readonly PlayerData _playerData;
        private readonly Transform _groundCheck;

        public PlayerJump(Rigidbody rigidbody, PlayerData playerData, Transform groundCheck)
        {
            _rigidbody = rigidbody;
            _playerData = playerData;
            _groundCheck = groundCheck;
        }

        public bool IsGrounded
        {
            get
            {
                Collider[] hits = Physics.OverlapSphere(_groundCheck.position, _playerData.groundCheckRadius, _playerData.groundLayer);
                foreach (var hit in hits)
                {
                    // Игнорируем коллайдер, который принадлежит самому игроку (или его детям)
                    if (!hit.transform.IsChildOf(_rigidbody.transform))
                        return true;
                }
                return false;
            }
        }

        public void Jump()
        {
            if (!IsGrounded) return;
            _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
            _rigidbody.AddForce(Vector3.up * _playerData.jumpForce, ForceMode.Impulse);
        }
    }
}