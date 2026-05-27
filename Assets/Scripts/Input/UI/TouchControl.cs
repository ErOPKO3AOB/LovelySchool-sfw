using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Input.UI
{
    public class TouchControl : MonoBehaviour,
        IDragHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        [HideInInspector]
        public UnityAction<Vector2> Input;

        private Vector2 _currentDelta;

        private void LateUpdate()
        {
            // Отдаем delta
            Input?.Invoke(_currentDelta);

            // И сразу сбрасываем
            _currentDelta = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            _currentDelta = eventData.delta;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _currentDelta = Vector2.zero;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _currentDelta = Vector2.zero;

            Input?.Invoke(Vector2.zero);
        }
    }
}