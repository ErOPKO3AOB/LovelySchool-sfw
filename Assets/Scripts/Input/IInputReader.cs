using System;
using UnityEngine;

namespace Assets.Scripts.Input
{
    public interface IInputReader : IDisposable
    {
        event Action OnJumpPerformed;
        event Action OnDashPerformed;
        event Action OnKickPerformed;

        event Action OnReloadPerformed;
        event Action<float> OnChangeWeaponPerformed;
        event Action<int> OnSelectInventorySlotPerformed;

        event Action OnUseStarted;    // Зажали
        event Action OnUseCanceled;   // Отпустили

        public Vector2 Move { get; }
        bool IsUsePressed { get; }
        public Vector2 Look { get; }

        public event Action OnPausePerformed;
    }
}