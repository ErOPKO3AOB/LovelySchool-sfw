using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Assets.Scripts.Input
{
    public class StandaloneInputReader : IInputReader
    {
        private readonly PlayerInputActions _inputActions;

        public event Action OnJumpPerformed;
        public event Action OnDashPerformed;
        public event Action OnKickPerformed;
        public event Action OnReloadPerformed;
        public event Action<float> OnChangeWeaponPerformed;
        public event Action<int> OnSelectInventorySlotPerformed;
        public event Action OnUseStarted;
        public event Action OnUseCanceled;
        public event Action OnPausePerformed;
        public bool IsUsePressed => _inputActions.Player.Use.IsPressed();
        public Vector2 Move => _inputActions.Player.Move.ReadValue<Vector2>();
        public Vector2 Look => _inputActions.Player.Look.ReadValue<Vector2>();


        public StandaloneInputReader()
        {
            _inputActions = new PlayerInputActions();

            // Jump
            _inputActions.Player.Jump.performed += ctx => OnJumpPerformed?.Invoke();
            // Dash
            _inputActions.Player.Dash.performed += ctx => OnDashPerformed?.Invoke();
            _inputActions.Player.Kick.performed += ctx => OnKickPerformed?.Invoke();

            // Reload (однократно)
            _inputActions.Player.Reload.performed += ctx => OnReloadPerformed?.Invoke();
            // ChangeWeapon (скролл)
            _inputActions.Player.ChangeWeapon.performed += ctx =>
            {
                Vector2 scrollValue = ctx.ReadValue<Vector2>();
                float delta = scrollValue.y;
                if (Mathf.Abs(delta) > 0.25f)
                    OnChangeWeaponPerformed?.Invoke(delta);
            };
            _inputActions.Player.SelectInventorySlot.started += ctx =>
            {
                var keyControl = ctx.control as KeyControl;

                if (keyControl == null)
                    return;

                int slotIndex;

                if (keyControl.keyCode == Key.Digit0)
                    slotIndex = 9;
                else
                    slotIndex = keyControl.keyCode - Key.Digit1;

                OnSelectInventorySlotPerformed?.Invoke(slotIndex);
            };

            // Use (Ћ ћ) Ц начало и конец
            _inputActions.Player.Use.started += ctx => OnUseStarted?.Invoke();
            _inputActions.Player.Use.canceled += ctx => OnUseCanceled?.Invoke();

            _inputActions.Player.Pause.performed += ctx => OnPausePerformed?.Invoke();

            _inputActions.Enable();
        }

        public void Dispose()
        {
            _inputActions.Disable();
            _inputActions.Dispose();
        }
    }
}