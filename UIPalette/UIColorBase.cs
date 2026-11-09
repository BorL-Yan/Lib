using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lib.UI
{
    /// <summary>
    /// База цветов UI: конкретные цвета (имя + оттенок), разложенные по спискам («Тёмные», «Акценты»…).
    /// UIColorPalette берёт оттенки отсюда: место группы ссылается на цвет базы по Id,
    /// и правка цвета в базе сразу перекрашивает все места, которые его используют.
    /// Id цвета уникален во всей базе (не только в своём списке) и не переиспользуется после удаления:
    /// перенос цвета в другой список и переименование ссылок не ломают.
    /// </summary>
    [CreateAssetMenu(menuName = "UI/UI Color Base", fileName = "UIColorBase")]
    public class UIColorBase : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [HideInInspector] public int Id;
            public string Name = "";
            public Color Color = Color.white;
        }

        [Serializable]
        public class ColorList
        {
            public string Name = "";
            public List<Entry> Colors = new();
        }

        [Tooltip("Списки цветов. В выборе цвета палитры каждый список — отдельный блок.")]
        [SerializeField] private List<ColorList> _lists = new();

        // Следующий свободный Id: удалённый цвет не отдаёт свой Id новому,
        // иначе старые ссылки в UIColorPalette молча указали бы на другой цвет.
        [SerializeField, HideInInspector] private int _nextId = 1;

        /// <summary>Изменился цвет или состав базы — палитры, которые её используют, перекрашивают UI.</summary>
        public event Action Changed;

        public IReadOnlyList<ColorList> Lists => _lists;

        public bool TryGet(int id, out Entry entry)
        {
            if (id > 0)
            {
                foreach (var list in _lists)
                {
                    if (list?.Colors == null)
                        continue;
                    foreach (var e in list.Colors)
                    {
                        if (e != null && e.Id == id)
                        {
                            entry = e;
                            return true;
                        }
                    }
                }
            }
            entry = null;
            return false;
        }

        public bool TryGetColor(int id, out Color color)
        {
            if (TryGet(id, out var e))
            {
                color = e.Color;
                return true;
            }
            color = Color.white;
            return false;
        }

        public void NotifyChanged() => Changed?.Invoke();

        /// <summary>Новый ассет: три списка базовых цветов.</summary>
        private void Reset()
        {
            _lists = new List<ColorList>
            {
                new()
                {
                    Name = "Нейтральные",
                    Colors = new List<Entry>
                    {
                        new() { Id = 1, Name = "White", Color = Color.white },
                        new() { Id = 2, Name = "Light Gray", Color = new Color(0.8f, 0.8f, 0.8f) },
                        new() { Id = 3, Name = "Gray", Color = new Color(0.5f, 0.5f, 0.5f) },
                        new() { Id = 4, Name = "Dark", Color = new Color(0.15f, 0.15f, 0.15f) },
                    },
                },
                new()
                {
                    Name = "Тёплые",
                    Colors = new List<Entry>
                    {
                        new() { Id = 5, Name = "Red", Color = new Color(1f, 0.3f, 0.3f) },
                        new() { Id = 6, Name = "Orange", Color = new Color(1f, 0.6f, 0.2f) },
                        new() { Id = 7, Name = "Yellow", Color = new Color(1f, 0.9f, 0.2f) },
                    },
                },
                new()
                {
                    Name = "Холодные",
                    Colors = new List<Entry>
                    {
                        new() { Id = 8, Name = "Green", Color = new Color(0.3f, 0.85f, 0.4f) },
                        new() { Id = 9, Name = "Teal", Color = new Color(0.2f, 0.7f, 0.7f) },
                        new() { Id = 10, Name = "Blue", Color = new Color(0.3f, 0.55f, 1f) },
                    },
                },
            };
            _nextId = 11;
        }

        private void OnValidate()
        {
            Sanitize();
            NotifyChanged();
        }

        /// <summary>
        /// Новая запись («+» стандартного списка копирует последнюю вместе с Id) или Id 0 — получает свободный Id.
        /// Проверка по всей базе: Id уникален между списками.
        /// </summary>
        private void Sanitize()
        {
            _lists ??= new List<ColorList>();
            foreach (var list in _lists)
            {
                if (list == null)
                    continue;
                list.Colors ??= new List<Entry>();
                foreach (var e in list.Colors)
                {
                    if (e != null)
                        _nextId = Mathf.Max(_nextId, e.Id + 1);
                }
            }

            var used = new HashSet<int>();
            for (int l = 0; l < _lists.Count; l++)
            {
                _lists[l] ??= new ColorList();
                var colors = _lists[l].Colors;
                for (int i = 0; i < colors.Count; i++)
                {
                    colors[i] ??= new Entry();
                    var e = colors[i];
                    if (e.Id <= 0 || !used.Add(e.Id))
                    {
                        e.Id = _nextId++;
                        used.Add(e.Id);
                    }
                }
            }
        }
    }
}
