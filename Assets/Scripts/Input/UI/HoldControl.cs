using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Input.UI
{
    public class HoldControl : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IDragHandler
    {
        [HideInInspector]
        public UnityAction<bool> Holding;

        [HideInInspector]
        public UnityAction<Vector2> Dragging;

        private Vector2 _delta;

        private void LateUpdate()
        {
            Dragging?.Invoke(_delta);

            _delta = Vector2.zero;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Holding?.Invoke(true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _delta = eventData.delta;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Holding?.Invoke(false);

            _delta = Vector2.zero;

            Dragging?.Invoke(Vector2.zero);
        }

        private void OnDisable()
        {
            Holding?.Invoke(false);

            _delta = Vector2.zero;

            Dragging?.Invoke(Vector2.zero);
        }
    }
}