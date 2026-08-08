using Assets.Scripts.Input;
using Data;
using UnityEngine;
using YG;

namespace Player
{
    public class CameraFacade : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float verticalClamp = 90f;
        [SerializeField] private Vector3 offset;          // смещение от центра игрока
        [SerializeField] private float tiltAmount = 2f;
        [SerializeField] private float tiltSmoothTime = 0.1f;
        [SerializeField] private float verticalSmoothTime = 0.1f;

        [SerializeField] private float kickTiltAmount = 4f;   // максимальный угол наклона при ударе
        [SerializeField] private float kickTiltSmoothTime = 0.15f;

        private float _kickTiltTarget = 0f;
        private float _kickTiltCurrent = 0f;
        private float _kickTiltVelocity = 0f;

        private IInputReader _input;
        private PlayerFacade _player;
        private Transform _playerTransform;

        private float xRotation = 0f;
        private float currentTilt = 0f;
        private float tiltVelocity = 0f;
        private Vector3 targetOffset;
        private Vector3 currentOffsetVelocity;

        public Camera Camera { get { return _camera; } }

        private GameSettingsSO _gameSettingsSO;

        public void Initialize(IInputReader input, PlayerFacade player, GameSettingsSO settings)
        {
            _input = input;
            _player = player;
            _playerTransform = player.transform;

            // Применить настройки
            _gameSettingsSO = settings;
            verticalClamp = settings.verticalClamp;
            offset = settings.cameraOffset;
            tiltAmount = settings.tiltAmount;
            tiltSmoothTime = settings.tiltSmoothTime;

            if (YG2.envir.device == YG2.Device.Mobile || YG2.envir.device == YG2.Device.Tablet)
            {
                Cursor.lockState = CursorLockMode.None;
            }

            else
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            targetOffset = offset;
        }

        private void LateUpdate()
        {
            if (_input == null) return;

            // 1. УПРАВЛЕНИЕ НАКЛОНОМ ОТ УДАРА (Kick Tilt)
            if (_player.LegController.IsKicking)
            {
                _kickTiltTarget = kickTiltAmount;
            }
            else
            {
                _kickTiltTarget = 0f;
            }

            // Важно: SmoothDamp чувствителен к deltaTime внутри себя. 
            // Если lag большой, smoothTime должен быть больше, иначе камера дернется резко.
            // Мы используем raw deltaTime здесь, это правильно для интерполяции состояний.
            _kickTiltCurrent = Mathf.SmoothDamp(_kickTiltCurrent, _kickTiltTarget, ref _kickTiltVelocity, kickTiltSmoothTime);

            if (_player.Health.IsDead)
            {
                transform.parent = null;
                // Защита от null, если Ragdoll еще не создан
                if (_player.RagdollRb != null)
                    transform.LookAt(_player.RagdollRb.position);
                return;
            }

            // 2. ИСПРАВЛЕНИЕ ВВОДА (ГЛАВНАЯ ПРОБЛЕМА)
            // Убираем умножение на deltaTime из чувствительности мыши!
            // Чувствительность должна быть "градусов в секунду" или просто коэффициентом, 
            // но не умножаться на время здесь, если ввод уже нормализован.

            float sensitivityMultiplier = YG2.envir.isDesktop
                ? _gameSettingsSO.mouseSensitivity
                : _gameSettingsSO.mouseSensitivity * 0.5f; // Подбери коэффициент для мобилок отдельно, без deltaTime

            Vector2 lookDelta = _input.Look * sensitivityMultiplier;

            // Вертикальный поворот
            xRotation -= lookDelta.y;
            xRotation = Mathf.Clamp(xRotation, -verticalClamp, verticalClamp);

            // Наклон от скорости горизонтального поворота
            // Теперь lookDelta.x - это просто величина поворота за этот кадр.
            float targetTilt = -lookDelta.x * tiltAmount;

            // Оптимизация SmoothDamp для стабильности при лагах
            // При очень низком FPS (большом deltaTime) smoothTime нужно увеличивать, 
            // чтобы камера не пыталась догнать цель за один кадр.
            float effectiveTiltSmooth = tiltSmoothTime + Time.deltaTime;
            currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, effectiveTiltSmooth);

            // Итоговый поворот
            transform.rotation = _playerTransform.rotation *
                                 Quaternion.Euler(xRotation, 0f, currentTilt + _kickTiltCurrent);

            // Горизонтальный поворот игрока
            // Здесь тоже лучше не использовать deltaTime, если RotateHorizontally ожидает угол поворота
            if (lookDelta.x != 0)
                _player.RotateHorizontally(lookDelta.x);

            // Позиция камеры
            float targetY = offset.y;
            targetOffset = new Vector3(offset.x, targetY, offset.z);

            // Аналогично для позиции: добавляем deltaTime к времени сглаживания, 
            // чтобы при лаге камера двигалась плавнее, а не телепортировалась
            float effectivePosSmooth = verticalSmoothTime + Time.deltaTime;
            Vector3 currentOffset = Vector3.SmoothDamp(transform.localPosition, targetOffset, ref currentOffsetVelocity, effectivePosSmooth);

            Vector3 worldOffset = _playerTransform.rotation * currentOffset;
            transform.position = _playerTransform.position + worldOffset;
        }

    }
}