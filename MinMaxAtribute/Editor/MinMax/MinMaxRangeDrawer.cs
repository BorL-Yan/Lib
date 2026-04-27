using UnityEngine;
using UnityEditor;
using Lib;

[CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
public class MinMaxRangeDrawer : PropertyDrawer
{
    private const float FieldWidth = 50f;
    private const float Padding = 5f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        MinMaxRangeAttribute rangeAttribute = (MinMaxRangeAttribute)attribute;

        label = EditorGUI.BeginProperty(position, label, property);
        
        Rect contentPosition = EditorGUI.PrefixLabel(position, label);

        int originalIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        Rect minFieldRect = new Rect(contentPosition.x, contentPosition.y, FieldWidth, contentPosition.height);
        Rect maxFieldRect = new Rect(contentPosition.x + contentPosition.width - FieldWidth, contentPosition.y, FieldWidth, contentPosition.height);
        Rect sliderRect = new Rect(
            contentPosition.x + FieldWidth + Padding, 
            contentPosition.y, 
            contentPosition.width - (FieldWidth * 2) - (Padding * 2), 
            contentPosition.height
        );

        if (property.propertyType == SerializedPropertyType.Vector2)
        {
            float minVal = property.vector2Value.x;
            float maxVal = property.vector2Value.y;

            minVal = EditorGUI.FloatField(minFieldRect, float.Parse(minVal.ToString("F2"))); 
            
            maxVal = EditorGUI.FloatField(maxFieldRect, float.Parse(maxVal.ToString("F2")));

            minVal = Mathf.Clamp(minVal, rangeAttribute.minLimit, maxVal);
            maxVal = Mathf.Clamp(maxVal, minVal, rangeAttribute.maxLimit);

            EditorGUI.MinMaxSlider(sliderRect, ref minVal, ref maxVal, rangeAttribute.minLimit, rangeAttribute.maxLimit);

            property.vector2Value = new Vector2(minVal, maxVal);
        }
        else if (property.propertyType == SerializedPropertyType.Vector2Int)
        {
            int minVal = property.vector2IntValue.x;
            int maxVal = property.vector2IntValue.y;
            float minFloat = minVal;
            float maxFloat = maxVal;

            minVal = EditorGUI.IntField(minFieldRect, minVal);
            
            maxVal = EditorGUI.IntField(maxFieldRect, maxVal);

            if (minVal < rangeAttribute.minLimit) minVal = (int)rangeAttribute.minLimit;
            if (maxVal > rangeAttribute.maxLimit) maxVal = (int)rangeAttribute.maxLimit;
            if (minVal > maxVal) minVal = maxVal;

            EditorGUI.MinMaxSlider(sliderRect, ref minFloat, ref maxFloat, rangeAttribute.minLimit, rangeAttribute.maxLimit);

            if (Mathf.Abs(minFloat - minVal) > 0.01f) minVal = Mathf.RoundToInt(minFloat);
            if (Mathf.Abs(maxFloat - maxVal) > 0.01f) maxVal = Mathf.RoundToInt(maxFloat);

            property.vector2IntValue = new Vector2Int(minVal, maxVal);
        }

        EditorGUI.indentLevel = originalIndent;
        EditorGUI.EndProperty();
    }
}