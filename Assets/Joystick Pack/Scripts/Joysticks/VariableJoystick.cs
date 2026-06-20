using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class VariableJoystick : Joystick
{
    public float MoveThreshold { get => moveThreshold; set => moveThreshold = Mathf.Abs(value); }

    [Header("Dash Settings")]
    [SerializeField] private float dashThresholdRadius = 1.2f;        // порог срабатывания
    [SerializeField] private float dashResetThresholdRadius = 0.8f;   // порог сброса готовности
    [SerializeField] private float dashAngleThreshold = 30f;          // допустимый угол отклонения от вертикали
    public UnityEvent OnDash;

    [Header("Dash Visuals")]
    [SerializeField] private RectTransform dashIndicator;             // иконка дэша
    [SerializeField] private RectTransform arrowPointer;              // стрелка
    [SerializeField] private float indicatorDistance = 1.5f;          // должно быть > dashThresholdRadius
    [SerializeField] private float arrowAngleOffset = -90f;           // смещение, если стрелка смотрит вверх

    [SerializeField] private float moveThreshold = 1;
    [SerializeField] private JoystickType joystickType = JoystickType.Fixed;

    private Vector2 fixedPosition = Vector2.zero;
    private bool dashReady = true;
    private Vector2 lastLocalDelta;
    private float backgroundRadius;
    private bool isDragging = false;

    public void SetMode(JoystickType joystickType)
    {
        this.joystickType = joystickType;
        if (joystickType == JoystickType.Fixed)
        {
            background.anchoredPosition = fixedPosition;
            background.gameObject.SetActive(true);
        }
        else
            background.gameObject.SetActive(false);
    }

    protected override void Start()
    {
        base.Start();
        fixedPosition = background.anchoredPosition;
        SetMode(joystickType);

        backgroundRadius = background.sizeDelta.x / 2f;
        if (backgroundRadius <= 0f) backgroundRadius = 100f;

        if (dashIndicator != null) dashIndicator.gameObject.SetActive(false);
        if (arrowPointer != null) arrowPointer.gameObject.SetActive(false);

        // Проверка на корректность расстояния
        if (indicatorDistance <= dashThresholdRadius)
            Debug.LogWarning("indicatorDistance должен быть больше dashThresholdRadius для правильного отображения!");
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (joystickType != JoystickType.Fixed)
        {
            background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
            background.gameObject.SetActive(true);
        }

        lastLocalDelta = Vector2.zero;
        dashReady = true;
        isDragging = true;

        base.OnPointerDown(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        base.OnDrag(eventData);
        Vector2 touchLocalPos = ScreenPointToAnchoredPosition(eventData.position);
        lastLocalDelta = touchLocalPos - background.anchoredPosition;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (joystickType != JoystickType.Fixed)
            background.gameObject.SetActive(false);

        isDragging = false;
        dashReady = true;

        if (dashIndicator != null) dashIndicator.gameObject.SetActive(false);
        if (arrowPointer != null) arrowPointer.gameObject.SetActive(false);

        base.OnPointerUp(eventData);
    }

    private bool IsDirectionUp(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.001f) return false;
        float angle = Vector2.Angle(dir, Vector2.up);
        return angle <= dashAngleThreshold;
    }

    protected override void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, Camera cam)
    {
        // Динамическое смещение фона (только для Dynamic)
        if (joystickType == JoystickType.Dynamic && magnitude > moveThreshold)
        {
            Vector2 difference = normalised * (magnitude - moveThreshold) * radius;
            background.anchoredPosition += difference;
        }

        float currentDistance = lastLocalDelta.magnitude;
        bool isUp = IsDirectionUp(lastLocalDelta);

        // Логика дэша
        if (isUp && currentDistance >= dashThresholdRadius * backgroundRadius && dashReady)
        {
            OnDash?.Invoke();
            dashReady = false;
        }
        else if (currentDistance < dashResetThresholdRadius * backgroundRadius)
        {
            dashReady = true;
        }

        // --- Визуалы: показываем ТОЛЬКО при выполнении условий ---
        bool showVisuals = isDragging && isUp && currentDistance >= dashThresholdRadius * backgroundRadius;

        if (showVisuals)
        {
            dashIndicator.gameObject.SetActive(true);
            arrowPointer.gameObject.SetActive(true);

            Vector2 direction = lastLocalDelta.normalized;

            // Иконка дэша — на орбите, без вращения
            dashIndicator.anchoredPosition = direction * (indicatorDistance * backgroundRadius);
            dashIndicator.rotation = Quaternion.identity;

            // Стрелка — поворот с учётом смещения
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + arrowAngleOffset;
            arrowPointer.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            if (dashIndicator != null) dashIndicator.gameObject.SetActive(false);
            if (arrowPointer != null) arrowPointer.gameObject.SetActive(false);
        }

        base.HandleInput(magnitude, normalised, radius, cam);
    }
}

public enum JoystickType { Fixed, Floating, Dynamic }