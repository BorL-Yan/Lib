using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
public class MinMaxRangeDrawer : PropertyDrawer
{
    // Настройки ширины полей и отступов
    private const float FieldWidth = 50f;
    private const float Padding = 5f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Получаем атрибут с лимитами
        MinMaxRangeAttribute rangeAttribute = (MinMaxRangeAttribute)attribute;

        // Начинаем отрисовку свойства
        label = EditorGUI.BeginProperty(position, label, property);
        
        // Рисуем заголовок переменной и получаем Rect для оставшейся части (справа)
        Rect contentPosition = EditorGUI.PrefixLabel(position, label);

        // Убираем отступ, чтобы элементы встали ровно
        int originalIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Рассчитываем прямоугольники для: левого поля, слайдера, правого поля
        Rect minFieldRect = new Rect(contentPosition.x, contentPosition.y, FieldWidth, contentPosition.height);
        Rect maxFieldRect = new Rect(contentPosition.x + contentPosition.width - FieldWidth, contentPosition.y, FieldWidth, contentPosition.height);
        Rect sliderRect = new Rect(
            contentPosition.x + FieldWidth + Padding, 
            contentPosition.y, 
            contentPosition.width - (FieldWidth * 2) - (Padding * 2), 
            contentPosition.height
        );

        // Логика для Vector2 (Float)
        if (property.propertyType == SerializedPropertyType.Vector2)
        {
            float minVal = property.vector2Value.x;
            float maxVal = property.vector2Value.y;

            // 1. Рисуем левое поле (Min)
            minVal = EditorGUI.FloatField(minFieldRect, float.Parse(minVal.ToString("F2"))); // F2 для красивого форматирования
            
            // 2. Рисуем правое поле (Max)
            maxVal = EditorGUI.FloatField(maxFieldRect, float.Parse(maxVal.ToString("F2")));

            // Ограничиваем ввод с клавиатуры пределами атрибута
            minVal = Mathf.Clamp(minVal, rangeAttribute.minLimit, maxVal);
            maxVal = Mathf.Clamp(maxVal, minVal, rangeAttribute.maxLimit);

            // 3. Рисуем сам слайдер
            EditorGUI.MinMaxSlider(sliderRect, ref minVal, ref maxVal, rangeAttribute.minLimit, rangeAttribute.maxLimit);

            // Сохраняем значения
            property.vector2Value = new Vector2(minVal, maxVal);
        }
        // Логика для Vector2Int (Int)
        else if (property.propertyType == SerializedPropertyType.Vector2Int)
        {
            int minVal = property.vector2IntValue.x;
            int maxVal = property.vector2IntValue.y;
            float minFloat = minVal;
            float maxFloat = maxVal;

            // 1. Рисуем левое поле (Min)
            minVal = EditorGUI.IntField(minFieldRect, minVal);
            
            // 2. Рисуем правое поле (Max)
            maxVal = EditorGUI.IntField(maxFieldRect, maxVal);

            // Ограничиваем ввод
            if (minVal < rangeAttribute.minLimit) minVal = (int)rangeAttribute.minLimit;
            if (maxVal > rangeAttribute.maxLimit) maxVal = (int)rangeAttribute.maxLimit;
            if (minVal > maxVal) minVal = maxVal;

            // 3. Рисуем слайдер (он работает только с float, поэтому конвертируем туда-сюда)
            EditorGUI.MinMaxSlider(sliderRect, ref minFloat, ref maxFloat, rangeAttribute.minLimit, rangeAttribute.maxLimit);

            // Если слайдер сдвинули, обновляем int значения
            if (Mathf.Abs(minFloat - minVal) > 0.01f) minVal = Mathf.RoundToInt(minFloat);
            if (Mathf.Abs(maxFloat - maxVal) > 0.01f) maxVal = Mathf.RoundToInt(maxFloat);

            property.vector2IntValue = new Vector2Int(minVal, maxVal);
        }

        // Возвращаем отступ как было
        EditorGUI.indentLevel = originalIndent;
        EditorGUI.EndProperty();
    }
}