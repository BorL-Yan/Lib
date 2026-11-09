using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lib.UI
{
    /// <summary>
    /// Палитра цветов UI: именованные места (имя + постоянный Id) и группы с оттенком для каждого места.
    /// Места общие для всех групп: добавление, удаление, переименование и порядок — сразу во всех группах,
    /// у каждой группы свой оттенок. Активна ровно одна группа — её оттенки получают все UIPaletteTint.
    /// Оттенок места в группе — либо цвет из базы (UIColorBase, ссылка по Id цвета), либо свой цвет.
    /// Ссылка на базу: правка цвета в базе перекрашивает все места, которые его используют.
    /// Id места — постоянная ссылка из UIPaletteTint: переименование и перестановка ссылок не ломают.
    /// Правка ассета сразу перекрашивает UI (и в редакторе, и в Play Mode); правка в Play Mode
    /// в редакторе сохраняется в файл, как у любого ScriptableObject.
    /// </summary>
    [CreateAssetMenu(menuName = "UI/UI Color Palette", fileName = "UIColorPalette")]
    public class UIColorPalette : ScriptableObject
    {
        [Serializable]
        public class Slot
        {
            public int Id;
            public string Name = "";
        }

        [Serializable]
        public class Group
        {
            public string Name = "";

            [Tooltip("Свой оттенок каждого места палитры, по индексу места. У места со ссылкой на базу — " +
                     "копия цвета базы: запасной, если цвет из базы удалят.")]
            public List<Color> Colors = new();

            [Tooltip("Id цвета UIColorBase для каждого места, по индексу места. 0 — свой цвет из Colors.")]
            public List<int> ColorIds = new();
        }

        [Tooltip("База цветов: из неё места групп выбирают оттенок.")]
        [SerializeField] private UIColorBase _base;

        [Tooltip("Места палитры: имя + Id. Общие для всех групп.")]
        [SerializeField] private List<Slot> _slots = new();

        [Tooltip("Группы оттенков. Цвет в UI берётся из активной.")]
        [SerializeField] private List<Group> _groups = new();

        [SerializeField] private int _activeGroup;

        // Следующий свободный Id: удалённое место не отдаёт свой Id новому,
        // иначе старые ссылки в UIPaletteTint молча указали бы на другой цвет.
        [SerializeField] private int _nextId = 1;

        [NonSerialized] private UIColorBase _subscribedBase;

        /// <summary>Изменился оттенок, место, активная группа или цвет базы — UIPaletteTint перекрашиваются.</summary>
        public event Action Changed;

        public UIColorBase ColorBase => _base;

        public IReadOnlyList<Slot> Slots => _slots;

        public IReadOnlyList<Group> Groups => _groups;

        public int ActiveGroupIndex => _groups.Count > 0 ? Mathf.Clamp(_activeGroup, 0, _groups.Count - 1) : -1;

        public Group ActiveGroup => _groups.Count > 0 ? _groups[ActiveGroupIndex] : null;

        public int IndexOfSlot(int id)
        {
            if (id != 0)
            {
                for (int i = 0; i < _slots.Count; i++)
                {
                    if (_slots[i] != null && _slots[i].Id == id)
                        return i;
                }
            }
            return -1;
        }

        /// <summary>Оттенок места в активной группе. False — места нет в палитре.</summary>
        public bool TryGetColor(int slotId, out Color color) => TryGetColor(slotId, ActiveGroupIndex, out color);

        public bool TryGetColor(int slotId, int groupIndex, out Color color)
        {
            int i = IndexOfSlot(slotId);
            if (i >= 0 && groupIndex >= 0 && groupIndex < _groups.Count)
                return TryGetSlotColor(_groups[groupIndex], i, out color);
            color = Color.white;
            return false;
        }

        /// <summary>Id цвета базы у места (по индексу) в группе. 0 — свой цвет.</summary>
        public int GetBaseColorId(int slotIndex, int groupIndex)
        {
            if (groupIndex < 0 || groupIndex >= _groups.Count)
                return 0;
            var ids = _groups[groupIndex]?.ColorIds;
            return ids != null && slotIndex >= 0 && slotIndex < ids.Count ? ids[slotIndex] : 0;
        }

        /// <summary>Сделать группу активной и перекрасить UI. В Play Mode в редакторе сохраняется в ассет.</summary>
        public void SetActiveGroup(int index)
        {
            if (index < 0 || index >= _groups.Count || index == _activeGroup)
                return;
            _activeGroup = index;
            NotifyChanged();
        }

        public void NotifyChanged() => Changed?.Invoke();

        /// <summary>Цвет из базы, если место на неё ссылается и цвет в базе есть; иначе свой.</summary>
        private bool TryGetSlotColor(Group group, int slotIndex, out Color color)
        {
            var ids = group?.ColorIds;
            if (_base != null && ids != null && slotIndex < ids.Count && ids[slotIndex] != 0
                && _base.TryGetColor(ids[slotIndex], out color))
                return true;

            var colors = group?.Colors;
            if (colors != null && slotIndex < colors.Count)
            {
                color = colors[slotIndex];
                return true;
            }
            color = Color.white;
            return false;
        }

        private void OnEnable()
        {
            SubscribeBase();
        }

        private void OnDisable()
        {
            if (_subscribedBase != null)
                _subscribedBase.Changed -= NotifyChanged;
            _subscribedBase = null;
        }

        /// <summary>Правка базы (в её инспекторе) перекрашивает UI через событие этой палитры.</summary>
        private void SubscribeBase()
        {
            if (_subscribedBase == _base)
                return;
            if (_subscribedBase != null)
                _subscribedBase.Changed -= NotifyChanged;
            _subscribedBase = _base;
            if (_subscribedBase != null)
                _subscribedBase.Changed += NotifyChanged;
        }

        /// <summary>Новый ассет: одна группа без мест.</summary>
        private void Reset()
        {
            _slots = new List<Slot>();
            _groups = new List<Group> { new() { Name = "Default" } };
            _activeGroup = 0;
            _nextId = 1;
        }

        private void OnValidate()
        {
            Sanitize();
            SubscribeBase();
            NotifyChanged();
        }

        /// <summary>
        /// Хотя бы одна группа, в каждой группе ровно по оттенку и Id цвета базы на место,
        /// Id мест уникальны и не 0. Инспектор держит это сам; здесь — страховка от ручной правки и старых данных.
        /// Свой цвет места со ссылкой на базу обновляется копией цвета базы — запас на случай удаления цвета из базы.
        /// </summary>
        private void Sanitize()
        {
            _slots ??= new List<Slot>();
            _groups ??= new List<Group>();
            if (_groups.Count == 0)
                _groups.Add(new Group { Name = "Default" });
            _activeGroup = Mathf.Clamp(_activeGroup, 0, _groups.Count - 1);

            foreach (var s in _slots)
            {
                if (s != null)
                    _nextId = Mathf.Max(_nextId, s.Id + 1);
            }
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i] ??= new Slot();
                var s = _slots[i];
                bool taken = s.Id <= 0;
                for (int j = 0; j < i && !taken; j++)
                    taken = _slots[j].Id == s.Id;
                if (taken)
                    s.Id = _nextId++;
            }

            for (int g = 0; g < _groups.Count; g++)
            {
                _groups[g] ??= new Group();
                var colors = _groups[g].Colors ??= new List<Color>();
                if (colors.Count > _slots.Count)
                    colors.RemoveRange(_slots.Count, colors.Count - _slots.Count);
                while (colors.Count < _slots.Count)
                    colors.Add(Color.white);

                var ids = _groups[g].ColorIds ??= new List<int>();
                if (ids.Count > _slots.Count)
                    ids.RemoveRange(_slots.Count, ids.Count - _slots.Count);
                while (ids.Count < _slots.Count)
                    ids.Add(0);

                if (_base == null)
                    continue;
                for (int i = 0; i < ids.Count; i++)
                {
                    if (ids[i] != 0 && _base.TryGetColor(ids[i], out var linked))
                        colors[i] = linked;
                }
            }
        }
    }

    /// <summary>
    /// int-поле = Id места UIColorPalette. В инспекторе — выбор по имени с образцом оттенка активной группы.
    /// PaletteField — имя поля UIColorPalette в том же объекте.
    /// </summary>
    public class UIPaletteSlotAttribute : PropertyAttribute
    {
        public readonly string PaletteField;

        public UIPaletteSlotAttribute(string paletteField)
        {
            PaletteField = paletteField;
        }
    }
}
