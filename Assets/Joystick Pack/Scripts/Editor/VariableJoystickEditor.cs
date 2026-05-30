using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VariableJoystick))]
public class VariableJoystickEditor : Editor
{
    // Базовые поля Joystick
    private SerializedProperty handleRange;
    private SerializedProperty deadZone;
    private SerializedProperty axisOptions;
    private SerializedProperty snapX;
    private SerializedProperty snapY;
    private SerializedProperty background;
    private SerializedProperty handle;

    // Специфичные для VariableJoystick
    private SerializedProperty moveThreshold;
    private SerializedProperty joystickType;

    // Dash
    private SerializedProperty dashThresholdRadius;
    private SerializedProperty dashResetThresholdRadius;
    private SerializedProperty dashAngleThreshold;
    private SerializedProperty onDash;

    // Dash визуал
    private SerializedProperty dashIndicator;
    private SerializedProperty indicatorDistance;

    private Vector2 center = new Vector2(0.5f, 0.5f);

    protected virtual void OnEnable()
    {
        handleRange = serializedObject.FindProperty("handleRange");
        deadZone = serializedObject.FindProperty("deadZone");
        axisOptions = serializedObject.FindProperty("axisOptions");
        snapX = serializedObject.FindProperty("snapX");
        snapY = serializedObject.FindProperty("snapY");
        background = serializedObject.FindProperty("background");
        handle = serializedObject.FindProperty("handle");

        moveThreshold = serializedObject.FindProperty("moveThreshold");
        joystickType = serializedObject.FindProperty("joystickType");

        dashThresholdRadius = serializedObject.FindProperty("dashThresholdRadius");
        dashResetThresholdRadius = serializedObject.FindProperty("dashResetThresholdRadius");
        dashAngleThreshold = serializedObject.FindProperty("dashAngleThreshold");
        onDash = serializedObject.FindProperty("OnDash");

        dashIndicator = serializedObject.FindProperty("dashIndicator");
        indicatorDistance = serializedObject.FindProperty("indicatorDistance");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawValues();
        EditorGUILayout.Space();
        DrawComponents();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Variable Joystick", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(moveThreshold, new GUIContent("Move Threshold"));
        EditorGUILayout.PropertyField(joystickType, new GUIContent("Joystick Type"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dash Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(dashThresholdRadius, new GUIContent("Threshold Radius", "Множитель радиуса для срабатывания (>1 = за пределами фона)"));
        EditorGUILayout.PropertyField(dashResetThresholdRadius, new GUIContent("Reset Radius", "Множитель радиуса для повторного дэша"));
        EditorGUILayout.PropertyField(dashAngleThreshold, new GUIContent("Angle Threshold", "Допустимый угол отклонения от вертикали (0 = строго вверх)"));
        EditorGUILayout.PropertyField(onDash);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dash Visuals", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(dashIndicator, new GUIContent("Indicator (RectTransform)", "Визуальный элемент DashZone (дочерний к Background)"));
        EditorGUILayout.PropertyField(indicatorDistance, new GUIContent("Indicator Distance", "Расстояние индикатора от центра (множитель радиуса)"));

        serializedObject.ApplyModifiedProperties();

        // Фиксим pivot фона и ручки
        if (background != null && background.objectReferenceValue != null)
        {
            RectTransform bg = (RectTransform)background.objectReferenceValue;
            bg.pivot = center;
        }
        if (handle != null && handle.objectReferenceValue != null)
        {
            RectTransform h = (RectTransform)handle.objectReferenceValue;
            h.anchorMax = center;
            h.anchorMin = center;
            h.pivot = center;
            h.anchoredPosition = Vector2.zero;
        }
    }

    protected virtual void DrawValues()
    {
        EditorGUILayout.PropertyField(handleRange, new GUIContent("Handle Range"));
        EditorGUILayout.PropertyField(deadZone, new GUIContent("Dead Zone"));
        EditorGUILayout.PropertyField(axisOptions, new GUIContent("Axis Options"));
        EditorGUILayout.PropertyField(snapX, new GUIContent("Snap X"));
        EditorGUILayout.PropertyField(snapY, new GUIContent("Snap Y"));
    }

    protected virtual void DrawComponents()
    {
        EditorGUILayout.ObjectField(background, new GUIContent("Background"));
        EditorGUILayout.ObjectField(handle, new GUIContent("Handle"));
    }
}