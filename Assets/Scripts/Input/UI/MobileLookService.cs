using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using VContainer.Unity;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Assets.Scripts.Input.UI
{
    public class MobileLookService : ITickable
    {
        public Vector2 LookDelta { get; private set; }

        private const float Deadzone = 0.25f;
        private const float SmoothTime = 0.035f;
        private const float MaxDelta = 35f;

        private Vector2 _currentDelta;
        private Vector2 _deltaVelocity;

        public void Tick()
        {
            Vector2 targetDelta = Vector2.zero;

            foreach (var touch in Touch.activeTouches)
            {
                // Левая часть экрана = движение
                if (touch.screenPosition.x < Screen.width * 0.4f)
                    continue;

                if (touch.phase != UnityEngine.InputSystem.TouchPhase.Moved)
                    continue;

                Vector2 delta = touch.delta;

                // Ограничение резких скачков
                delta.x = Mathf.Clamp(delta.x, -MaxDelta, MaxDelta);
                delta.y = Mathf.Clamp(delta.y, -MaxDelta, MaxDelta);

                targetDelta += delta;
            }

            // Deadzone для микро-дрожи
            if (targetDelta.sqrMagnitude < Deadzone * Deadzone)
            {
                targetDelta = Vector2.zero;
            }

            // Плавное сглаживание
            _currentDelta = Vector2.SmoothDamp(
                _currentDelta,
                targetDelta,
                ref _deltaVelocity,
                SmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );

            // Отсекаем почти нулевые значения
            if (_currentDelta.sqrMagnitude < 0.0001f)
            {
                _currentDelta = Vector2.zero;
            }

            LookDelta = _currentDelta;
        }
    }
}