using UnityEngine;
using UnityEngine.UI;

namespace Lib.UI
{
    /// <summary>
    /// Красит Graphic этого объекта (Image, RawImage, TMP-текст) оттенком места UIColorPalette
    /// из активной группы палитры. Перекрашивает сразу при правке палитры или смене активной группы —
    /// и в редакторе, и в Play Mode. Цвет Graphic, заданный вручную, перетирается.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public class UIPaletteTint : MonoBehaviour
    {
        [Tooltip("Палитра UI (ассет UIColorPalette).")]
        [SerializeField] private UIColorPalette _palette;

        [Tooltip("Место палитры по имени; оттенок — из активной группы.")]
        [SerializeField, UIPaletteSlot(nameof(_palette))] private int _slotId;

        [System.NonSerialized] private Graphic _graphic;
        [System.NonSerialized] private UIColorPalette _subscribed;

        public UIColorPalette Palette => _palette;

        public int SlotId => _slotId;

        private void OnEnable()
        {
            Subscribe();
            Apply();
        }

        private void OnDisable()
        {
            if (_subscribed != null)
                _subscribed.Changed -= Apply;
            _subscribed = null;
        }

        // Смена палитры или места в инспекторе. Выключенный компонент не подписывается: OnDisable не отпишет.
        private void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;
            Subscribe();
            Apply();
        }

        public void Apply()
        {
            if (_palette == null || !_palette.TryGetColor(_slotId, out var color))
                return;
            if (_graphic == null)
                _graphic = GetComponent<Graphic>();
            if (_graphic != null && _graphic.color != color)
                _graphic.color = color;
        }

        private void Subscribe()
        {
            if (_subscribed == _palette)
                return;
            if (_subscribed != null)
                _subscribed.Changed -= Apply;
            _subscribed = _palette;
            if (_subscribed != null)
                _subscribed.Changed += Apply;
        }
    }
}
