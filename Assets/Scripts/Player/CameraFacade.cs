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

            // Управление целью наклона при ударе
            if (_player.LegController.IsKicking)
            {
                // Можно менять знак в зависимости от ноги (_player.LegController.WhichLeg?)
                _kickTiltTarget = kickTiltAmount; // или -kickTiltAmount
            }
            else
            {
                _kickTiltTarget = 0f;
            }

            // Плавный наклон удара
            _kickTiltCurrent = Mathf.SmoothDamp(_kickTiltCurrent, _kickTiltTarget,
                ref _kickTiltVelocity, kickTiltSmoothTime);

            if (_player.Health.IsDead)
            {
                transform.parent = null;
                transform.LookAt(_player.RagdollRb.position);
                return;
            }

            // Ввод мыши (теперь обрабатывается всегда)
            Vector2 lookDelta = YG2.envir.isDesktop ?
            _input.Look * _gameSettingsSO.mouseSensitivity * Time.deltaTime :
            _input.Look * _gameSettingsSO.mouseSensitivity * 0.005f;

            // Вертикальный поворот
            xRotation -= lookDelta.y;
            xRotation = Mathf.Clamp(xRotation, -verticalClamp, verticalClamp);

            // Наклон от скорости горизонтального поворота
            float targetTilt = -lookDelta.x * tiltAmount;
            currentTilt = Mathf.SmoothDamp(currentTilt, targetTilt, ref tiltVelocity, tiltSmoothTime);

            // Итоговый поворот: добавляем наклон от удара к уже имеющемуся
            transform.rotation = _playerTransform.rotation *
                                 Quaternion.Euler(xRotation, 0f, currentTilt + _kickTiltCurrent);

            // Горизонтальный поворот игрока
            if (lookDelta.x != 0)
                _player.RotateHorizontally(lookDelta.x);

            // Позиция камеры (сглаженное смещение при приседании)
            float targetY = offset.y;
            targetOffset = new Vector3(offset.x, targetY, offset.z);
            Vector3 currentOffset = Vector3.SmoothDamp(transform.localPosition, targetOffset,
                ref currentOffsetVelocity, verticalSmoothTime);

            Vector3 worldOffset = _playerTransform.rotation * currentOffset;
            transform.position = _playerTransform.position + worldOffset;
        }
    }
}