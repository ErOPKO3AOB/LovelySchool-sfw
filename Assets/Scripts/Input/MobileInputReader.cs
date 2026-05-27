using Assets.Scripts.Input.UI;
using System;
using UnityEngine;

namespace Assets.Scripts.Input
{
    public class MobileInputReader : IInputReader
    {
        public event Action OnJumpPerformed;
        public event Action OnDashPerformed;
        public event Action OnKickPerformed;
        public event Action OnReloadPerformed;
        public event Action<float> OnChangeWeaponPerformed;
        public event Action<int> OnSelectInventorySlotPerformed;
        public event Action OnUseStarted;
        public event Action OnUseCanceled;
        public event Action OnPausePerformed;
        public bool IsUsePressed => _isUsePressed;
        public bool _isUsePressed;
        public Vector2 Move => _mobileCanvasController.joystick.Direction;
        public Vector2 Look => _look;
        private Vector2 _look;

        private readonly MobileCanvasController _mobileCanvasController;

        public MobileInputReader(MobileCanvasController mobileCanvasController)
        {
            _mobileCanvasController = mobileCanvasController;

            // Jump
            _mobileCanvasController.jumpButton.onClick.AddListener(() => OnJumpPerformed?.Invoke());
            // Dash
            _mobileCanvasController.dashButton.onClick.AddListener(() => OnDashPerformed?.Invoke());
            _mobileCanvasController.kickButton.onClick.AddListener(() => OnKickPerformed?.Invoke());

            // Reload (однократно)
            _mobileCanvasController.reloadButton.onClick.AddListener(() => OnReloadPerformed?.Invoke());

            _mobileCanvasController.OnSlotChoose += slotIndex =>
            {
                OnSelectInventorySlotPerformed?.Invoke(slotIndex);
            };
            
            _mobileCanvasController.attackHoldButton.holding += (holding) =>
            {
                _isUsePressed = holding;
                if (_isUsePressed)
                    OnUseStarted?.Invoke();
                else
                    OnUseCanceled?.Invoke();
            };

            _mobileCanvasController.pauseButton.onClick.AddListener(() => OnPausePerformed?.Invoke());

            _mobileCanvasController.lookControl.Input += (delta) => _look = delta;
        }

        public void Dispose()
        {
            _mobileCanvasController.Dispose();
            OnChangeWeaponPerformed?.Invoke(0);
            OnChangeWeaponPerformed = null;
        }
    }
}