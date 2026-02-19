using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[InitializeOnLoad]
public class HierarchyAutomator
{
    private static bool isProcessing = false;

    static HierarchyAutomator()
    {
        // Это событие срабатывает при ЛЮБОМ изменении в иерархии
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private static void OnHierarchyChanged()
    {
        // Защита от бесконечного цикла (так как создание объекта тоже вызывает hierarchyChanged)
        if (isProcessing) return;

        isProcessing = true;
        // Задержка, чтобы Unity успела завершить внутренние процессы перемещения
        EditorApplication.delayCall += ProcessHierarchy;
    }

    private static void ProcessHierarchy()
    {
        // 1. Сначала чистим все старые или лишние разделители
        CleanupOrphanedSpacers();

        // 2. Создаем новые разделители там, где их не хватает
        CreateMissingSpacers();

        isProcessing = false;
    }

    private static void CleanupOrphanedSpacers()
    {
        // Находим все объекты с именем "---"
        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj.name == "---")
            {
                // Проверяем, есть ли под ним HierarchyDesign
                if (!IsDesignObjectBelow(obj))
                {
                    Undo.DestroyObjectImmediate(obj);
                }
            }
        }
    }

    private static void CreateMissingSpacers()
    {
        HierarchyDesign[] designs = GameObject.FindObjectsByType<HierarchyDesign>(FindObjectsSortMode.None);
        foreach (var design in designs)
        {
            if (!HasSpacerAbove(design.gameObject))
            {
                CreateSpacerAbove(design.gameObject);
            }
        }
    }

    // Вспомогательные методы
    private static bool IsDesignObjectBelow(GameObject spacer)
    {
        int index = spacer.transform.GetSiblingIndex();
        Transform parent = spacer.transform.parent;
        int count = parent != null ? parent.childCount : spacer.scene.rootCount;

        if (index + 1 < count)
        {
            GameObject next = parent != null ? parent.GetChild(index + 1).gameObject : spacer.scene.GetRootGameObjects()[index + 1];
            return next.GetComponent<HierarchyDesign>() != null;
        }
        return false;
    }

    private static bool HasSpacerAbove(GameObject obj)
    {
        int index = obj.transform.GetSiblingIndex();
        if (index == 0) return false;

        Transform parent = obj.transform.parent;
        GameObject prev = parent != null ? parent.GetChild(index - 1).gameObject : obj.scene.GetRootGameObjects()[index - 1];
        return prev.name == "---";
    }

    private static void CreateSpacerAbove(GameObject target)
    {
        GameObject spacer = new GameObject("---");
        spacer.transform.SetParent(target.transform.parent);
        spacer.transform.SetSiblingIndex(target.transform.GetSiblingIndex());
        spacer.hideFlags = HideFlags.HideInInspector; // Скрываем из инспектора
        Undo.RegisterCreatedObjectUndo(spacer, "Auto-Create Spacer");
    }
}