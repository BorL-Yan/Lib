using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


[CustomEditor(typeof(Transform))]
[CanEditMultipleObjects]
public class CustomTransformEditor : Editor
{
    private static readonly Dictionary<int, SavedTransformData> _pendingChanges = new Dictionary<int, SavedTransformData>();
    private static readonly HashSet<int> _lockedScales = new HashSet<int>();

    private class SavedTransformData
    {
        public Vector3? Pos;
        public Quaternion? Rot;
        public Vector3? Scale;
    }

    private SerializedProperty m_Pos, m_Rot, m_Scale;

    private void OnEnable()
    {
        if (target == null || targets == null || targets.Length == 0 || targets[0] == null)
            return;

        m_Pos = serializedObject.FindProperty("m_LocalPosition");
        m_Rot = serializedObject.FindProperty("m_LocalRotation");
        m_Scale = serializedObject.FindProperty("m_LocalScale");

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    public override void OnInspectorGUI()
    {
        if (serializedObject == null || serializedObject.targetObject == null) 
            return;
        serializedObject.Update();
        float originalLabelWidth = EditorGUIUtility.labelWidth;
        
        EditorGUIUtility.labelWidth = 70;

        DrawPropertyRow("Position", m_Pos, Vector3.zero);
        DrawPropertyRow("Rotation", m_Rot, Quaternion.identity);
        DrawScaleRow();

        EditorGUIUtility.labelWidth = originalLabelWidth;
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPropertyRow(string label, SerializedProperty property, object resetValue)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth));

        if (property.propertyType == SerializedPropertyType.Quaternion)
        {
            Vector3 euler = property.quaternionValue.eulerAngles;
            EditorGUI.BeginChangeCheck();
            euler = EditorGUILayout.Vector3Field(GUIContent.none, euler);
            if (EditorGUI.EndChangeCheck())
            {
                property.quaternionValue = Quaternion.Euler(euler);
            }
        }
        else
        {
            EditorGUILayout.PropertyField(property, GUIContent.none);
        }

        GUILayout.Space(5);
        DrawActionButtons(label, property, resetValue);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawScaleRow()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Scale", GUILayout.Width(EditorGUIUtility.labelWidth - 25));

        int id = target.GetInstanceID();
        bool isLocked = _lockedScales.Contains(id);
        
        GUIContent lockIcon = EditorGUIUtility.IconContent(isLocked ? "Linked" : "Unlinked");
        lockIcon.tooltip = "Равномерное масштабирование";

        if (GUILayout.Button(lockIcon, EditorStyles.iconButton, GUILayout.Width(22), GUILayout.Height(18)))
        {
            if (isLocked) _lockedScales.Remove(id);
            else _lockedScales.Add(id);
        }

        EditorGUI.BeginChangeCheck();
        Vector3 oldScale = m_Scale.vector3Value;
        Vector3 newScale = EditorGUILayout.Vector3Field(GUIContent.none, oldScale);

        if (EditorGUI.EndChangeCheck())
        {
            if (isLocked)
            {
                float ratio = 1f;
                if (!Mathf.Approximately(newScale.x, oldScale.x)) 
                    ratio = oldScale.x != 0 ? newScale.x / oldScale.x : newScale.x;
                else if (!Mathf.Approximately(newScale.y, oldScale.y)) 
                    ratio = oldScale.y != 0 ? newScale.y / oldScale.y : newScale.y;
                else if (!Mathf.Approximately(newScale.z, oldScale.z)) 
                    ratio = oldScale.z != 0 ? newScale.z / oldScale.z : newScale.z;
                if (ratio != 0)
                {
                    m_Scale.vector3Value = oldScale * ratio;
                }
            }
            else
            {
                m_Scale.vector3Value = newScale;
            }
        }

        GUILayout.Space(5);
        DrawActionButtons("Scale", m_Scale, Vector3.one);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawActionButtons(string label, SerializedProperty property, object resetValue)
    {
        bool isPlaying = Application.isPlaying;

        GUI.enabled = !isPlaying;
        if (!isPlaying)
        {
            GUI.backgroundColor = isPlaying ? Color.gray : Color.cyan;
            
            GUIContent resetContent = new GUIContent("R", "Click to reset the variables.");
            if (GUILayout.Button(resetContent, GUILayout.Width(25), GUILayout.Height(18)))
            {
                if (resetValue is Vector3 v3) property.vector3Value = v3;
                else if (resetValue is Quaternion q) property.quaternionValue = q;
            }
            
            GUI.backgroundColor = Color.white;
        }
        
        GUI.enabled = true;

        if (isPlaying)
        {
            int id = target.GetInstanceID();        
            bool isSaved = IsValueMatchingSaved(label, id, property);

            GUI.backgroundColor = isSaved ? Color.green : Color.yellow;
            
            string saveTooltip = isSaved 
                ? "The value is saved! It will be applied after exiting. Click to cancel." 
                : "Click to save " + label + " and apply it later.";
            
            if (GUILayout.Button(new GUIContent("S", saveTooltip), GUILayout.Width(25), GUILayout.Height(18)))
            {
                if (isSaved) RemoveSavedValue(label, id);
                else SaveCurrentValue(label, id, property);
            }
            GUI.backgroundColor = Color.white;
        }
    }
    
    private bool IsValueMatchingSaved(string label, int id, SerializedProperty prop)
    {
        if (!_pendingChanges.TryGetValue(id, out var data)) return false;
        if (label == "Position") return data.Pos.HasValue && data.Pos.Value == prop.vector3Value;
        if (label == "Rotation") return data.Rot.HasValue && data.Rot.Value == prop.quaternionValue;
        if (label == "Scale") return data.Scale.HasValue && data.Scale.Value == prop.vector3Value;
        return false;
    }

    private void SaveCurrentValue(string label, int id, SerializedProperty prop)
    {
        if (!_pendingChanges.ContainsKey(id)) _pendingChanges[id] = new SavedTransformData();
        var data = _pendingChanges[id];
        if (label == "Position") data.Pos = prop.vector3Value;
        else if (label == "Rotation") data.Rot = prop.quaternionValue;
        else if (label == "Scale") data.Scale = prop.vector3Value;
    }

    private void RemoveSavedValue(string label, int id)
    {
        if (_pendingChanges.TryGetValue(id, out var data))
        {
            if (label == "Position") data.Pos = null;
            else if (label == "Rotation") data.Rot = null;
            else if (label == "Scale") data.Scale = null;
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            foreach (var entry in _pendingChanges)
            {
                Transform t = EditorUtility.InstanceIDToObject(entry.Key) as Transform;
                if (t != null)
                {
                    Undo.RecordObject(t, "Apply Saved Playmode Transform");
                    if (entry.Value.Pos.HasValue) t.localPosition = entry.Value.Pos.Value;
                    if (entry.Value.Rot.HasValue) t.localRotation = entry.Value.Rot.Value;
                    if (entry.Value.Scale.HasValue) t.localScale = entry.Value.Scale.Value;
                    EditorUtility.SetDirty(t);
                }
            }
            _pendingChanges.Clear();
        }
    }
}