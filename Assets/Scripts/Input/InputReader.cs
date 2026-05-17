using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Input
{
    public class InputReader : IDisposable
    {
        private readonly PlayerInputActions _inputActions;

        // Одноразовые действия
        public event Action OnJumpPerformed;
        public event Action OnDashPerformed;
        public event Action OnKickPerformed;

        public event Action OnReloadPerformed;       // ПКМ (однократно)
        public event Action<float> OnChangeWeaponPerformed; // Скролл
        public event Action<int> OnSelectInventorySlotPerformed;

        // Состояния кнопки Use (ЛКМ) – для автоматической стрельбы
        public event Action OnUseStarted;    // Зажали
        public event Action OnUseCanceled;   // Отпустили
        public bool IsUsePressed => _inputActions.Player.Use.IsPressed(); // Текущее состояние

        public Vector2 Move => _inputActions.Player.Move.ReadValue<Vector2>();
        public Vector2 Look => _inputActions.Player.Look.ReadValue<Vector2>();

        public event Action OnPausePerformed;

        public InputReader()
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
            _inputActions.Player.SelectInventorySlot.performed += ctx =>
            {
                var keyControl = ctx.control as KeyControl;
                if (keyControl == null) return;

                // 1 -> индекс 0, 2 -> 1, ..., 0 -> 9 (если нужна клавиша 0)
                int slotIndex;
                if (keyControl.keyCode == Key.Digit0)
                    slotIndex = 9;   // подстрой под свой размер инвентаря
                else
                    slotIndex = keyControl.keyCode - Key.Digit1;   // 0..8

                OnSelectInventorySlotPerformed?.Invoke(slotIndex);
            };

            // Use (ЛКМ) – начало и конец
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