using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Input.UI
{
    public class HoldControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [HideInInspector]
        public UnityAction<bool> holding;

        public void OnPointerDown(PointerEventData eventData)
        {
            holding?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            holding?.Invoke(false);
        }
    }
}