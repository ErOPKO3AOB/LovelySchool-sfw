using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class VariableJoystick : Joystick
{
    public float MoveThreshold { get => moveThreshold; set => moveThreshold = Mathf.Abs(value); }

    [Header("Dash Settings")]
    [SerializeField] private float dashThresholdRadius = 1.2f;        // порог срабатывания (множитель радиуса)
    [SerializeField] private float dashResetThresholdRadius = 0.8f;   // порог сброса готовности
    [SerializeField] private float dashAngleThreshold = 30f;          // допустимый угол отклонения от вертикали (в градусах)
    public UnityEvent OnDash;

    [Header("Dash Visuals")]
    [SerializeField] private RectTransform dashIndicator;             // ссылка на RectTransform визуала DashZone
    [SerializeField] private float indicatorDistance = 1.4f;          // расстояние индикатора от центра (множитель радиуса)

    [SerializeField] private float moveThreshold = 1;
    [SerializeField] private JoystickType joystickType = JoystickType.Fixed;

    private Vector2 fixedPosition = Vector2.zero;
    private bool dashReady = true;
    private Vector2 lastLocalDelta;           // смещение пальца от центра джойстика
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

        if (dashIndicator != null)
            dashIndicator.gameObject.SetActive(false);
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

        if (dashIndicator != null)
            dashIndicator.gameObject.SetActive(false);

        base.OnPointerUp(eventData);
    }

    /// <summary>
    /// Проверяет, находится ли вектор направления в допустимом секторе "вверх".
    /// </summary>
    private bool IsDirectionUp(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.001f) return false;
        float angle = Vector2.Angle(dir, Vector2.up);
        return angle <= dashAngleThreshold;
    }

    protected override void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, Camera cam)
    {
        // Динамическое перемещение фона (если нужно)
        if (joystickType == JoystickType.Dynamic && magnitude > moveThreshold)
        {
            Vector2 difference = normalised * (magnitude - moveThreshold) * radius;
            background.anchoredPosition += difference;
        }

        // Расстояние от центра до пальца
        float currentDistance = lastLocalDelta.magnitude;
        bool isUp = IsDirectionUp(lastLocalDelta);

        // --- ДЭШ ТОЛЬКО ВВЕРХ ---
        if (isUp && currentDistance >= dashThresholdRadius * backgroundRadius && dashReady)
        {
            OnDash?.Invoke();
            dashReady = false;
        }
        else if (currentDistance < dashResetThresholdRadius * backgroundRadius)
        {
            dashReady = true;
        }

        // --- ИНДИКАТОР (показываем только когда тянем вверх) ---
        if (dashIndicator != null && isDragging && isUp)
        {
            dashIndicator.gameObject.SetActive(true);
            // Позиция строго вверх по Y на нужном расстоянии (не вращаем)
            dashIndicator.anchoredPosition = new Vector2(0f, indicatorDistance * backgroundRadius);
        }
        else if (dashIndicator != null)
        {
            dashIndicator.gameObject.SetActive(false);
        }

        base.HandleInput(magnitude, normalised, radius, cam);
    }
}

public enum JoystickType { Fixed, Floating, Dynamic }