using Assets.Scripts.Data;
using System;
using UnityEngine;

namespace Player
{
    public class PlayerStamina
    {
        private readonly PlayerData _playerData;
        private float _currentStamina;
        private float _regenTimer;

        public event Action<float, float> OnStaminaChanged; // текущее, макс
        public bool IsExhausted => _currentStamina <= 0;

        public PlayerStamina(PlayerData playerData)
        {
            _playerData = playerData;
            _currentStamina = playerData.maxStamina;
        }

        public void Update(float deltaTime)
        {
            if (_regenTimer > 0)
            {
                _regenTimer -= deltaTime;
            }
            else if (_currentStamina < _playerData.maxStamina)
            {
                _currentStamina = Mathf.Min(_currentStamina + _playerData.staminaRegenRate * deltaTime, _playerData.maxStamina);
                OnStaminaChanged?.Invoke(_currentStamina, _playerData.maxStamina);
            }
        }

        public bool TryConsume(float amount)
        {
            if (_currentStamina < amount) return false;
            _currentStamina -= amount;
            _regenTimer = _playerData.staminaRegenDelay;
            OnStaminaChanged?.Invoke(_currentStamina, _playerData.maxStamina);
            return true;
        }

        public void Consume(float amount)
        {
            _currentStamina = Mathf.Max(0, _currentStamina - amount);
            _regenTimer = _playerData.staminaRegenDelay;
            OnStaminaChanged?.Invoke(_currentStamina, _playerData.maxStamina);
        }
    }
}