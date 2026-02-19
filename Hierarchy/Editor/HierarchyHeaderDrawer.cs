using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class HierarchyCustomizer
{
    static HierarchyCustomizer()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        // 1. Обработка "Пустых строк" (Spacers)
        if (obj.name == "---")
        {
            // Закрашиваем пустую строку цветом фона иерархии, чтобы она казалась пустой
            Color bgColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.78f, 0.78f, 0.78f);
            Rect fullRect = new Rect(32, selectionRect.y, selectionRect.width + selectionRect.x, selectionRect.height);
            EditorGUI.DrawRect(fullRect, bgColor);
            return; 
        }
        
        HierarchyDesign design = obj.GetComponent<HierarchyDesign>();

        if (design != null)
        {
            string displayTitle = string.IsNullOrEmpty(design.customName) ? obj.name : design.customName;
            if (design.useUppercase) displayTitle = displayTitle.ToUpper();

            // 2. Отрисовка фоновой области (Highlight)
            // Расширяем Rect, чтобы он занимал всю ширину строки
            Rect fullRect = new Rect(32, selectionRect.y, selectionRect.width + selectionRect.x, selectionRect.height);
            EditorGUI.DrawRect(fullRect, design.backgroundColor);

            // 3. Создание эффекта "Всплывающего заголовка" (Линия сверху)
            if (design.isHeader)
            {
                Rect topBorder = new Rect(fullRect.x, fullRect.y, fullRect.width, design.lightStripeSize);
                EditorGUI.DrawRect(topBorder, design.lightStripeTop);
            }

            // 4. Стиль текста
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = design.textColor },
                fontSize = design.isHeader ? 15 : 11,
                alignment = TextAnchor.MiddleLeft
            };

            // Смещаем текст чуть правее, если это заголовок
            Rect textRect = new Rect(selectionRect.x + 5, selectionRect.y, selectionRect.width, selectionRect.height);
            EditorGUI.LabelField(textRect, displayTitle, style);
        }
    }
}