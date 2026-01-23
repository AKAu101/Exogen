#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(ExtendedButton), true)]
[CanEditMultipleObjects]
public class ExtendedButtonEditor : ButtonEditor
{
    SerializedProperty text;
    SerializedProperty defaultColor;
    SerializedProperty highlightColor;
    SerializedProperty pressedColor;
    SerializedProperty selectedColor;
    SerializedProperty disabledColor;

    protected override void OnEnable()
    {
        base.OnEnable();

        text = serializedObject.FindProperty("text");
        defaultColor = serializedObject.FindProperty("defaultColor");
        highlightColor = serializedObject.FindProperty("highlightColor");
        pressedColor = serializedObject.FindProperty("pressedColor");
        selectedColor = serializedObject.FindProperty("selectedColor");
        disabledColor = serializedObject.FindProperty("disabledColor");
    }

    public override void OnInspectorGUI()
    {
        // Draw the normal Unity Button inspector first
        base.OnInspectorGUI();

        // Then draw your extra fields
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Extended Button", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(text);
        EditorGUILayout.PropertyField(defaultColor);
        EditorGUILayout.PropertyField(highlightColor);
        EditorGUILayout.PropertyField(pressedColor);
        EditorGUILayout.PropertyField(selectedColor);
        EditorGUILayout.PropertyField(disabledColor);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
