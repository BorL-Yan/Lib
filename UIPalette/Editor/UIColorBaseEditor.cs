using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Lib.UI
{
    /// <summary>
    /// Инспектор UIColorBase: каждый список — блок [свернуть][имя][удалить] со строками [имя][цвет].
    /// Правый клик по строке — перенос цвета в другой список (Id сохраняется, ссылки палитр не ломаются).
    /// Рисуется и сам по себе, и встроенным в инспектор UIColorPalette.
    /// </summary>
    [CustomEditor(typeof(UIColorBase))]
    public class UIColorBaseEditor : Editor
    {
        private const float Gap = 4f;

        private SerializedProperty _lists;
        private SerializedProperty _nextId;
        private readonly List<ReorderableList> _reorderables = new();

        private void OnEnable()
        {
            _lists = serializedObject.FindProperty("_lists");
            _nextId = serializedObject.FindProperty("_nextId");
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        // Undo возвращает данные мимо инспектора — перекрашиваем UI явно.
        private void OnUndoRedo()
        {
            if (target is UIColorBase colorBase)
                colorBase.NotifyChanged();
            InternalEditorUtility.RepaintAllViews();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (_reorderables.Count != _lists.arraySize)
                RebuildLists();

            int removeList = -1;
            for (int l = 0; l < _lists.arraySize; l++)
            {
                if (DrawList(l))
                    removeList = l;
                EditorGUILayout.Space(2f);
            }

            if (GUILayout.Button(new GUIContent("+ Список", "Новый пустой список цветов.")))
                AddList();
            if (removeList >= 0)
            {
                _lists.DeleteArrayElementAtIndex(removeList);
                RebuildLists();
            }

            // OnValidate базы шлёт Changed — палитры перекрашивают UI; окна перерисовываем, чтобы было видно сразу.
            if (serializedObject.ApplyModifiedProperties())
                InternalEditorUtility.RepaintAllViews();
        }

        /// <summary>Шапка списка [свернуть][имя][удалить] + строки цветов. True — нажато «Удалить».</summary>
        private bool DrawList(int l)
        {
            var list = _lists.GetArrayElementAtIndex(l);
            string foldKey = "UIColorBaseEditor.Folded." + AssetDatabase.GetAssetPath(target) + "." + l;
            bool open = !SessionState.GetBool(foldKey, false);
            bool remove;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            var foldRect = GUILayoutUtility.GetRect(14f, EditorGUIUtility.singleLineHeight, GUILayout.Width(14f));
            bool nowOpen = EditorGUI.Foldout(foldRect, open, GUIContent.none, true);
            if (nowOpen != open)
                SessionState.SetBool(foldKey, !nowOpen);
            EditorGUILayout.PropertyField(list.FindPropertyRelative(nameof(UIColorBase.ColorList.Name)), GUIContent.none);
            if (!nowOpen)
                DrawSwatchStrip(Colors(l));
            var removeContent = new GUIContent("Удалить",
                "Удалить список вместе с цветами. Места палитры, которые ссылались на них, " +
                "остаются со своей копией цвета.");
            remove = GUILayout.Button(removeContent, EditorStyles.miniButton, GUILayout.Width(60f));
            EditorGUILayout.EndHorizontal();

            if (nowOpen)
                _reorderables[l].DoLayoutList();
            EditorGUILayout.EndVertical();
            return remove;
        }

        /// <summary>Свёрнутый список — полоска образцов его цветов в шапке.</summary>
        private static void DrawSwatchStrip(SerializedProperty colors)
        {
            int count = Mathf.Min(colors.arraySize, 12);
            var rect = GUILayoutUtility.GetRect(count * 10f, EditorGUIUtility.singleLineHeight, GUILayout.Width(count * 10f));
            for (int i = 0; i < count; i++)
            {
                var c = colors.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(UIColorBase.Entry.Color)).colorValue;
                c.a = 1f;
                EditorGUI.DrawRect(new Rect(rect.x + i * 10f, rect.y + 3f, 9f, rect.height - 6f), c);
            }
        }

        private SerializedProperty Colors(int l)
        {
            return _lists.GetArrayElementAtIndex(l).FindPropertyRelative(nameof(UIColorBase.ColorList.Colors));
        }

        private void RebuildLists()
        {
            _reorderables.Clear();
            for (int l = 0; l < _lists.arraySize; l++)
            {
                int list = l;
                var reorderable = new ReorderableList(serializedObject, Colors(l), true, false, true, true)
                {
                    drawElementCallback = (rect, i, _, _) => DrawEntry(rect, list, i),
                    onAddCallback = r => AddEntry(r, list),
                };
                _reorderables.Add(reorderable);
            }
        }

        private void DrawEntry(Rect rect, int l, int i)
        {
            var colors = Colors(l);
            if (i >= colors.arraySize)
                return;
            var entry = colors.GetArrayElementAtIndex(i);

            var row = rect;
            var e = Event.current;
            if (e.type == EventType.ContextClick && row.Contains(e.mousePosition))
            {
                ShowMoveMenu(l, i);
                e.Use();
            }

            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;
            float colorWidth = Mathf.Min(140f, rect.width * 0.45f);
            var nameRect = new Rect(rect.x, rect.y, rect.width - colorWidth - Gap, rect.height);
            var colorRect = new Rect(nameRect.xMax + Gap, rect.y, colorWidth, rect.height);
            EditorGUI.PropertyField(nameRect, entry.FindPropertyRelative(nameof(UIColorBase.Entry.Name)), GUIContent.none);
            EditorGUI.PropertyField(colorRect, entry.FindPropertyRelative(nameof(UIColorBase.Entry.Color)), GUIContent.none);
        }

        /// <summary>Новый цвет в конец списка: свободный Id, оттенок — копия последнего цвета списка.</summary>
        private void AddEntry(ReorderableList reorderable, int l)
        {
            var colors = Colors(l);
            int index = colors.arraySize;
            var color = index > 0
                ? colors.GetArrayElementAtIndex(index - 1).FindPropertyRelative(nameof(UIColorBase.Entry.Color)).colorValue
                : Color.white;
            colors.arraySize = index + 1;
            var entry = colors.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative(nameof(UIColorBase.Entry.Id)).intValue = TakeFreeId(serializedObject);
            entry.FindPropertyRelative(nameof(UIColorBase.Entry.Name)).stringValue = "New Color";
            entry.FindPropertyRelative(nameof(UIColorBase.Entry.Color)).colorValue = color;
            reorderable.index = index;
        }

        private void AddList()
        {
            int index = _lists.arraySize;
            _lists.arraySize = index + 1;
            var list = _lists.GetArrayElementAtIndex(index);
            list.FindPropertyRelative(nameof(UIColorBase.ColorList.Name)).stringValue = "List " + (index + 1);
            list.FindPropertyRelative(nameof(UIColorBase.ColorList.Colors)).arraySize = 0;
            RebuildLists();
        }

        /// <summary>Правый клик по цвету: перенести в другой список. Id не меняется.</summary>
        private void ShowMoveMenu(int from, int index)
        {
            var menu = new GenericMenu();
            for (int l = 0; l < _lists.arraySize; l++)
            {
                string name = _lists.GetArrayElementAtIndex(l).FindPropertyRelative(nameof(UIColorBase.ColorList.Name)).stringValue;
                var content = new GUIContent("Перенести в список/" + (string.IsNullOrEmpty(name) ? "#" + (l + 1) : name));
                if (l == from)
                {
                    menu.AddDisabledItem(content, true);
                    continue;
                }
                int to = l;
                menu.AddItem(content, false, () => MoveEntry(from, index, to));
            }
            menu.ShowAsContext();
        }

        private void MoveEntry(int from, int index, int to)
        {
            serializedObject.Update();
            var src = Colors(from);
            if (index >= src.arraySize)
                return;
            var entry = src.GetArrayElementAtIndex(index);
            int id = entry.FindPropertyRelative(nameof(UIColorBase.Entry.Id)).intValue;
            string name = entry.FindPropertyRelative(nameof(UIColorBase.Entry.Name)).stringValue;
            var color = entry.FindPropertyRelative(nameof(UIColorBase.Entry.Color)).colorValue;

            var dst = Colors(to);
            int at = dst.arraySize;
            dst.arraySize = at + 1;
            var moved = dst.GetArrayElementAtIndex(at);
            moved.FindPropertyRelative(nameof(UIColorBase.Entry.Id)).intValue = id;
            moved.FindPropertyRelative(nameof(UIColorBase.Entry.Name)).stringValue = name;
            moved.FindPropertyRelative(nameof(UIColorBase.Entry.Color)).colorValue = color;
            src.DeleteArrayElementAtIndex(index);

            serializedObject.ApplyModifiedProperties();
            InternalEditorUtility.RepaintAllViews();
        }

        /// <summary>Следующий свободный Id базы (больше всех Id во всех списках) — и сдвиг _nextId.</summary>
        public static int TakeFreeId(SerializedObject colorBase)
        {
            var nextId = colorBase.FindProperty("_nextId");
            var lists = colorBase.FindProperty("_lists");
            int id = Mathf.Max(1, nextId.intValue);
            for (int l = 0; l < lists.arraySize; l++)
            {
                var colors = lists.GetArrayElementAtIndex(l).FindPropertyRelative(nameof(UIColorBase.ColorList.Colors));
                for (int i = 0; i < colors.arraySize; i++)
                    id = Mathf.Max(id, colors.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(UIColorBase.Entry.Id)).intValue + 1);
            }
            nextId.intValue = id + 1;
            return id;
        }
    }

    /// <summary>
    /// Выбор цвета базы для места палитры: «Свой цвет» + все списки базы блоками,
    /// в блоке — плашки [образец][имя] в две колонки. Клик выбирает и закрывает окно.
    /// </summary>
    public class UIColorBasePicker : PopupWindowContent
    {
        private const float Pad = 6f;
        private const float Gap = 4f;
        private const float ChipHeight = 20f;
        private const float HeaderHeight = 18f;
        private const float SwatchSize = 14f;
        private const float MaxHeight = 460f;
        private const int Columns = 2;

        private static readonly Color SelectedTint = new(0.24f, 0.49f, 0.91f, 0.45f);
        private static readonly Color HoverTint = new(1f, 1f, 1f, 0.1f);

        private readonly UIColorBase _base;
        private readonly int _selectedId;
        private readonly Action<int> _onPick;
        private readonly float _width;
        private Vector2 _scroll;

        public UIColorBasePicker(UIColorBase colorBase, int selectedId, float width, Action<int> onPick)
        {
            _base = colorBase;
            _selectedId = selectedId;
            _onPick = onPick;
            _width = Mathf.Max(width, 300f);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(_width, Mathf.Min(ContentHeight() + 2f, MaxHeight));
        }

        public override void OnOpen()
        {
            editorWindow.wantsMouseMove = true;
        }

        private float ContentHeight()
        {
            float h = Pad + ChipHeight + Pad;
            if (_base == null || _base.Lists.Count == 0)
                return h + HeaderHeight * 2f + Pad;
            foreach (var list in _base.Lists)
            {
                int count = list?.Colors?.Count ?? 0;
                h += HeaderHeight + Mathf.Max(1, Mathf.CeilToInt(count / (float)Columns)) * ChipHeight + Pad;
            }
            return h;
        }

        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.MouseMove)
                editorWindow.Repaint();

            float contentHeight = ContentHeight();
            bool scroll = contentHeight > rect.height;
            float width = rect.width - (scroll ? 14f : 0f);
            _scroll = GUI.BeginScrollView(rect, _scroll, new Rect(0f, 0f, width, contentHeight));

            float y = Pad;
            float innerWidth = width - Pad * 2f;
            var own = new Rect(Pad, y, innerWidth, ChipHeight);
            if (Chip(own, null, "Свой цвет — задаётся в палитре", _selectedId == 0))
                Pick(0);
            y += ChipHeight + Pad;

            if (_base == null || _base.Lists.Count == 0)
            {
                var help = new Rect(Pad, y, innerWidth, HeaderHeight * 2f);
                EditorGUI.LabelField(help, "База пуста: добавьте списки и цвета в ассет базы.", EditorStyles.wordWrappedMiniLabel);
            }
            else
            {
                float chipWidth = (innerWidth - Gap * (Columns - 1)) / Columns;
                for (int l = 0; l < _base.Lists.Count; l++)
                {
                    var list = _base.Lists[l];
                    string title = list != null && !string.IsNullOrEmpty(list.Name) ? list.Name : "Список " + (l + 1);
                    EditorGUI.LabelField(new Rect(Pad, y, innerWidth, HeaderHeight), title, EditorStyles.boldLabel);
                    y += HeaderHeight;

                    int count = list?.Colors?.Count ?? 0;
                    if (count == 0)
                    {
                        EditorGUI.LabelField(new Rect(Pad, y, innerWidth, ChipHeight), "пусто", EditorStyles.miniLabel);
                        y += ChipHeight + Pad;
                        continue;
                    }
                    for (int i = 0; i < count; i++)
                    {
                        var e = list.Colors[i];
                        if (e == null)
                            continue;
                        int col = i % Columns;
                        int row = i / Columns;
                        var chip = new Rect(Pad + col * (chipWidth + Gap), y + row * ChipHeight, chipWidth, ChipHeight);
                        string name = string.IsNullOrEmpty(e.Name) ? "#" + e.Id : e.Name;
                        if (Chip(chip, e.Color, name, e.Id == _selectedId))
                            Pick(e.Id);
                    }
                    y += Mathf.CeilToInt(count / (float)Columns) * ChipHeight + Pad;
                }
            }

            GUI.EndScrollView();
        }

        /// <summary>Плашка [образец][имя]; без цвета — только текст. True — клик.</summary>
        private static bool Chip(Rect rect, Color? color, string label, bool selected)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            if (Event.current.type == EventType.Repaint)
            {
                if (selected)
                    EditorGUI.DrawRect(rect, SelectedTint);
                else if (hover)
                    EditorGUI.DrawRect(rect, HoverTint);
            }

            var labelRect = new Rect(rect.x + 4f, rect.y, rect.width - 4f, rect.height);
            if (color.HasValue)
            {
                var swatch = new Rect(rect.x + 3f, rect.y + (rect.height - SwatchSize) * 0.5f, SwatchSize, SwatchSize);
                DrawSwatch(swatch, color.Value);
                labelRect.xMin = swatch.xMax + 5f;
            }
            EditorGUI.LabelField(labelRect, new GUIContent(label, label));
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        /// <summary>Образец с рамкой; прозрачность — полоской снизу (белая часть = альфа).</summary>
        public static void DrawSwatch(Rect rect, Color color)
        {
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.6f));
            var inner = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
            var opaque = color;
            opaque.a = 1f;
            EditorGUI.DrawRect(inner, opaque);
            if (color.a < 0.999f)
            {
                var bar = new Rect(inner.x, inner.yMax - 2f, inner.width, 2f);
                EditorGUI.DrawRect(bar, Color.black);
                EditorGUI.DrawRect(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(color.a), bar.height), Color.white);
            }
        }

        private void Pick(int id)
        {
            _onPick?.Invoke(id);
            editorWindow.Close();
        }
    }
}
