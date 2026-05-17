using Data;
using System;
using UnityEngine;

namespace Player
{
    public class PlayerStamina
    {
        private readonly GameSettingsSO _settings;
        private float _currentStamina;
        private float _regenTimer;

        public event Action<float, float> OnStaminaChanged; // текущее, макс
        public bool IsExhausted => _currentStamina <= 0;

        public PlayerStamina(GameSettingsSO settings)
        {
            _settings = settings;
            _currentStamina = _settings.maxStamina;
        }

        public void Update(float deltaTime)
        {
            if (_regenTimer > 0)
            {
                _regenTimer -= deltaTime;
            }
            else if (_currentStamina < _settings.maxStamina)
            {
                _currentStamina = Mathf.Min(_currentStamina + _settings.staminaRegenRate * deltaTime, _settings.maxStamina);
                OnStaminaChanged?.Invoke(_currentStamina, _settings.maxStamina);
            }
        }

        public bool TryConsume(float amount)
        {
            if (_currentStamina < amount) return false;
            _currentStamina -= amount;
            _regenTimer = _settings.staminaRegenDelay;
            OnStaminaChanged?.Invoke(_currentStamina, _settings.maxStamina);
            return true;
        }

        public void Consume(float amount)
        {
            _currentStamina = Mathf.Max(0, _currentStamina - amount);
            _regenTimer = _settings.staminaRegenDelay;
            OnStaminaChanged?.Invoke(_currentStamina, _settings.maxStamina);
        }
    }
}