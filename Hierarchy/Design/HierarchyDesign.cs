using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HierarchyDesign : MonoBehaviour
{
    [Header("Настройки заголовка")]
    public string customName;
    public Color textColor = Color.white;
    public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color lightStripeTop = new Color(1f, 1f, 1f, 0.3f);
    public float lightStripeSize = 1.5f;
    
    [Header("Стиль")]
    public bool isHeader = true;
    public bool useUppercase = true;
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Используем delayCall, чтобы не ломать поток Unity при создании объекта
        EditorApplication.delayCall += EnsureSpacerExists;
    }

    private void EnsureSpacerExists()
    {
        if (this == null) return; // Проверка, что объект еще существует

        // Проверяем, является ли объект уже разделителем (чтобы не зациклиться)
        if (gameObject.name == "---") return;
        int currentIndex = transform.GetSiblingIndex();
        if (currentIndex <= 0) return; // Над нами никого нет

        GameObject previousSibling = null;

        if (transform.parent != null)
        {
            previousSibling = transform.parent.GetChild(currentIndex - 1).gameObject;
        }
        else
        {
            // Работаем с корнем сцены
            var rootObjects = gameObject.scene.GetRootGameObjects();
            previousSibling = rootObjects[currentIndex - 1];
        }
        
        if (previousSibling != null && previousSibling.GetComponent<HierarchyHeader>() == null)
        {
            GameObject spacer = new GameObject("---");
            spacer.AddComponent<HierarchyHeader>();
            
            // Устанавливаем того же родителя
            spacer.transform.SetParent(transform.parent);
            
            // Ставим его ровно над текущим объектом
            spacer.transform.SetSiblingIndex(currentIndex);

            // Скрываем разделитель в Инспекторе, чтобы он не мешал
           // spacer.hideFlags = HideFlags.HideInInspector;

            // Регистрируем создание для возможности отмены (Undo)
            Undo.RegisterCreatedObjectUndo(spacer, "Create Hierarchy Spacer");
        }
        
        // Отписываемся от события, чтобы не плодить вызовы
        EditorApplication.delayCall -= EnsureSpacerExists;
    }
#endif
}