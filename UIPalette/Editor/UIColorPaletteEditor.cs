using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Lib.UI
{
    /// <summary>
    /// Инспектор UIColorPalette. Сверху — поле базы цветов (UIColorBase) и её списки, встроенные для быстрой правки.
    /// Ниже — группы: каждая группа — блок с переключателем активности (активна ровно одна),
    /// именем и списком мест [имя][цвет базы ▾][оттенок]. Добавление, удаление и перестановка места
    /// в любой группе сразу делаются во всех группах; имя места общее.
    /// Кнопка «Свои цвета → в базу» складывает несвязанные оттенки в базу и связывает с ними места.
    /// </summary>
    [CustomEditor(typeof(UIColorPalette))]
    public class UIColorPaletteEditor : Editor
    {
        private const float Gap = 4f;
        private const string ImportListName = "Из палитры";
        private static readonly Color ActiveTint = new(0.55f, 1f, 0.6f);

        private SerializedProperty _base;
        private SerializedProperty _slots;
        private SerializedProperty _groups;
        private SerializedProperty _activeGroup;
        private SerializedProperty _nextId;
        private readonly List<ReorderableList> _lists = new();
        private Editor _baseEditor;

        private static GUIStyle _pickerStyle;

        /// <summary>Кнопка выбора цвета базы: выпадающий список с местом под образец слева.</summary>
        private static GUIStyle PickerStyle
        {
            get
            {
                if (_pickerStyle == null)
                {
                    _pickerStyle = new GUIStyle(EditorStyles.popup);
                    var p = _pickerStyle.padding;
                    _pickerStyle.padding = new RectOffset(p.left + 18, p.right, p.top, p.bottom);
                }
                return _pickerStyle;
            }
        }

        private void OnEnable()
        {
            _base = serializedObject.FindProperty("_base");
            _slots = serializedObject.FindProperty("_slots");
            _groups = serializedObject.FindProperty("_groups");
            _activeGroup = serializedObject.FindProperty("_activeGroup");
            _nextId = serializedObject.FindProperty("_nextId");
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            if (_baseEditor != null)
                DestroyImmediate(_baseEditor);
        }

        // Undo возвращает данные мимо инспектора — перекрашиваем UI явно.
        private void OnUndoRedo()
        {
            if (target is UIColorPalette palette)
                palette.NotifyChanged();
            InternalEditorUtility.RepaintAllViews();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (_lists.Count != _groups.arraySize)
                RebuildLists();

            DrawBase();
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Группы палитры", EditorStyles.boldLabel);

            int removeGroup = -1;
            for (int g = 0; g < _groups.arraySize; g++)
            {
                if (DrawGroup(g))
                    removeGroup = g;
                EditorGUILayout.Space(2f);
            }

            if (GUILayout.Button(new GUIContent("+ Группа", "Новая группа — копия оттенков и ссылок на базу активной.")))
                AddGroup();
            if (removeGroup >= 0)
                RemoveGroup(removeGroup);

            DrawImportButton();

            // OnValidate палитры перекрашивает UI; окна перерисовываем, чтобы в редакторе было видно сразу.
            if (serializedObject.ApplyModifiedProperties())
                InternalEditorUtility.RepaintAllViews();
        }

        private UIColorBase ColorBase => _base.objectReferenceValue as UIColorBase;

        /// <summary>Поле базы + её списки (встроенный инспектор UIColorBase) под сворачиваемой шапкой.</summary>
        private void DrawBase()
        {
            EditorGUILayout.PropertyField(_base, new GUIContent("База цветов", _base.tooltip));
            var colorBase = ColorBase;
            if (colorBase == null)
            {
                EditorGUILayout.HelpBox(
                    "Без базы у каждого места свой цвет. Назначьте UIColorBase — или нажмите внизу " +
                    "«Свои цвета → в базу»: база создастся рядом с палитрой из текущих оттенков.",
                    MessageType.None);
                return;
            }

            const string foldKey = "UIColorPaletteEditor.BaseFolded";
            bool open = !SessionState.GetBool(foldKey, false);
            bool nowOpen = EditorGUILayout.Foldout(open, "Цвета базы — правка сразу во всех местах, которые их используют", true);
            if (nowOpen != open)
                SessionState.SetBool(foldKey, !nowOpen);
            if (!nowOpen)
                return;

            CreateCachedEditor(colorBase, typeof(UIColorBaseEditor), ref _baseEditor);
            _baseEditor.OnInspectorGUI();
        }

        /// <summary>Шапка группы [свернуть][активна][имя][удалить] + список мест. True — нажато «Удалить».</summary>
        private bool DrawGroup(int g)
        {
            var group = _groups.GetArrayElementAtIndex(g);
            bool active = _activeGroup.intValue == g;
            string foldKey = "UIColorPaletteEditor.Folded." + AssetDatabase.GetAssetPath(target) + "." + g;
            bool open = !SessionState.GetBool(foldKey, false);
            bool remove;

            var bg = GUI.backgroundColor;
            if (active)
                GUI.backgroundColor = ActiveTint;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = bg;

            EditorGUILayout.BeginHorizontal();
            var foldRect = GUILayoutUtility.GetRect(14f, EditorGUIUtility.singleLineHeight, GUILayout.Width(14f));
            bool nowOpen = EditorGUI.Foldout(foldRect, open, GUIContent.none, true);
            if (nowOpen != open)
                SessionState.SetBool(foldKey, !nowOpen);
            // Радио: выключить активную нельзя — включается другая.
            var toggle = new GUIContent("", "Активная группа: её оттенки получает UI. Активна ровно одна.");
            if (GUILayout.Toggle(active, toggle, EditorStyles.radioButton, GUILayout.Width(16f)) && !active)
                _activeGroup.intValue = g;
            EditorGUILayout.PropertyField(group.FindPropertyRelative(nameof(UIColorPalette.Group.Name)), GUIContent.none);
            using (new EditorGUI.DisabledScope(_groups.arraySize <= 1))
            {
                var removeContent = new GUIContent("Удалить", "Удалить группу. Последнюю удалить нельзя.");
                remove = GUILayout.Button(removeContent, EditorStyles.miniButton, GUILayout.Width(60f));
            }
            EditorGUILayout.EndHorizontal();

            if (nowOpen)
                _lists[g].DoLayoutList();
            EditorGUILayout.EndVertical();
            return remove;
        }

        /// <summary>Списки всех групп рисуют один и тот же _slots, у каждого — оттенки своей группы.</summary>
        private void RebuildLists()
        {
            _lists.Clear();
            for (int g = 0; g < _groups.arraySize; g++)
            {
                int group = g;
                var list = new ReorderableList(serializedObject, _slots, true, false, true, true)
                {
                    drawElementCallback = (rect, i, _, _) => DrawSlot(rect, group, i),
                    onAddCallback = AddSlot,
                    onRemoveCallback = RemoveSlot,
                    // _slots список переставил сам — оттенки и ссылки всех групп переставляем следом.
                    onReorderCallbackWithDetails = (_, from, to) => MoveColors(from, to),
                };
                _lists.Add(list);
            }
        }

        /// <summary>
        /// Без базы: [имя][оттенок]. С базой: [имя][▾ цвет базы][оттенок] — у связанного места оттенок
        /// только показывается (меняется в базе), у своего — правится здесь.
        /// </summary>
        private void DrawSlot(Rect rect, int g, int i)
        {
            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;
            var slot = _slots.GetArrayElementAtIndex(i);
            var colors = Colors(g);
            var ids = ColorIds(g);
            var colorBase = ColorBase;

            if (colorBase == null)
            {
                float width = Mathf.Min(140f, rect.width * 0.45f);
                var name = new Rect(rect.x, rect.y, rect.width - width - Gap, rect.height);
                EditorGUI.PropertyField(name, slot.FindPropertyRelative(nameof(UIColorPalette.Slot.Name)), GUIContent.none);
                if (i < colors.arraySize)
                    EditorGUI.PropertyField(new Rect(name.xMax + Gap, rect.y, width, rect.height), colors.GetArrayElementAtIndex(i), GUIContent.none);
                return;
            }

            float pickerWidth = Mathf.Min(150f, rect.width * 0.36f);
            float colorWidth = Mathf.Min(70f, rect.width * 0.16f);
            var nameRect = new Rect(rect.x, rect.y, rect.width - pickerWidth - colorWidth - Gap * 2f, rect.height);
            var pickerRect = new Rect(nameRect.xMax + Gap, rect.y, pickerWidth, rect.height);
            var colorRect = new Rect(pickerRect.xMax + Gap, rect.y, colorWidth, rect.height);
            EditorGUI.PropertyField(nameRect, slot.FindPropertyRelative(nameof(UIColorPalette.Slot.Name)), GUIContent.none);

            int id = i < ids.arraySize ? ids.GetArrayElementAtIndex(i).intValue : 0;
            bool linked = colorBase.TryGet(id, out var entry);
            var own = i < colors.arraySize ? colors.GetArrayElementAtIndex(i) : null;
            Color shown = linked ? entry.Color : own?.colorValue ?? Color.white;

            GUIContent label;
            if (linked)
                label = new GUIContent(string.IsNullOrEmpty(entry.Name) ? "#" + entry.Id : entry.Name, "Цвет из базы. Клик — выбрать другой.");
            else if (id != 0)
                label = new GUIContent($"нет в базе ({id})", "Цвет удалён из базы — место пока со своей копией. Клик — выбрать заново.");
            else
                label = new GUIContent("Свой", "Свой цвет места в этой группе. Клик — взять цвет из базы.");

            if (EditorGUI.DropdownButton(pickerRect, label, FocusType.Passive, PickerStyle))
            {
                int group = g;
                int index = i;
                PopupWindow.Show(pickerRect, new UIColorBasePicker(colorBase, linked ? id : 0, pickerRect.width,
                    picked => LinkSlot(group, index, picked)));
            }
            if (Event.current.type == EventType.Repaint)
            {
                var swatch = new Rect(pickerRect.x + 4f, pickerRect.y + 3f, 14f, pickerRect.height - 6f);
                UIColorBasePicker.DrawSwatch(swatch, shown);
            }

            if (linked)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUI.ColorField(colorRect, GUIContent.none, entry.Color, false, true, false);
            }
            else if (own != null)
            {
                EditorGUI.PropertyField(colorRect, own, GUIContent.none);
            }
        }

        /// <summary>
        /// Выбор из окна цвета базы: Id 0 — свой цвет (берёт оттенок, который место показывало, — UI не дёргается),
        /// иначе ссылка на цвет базы, а свой оттенок становится его копией (запас на случай удаления из базы).
        /// </summary>
        private void LinkSlot(int g, int i, int id)
        {
            if (this == null || target is not UIColorPalette palette)
                return;
            serializedObject.Update();
            if (g >= _groups.arraySize || i >= _slots.arraySize)
                return;

            var ids = ColorIds(g);
            var colors = Colors(g);
            Resize(ids, _slots.arraySize, p => p.intValue = 0);
            Resize(colors, _slots.arraySize, p => p.colorValue = Color.white);

            int slotId = _slots.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(UIColorPalette.Slot.Id)).intValue;
            var colorBase = ColorBase;
            if (id != 0 && colorBase != null && colorBase.TryGetColor(id, out var fromBase))
                colors.GetArrayElementAtIndex(i).colorValue = fromBase;
            else if (id == 0 && palette.TryGetColor(slotId, g, out var current))
                colors.GetArrayElementAtIndex(i).colorValue = current;
            ids.GetArrayElementAtIndex(i).intValue = id;

            serializedObject.ApplyModifiedProperties();
            InternalEditorUtility.RepaintAllViews();
        }

        private SerializedProperty Colors(int g)
        {
            return _groups.GetArrayElementAtIndex(g).FindPropertyRelative(nameof(UIColorPalette.Group.Colors));
        }

        private SerializedProperty ColorIds(int g)
        {
            return _groups.GetArrayElementAtIndex(g).FindPropertyRelative(nameof(UIColorPalette.Group.ColorIds));
        }

        /// <summary>Новые элементы массива при росте копируют последний — задаём им значение явно.</summary>
        private static void Resize(SerializedProperty array, int size, Action<SerializedProperty> init)
        {
            int old = array.arraySize;
            if (old == size)
                return;
            array.arraySize = size;
            for (int k = old; k < size; k++)
                init(array.GetArrayElementAtIndex(k));
        }

        /// <summary>Новое место в конец — во всех группах, белым, со своим цветом. Id — следующий свободный.</summary>
        private void AddSlot(ReorderableList list)
        {
            int id = Mathf.Max(1, _nextId.intValue);
            for (int i = 0; i < _slots.arraySize; i++)
                id = Mathf.Max(id, _slots.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(UIColorPalette.Slot.Id)).intValue + 1);
            _nextId.intValue = id + 1;

            int index = _slots.arraySize;
            _slots.arraySize = index + 1;
            var slot = _slots.GetArrayElementAtIndex(index);
            slot.FindPropertyRelative(nameof(UIColorPalette.Slot.Id)).intValue = id;
            slot.FindPropertyRelative(nameof(UIColorPalette.Slot.Name)).stringValue = "New Color";
            for (int g = 0; g < _groups.arraySize; g++)
            {
                var colors = Colors(g);
                var ids = ColorIds(g);
                Resize(colors, index, p => p.colorValue = Color.white);
                Resize(ids, index, p => p.intValue = 0);
                Resize(colors, index + 1, p => p.colorValue = Color.white);
                Resize(ids, index + 1, p => p.intValue = 0);
            }
            list.index = index;
        }

        private void RemoveSlot(ReorderableList list)
        {
            int index = list.index;
            if (index < 0 || index >= _slots.arraySize)
                return;
            _slots.DeleteArrayElementAtIndex(index);
            for (int g = 0; g < _groups.arraySize; g++)
            {
                var colors = Colors(g);
                if (index < colors.arraySize)
                    colors.DeleteArrayElementAtIndex(index);
                var ids = ColorIds(g);
                if (index < ids.arraySize)
                    ids.DeleteArrayElementAtIndex(index);
            }
            list.index = Mathf.Min(index, _slots.arraySize - 1);
        }

        private void MoveColors(int from, int to)
        {
            for (int g = 0; g < _groups.arraySize; g++)
            {
                var colors = Colors(g);
                if (from < colors.arraySize && to < colors.arraySize)
                    colors.MoveArrayElement(from, to);
                var ids = ColorIds(g);
                if (from < ids.arraySize && to < ids.arraySize)
                    ids.MoveArrayElement(from, to);
            }
        }

        /// <summary>Новая группа в конец: оттенки и ссылки на базу — копия активной.</summary>
        private void AddGroup()
        {
            int source = Mathf.Clamp(_activeGroup.intValue, 0, _groups.arraySize - 1);
            int index = _groups.arraySize;
            _groups.arraySize = index + 1;
            var group = _groups.GetArrayElementAtIndex(index);
            group.FindPropertyRelative(nameof(UIColorPalette.Group.Name)).stringValue = "Group " + (index + 1);

            var from = Colors(source);
            var to = Colors(index);
            to.arraySize = from.arraySize;
            for (int i = 0; i < from.arraySize; i++)
                to.GetArrayElementAtIndex(i).colorValue = from.GetArrayElementAtIndex(i).colorValue;

            var fromIds = ColorIds(source);
            var toIds = ColorIds(index);
            toIds.arraySize = fromIds.arraySize;
            for (int i = 0; i < fromIds.arraySize; i++)
                toIds.GetArrayElementAtIndex(i).intValue = fromIds.GetArrayElementAtIndex(i).intValue;
        }

        /// <summary>Удалённая активная группа передаёт активность следующей (или предыдущей, если была последней).</summary>
        private void RemoveGroup(int g)
        {
            if (_groups.arraySize <= 1)
                return;
            _groups.DeleteArrayElementAtIndex(g);
            int active = _activeGroup.intValue;
            if (active > g)
                active--;
            _activeGroup.intValue = Mathf.Clamp(active, 0, _groups.arraySize - 1);
        }

        /// <summary>Сколько мест во всех группах со своим цветом (без ссылки или со ссылкой на удалённый цвет базы).</summary>
        private int CountOwnColors()
        {
            var colorBase = ColorBase;
            int count = 0;
            for (int g = 0; g < _groups.arraySize; g++)
            {
                var ids = ColorIds(g);
                for (int i = 0; i < _slots.arraySize; i++)
                {
                    int id = i < ids.arraySize ? ids.GetArrayElementAtIndex(i).intValue : 0;
                    if (colorBase == null || !colorBase.TryGet(id, out _))
                        count++;
                }
            }
            return count;
        }

        private void DrawImportButton()
        {
            int own = CountOwnColors();
            if (own == 0)
                return;
            EditorGUILayout.Space(4f);
            var content = new GUIContent($"Свои цвета → в базу ({own})",
                "Все места со своим цветом получают ссылку на цвет базы. Одинаковые оттенки (до 1/255) — один цвет базы; " +
                "оттенок, который в базе уже есть, берётся оттуда. Новые цвета — в список «" + ImportListName + "», " +
                "имя — по первому месту. Нет базы — создаётся рядом с палитрой. Отменяется через Undo.");
            if (GUILayout.Button(content))
            {
                MoveOwnColorsToBase();
                // Изменения уже применены; создание ассета посреди отрисовки — кадр дорисовывать не нужно.
                GUIUtility.ExitGUI();
            }
        }

        /// <summary>Свои оттенки мест → цвета базы (с объединением одинаковых) и ссылки на них.</summary>
        private void MoveOwnColorsToBase()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Свои цвета палитры → в базу");
            int undoGroup = Undo.GetCurrentGroup();

            var colorBase = ColorBase;
            if (colorBase == null)
            {
                string palettePath = AssetDatabase.GetAssetPath(target);
                string dir = string.IsNullOrEmpty(palettePath) ? "Assets" : Path.GetDirectoryName(palettePath)?.Replace('\\', '/');
                colorBase = CreateInstance<UIColorBase>();
                AssetDatabase.CreateAsset(colorBase, AssetDatabase.GenerateUniqueAssetPath(dir + "/UIColorBase.asset"));
                _base.objectReferenceValue = colorBase;
            }

            var baseSo = new SerializedObject(colorBase);
            var lists = baseSo.FindProperty("_lists");
            var byColor = new Dictionary<int, int>();
            for (int l = 0; l < lists.arraySize; l++)
            {
                var entries = lists.GetArrayElementAtIndex(l).FindPropertyRelative(nameof(UIColorBase.ColorList.Colors));
                for (int e = 0; e < entries.arraySize; e++)
                {
                    var entry = entries.GetArrayElementAtIndex(e);
                    int key = ColorKey(entry.FindPropertyRelative(nameof(UIColorBase.Entry.Color)).colorValue);
                    byColor.TryAdd(key, entry.FindPropertyRelative(nameof(UIColorBase.Entry.Id)).intValue);
                }
            }

            SerializedProperty importList = null;
            int added = 0;
            int linkedCount = 0;
            for (int g = 0; g < _groups.arraySize; g++)
            {
                var ids = ColorIds(g);
                var colors = Colors(g);
                Resize(ids, _slots.arraySize, p => p.intValue = 0);
                Resize(colors, _slots.arraySize, p => p.colorValue = Color.white);
                for (int i = 0; i < _slots.arraySize; i++)
                {
                    var idProp = ids.GetArrayElementAtIndex(i);
                    if (colorBase.TryGet(idProp.intValue, out _))
                        continue;

                    var color = colors.GetArrayElementAtIndex(i).colorValue;
                    int key = ColorKey(color);
                    if (!byColor.TryGetValue(key, out int baseId))
                    {
                        importList ??= FindOrAddList(lists, ImportListName);
                        var entries = importList.FindPropertyRelative(nameof(UIColorBase.ColorList.Colors));
                        int at = entries.arraySize;
                        entries.arraySize = at + 1;
                        var entry = entries.GetArrayElementAtIndex(at);
                        baseId = UIColorBaseEditor.TakeFreeId(baseSo);
                        entry.FindPropertyRelative(nameof(UIColorBase.Entry.Id)).intValue = baseId;
                        entry.FindPropertyRelative(nameof(UIColorBase.Entry.Name)).stringValue =
                            _slots.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(UIColorPalette.Slot.Name)).stringValue;
                        entry.FindPropertyRelative(nameof(UIColorBase.Entry.Color)).colorValue = color;
                        byColor[key] = baseId;
                        added++;
                    }
                    idProp.intValue = baseId;
                    linkedCount++;
                }
            }

            baseSo.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"[UIColorPalette] «{target.name}»: в базу «{colorBase.name}» добавлено цветов — {added}, " +
                      $"связано мест — {linkedCount}.", colorBase);
            InternalEditorUtility.RepaintAllViews();
        }

        private static SerializedProperty FindOrAddList(SerializedProperty lists, string name)
        {
            for (int l = 0; l < lists.arraySize; l++)
            {
                var list = lists.GetArrayElementAtIndex(l);
                if (list.FindPropertyRelative(nameof(UIColorBase.ColorList.Name)).stringValue == name)
                    return list;
            }
            int index = lists.arraySize;
            lists.arraySize = index + 1;
            var added = lists.GetArrayElementAtIndex(index);
            added.FindPropertyRelative(nameof(UIColorBase.ColorList.Name)).stringValue = name;
            added.FindPropertyRelative(nameof(UIColorBase.ColorList.Colors)).arraySize = 0;
            return added;
        }

        /// <summary>Оттенок с точностью 8 бит на канал: 0.9843138 и 0.984314 — один цвет.</summary>
        private static int ColorKey(Color color)
        {
            Color32 c = color;
            return (c.r << 24) | (c.g << 16) | (c.b << 8) | c.a;
        }
    }

    /// <summary>
    /// int с [UIPaletteSlot]: образец оттенка активной группы + выбор места палитры по имени.
    /// Нет палитры — обычное int-поле. Id, которого нет в палитре, — пункт «нет в палитре».
    /// </summary>
    [CustomPropertyDrawer(typeof(UIPaletteSlotAttribute))]
    public class UIPaletteSlotDrawer : PropertyDrawer
    {
        private const float SwatchWidth = 18f;
        private const float Gap = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (UIPaletteSlotAttribute)attribute;
            var paletteProp = property.serializedObject.FindProperty(attr.PaletteField);
            var palette = paletteProp != null ? paletteProp.objectReferenceValue as UIColorPalette : null;
            if (palette == null || property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, label);

            // Пункты: 0 — «Нет», 1..N — места палитры, последний — потерянный Id (если есть).
            var slots = palette.Slots;
            int id = property.intValue;
            int slotIndex = palette.IndexOfSlot(id);
            bool missing = id != 0 && slotIndex < 0;
            var options = new GUIContent[slots.Count + 1 + (missing ? 1 : 0)];
            options[0] = new GUIContent("Нет");
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                options[i + 1] = new GUIContent(s != null && !string.IsNullOrEmpty(s.Name) ? s.Name : "#" + (s?.Id ?? 0));
            }
            int selected = id == 0 ? 0 : slotIndex + 1;
            if (missing)
            {
                selected = options.Length - 1;
                options[selected] = new GUIContent($"нет в палитре (Id {id})");
            }

            var swatch = new Rect(position.x, position.y + 2f, SwatchWidth, position.height - 4f);
            var popup = new Rect(swatch.xMax + Gap, position.y, position.width - SwatchWidth - Gap, position.height);
            if (palette.TryGetColor(id, out var shown))
            {
                shown.a = 1f;
                EditorGUI.DrawRect(swatch, shown);
            }
            else if (missing)
            {
                EditorGUI.DrawRect(swatch, Color.gray);
            }

            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int picked = EditorGUI.Popup(popup, selected, options);
            if (EditorGUI.EndChangeCheck())
            {
                if (picked == 0)
                    property.intValue = 0;
                else if (picked <= slots.Count && slots[picked - 1] != null)
                    property.intValue = slots[picked - 1].Id;
            }
            EditorGUI.showMixedValue = false;

            EditorGUI.EndProperty();
        }
    }
}
