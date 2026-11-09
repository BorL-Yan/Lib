using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Lib.UI
{
    public abstract class UIMobileButton : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] protected float _clickScale = 1.2f;
        [SerializeField] protected float _clickDuration = 0.125f;
        [Tooltip("Анимация клика идёт в реальном времени — кнопка работает и на паузе (Time.timeScale = 0).")]
        [SerializeField] protected bool _ignoreTimeScale = true;
        [Tooltip("Выключенная кнопка не реагирует на нажатия (как Button.interactable).")]
        [SerializeField] protected bool _interactable = true;
        public event Action OnClick;
        private Sequence anim;
        private Vector3 _baseScale = Vector3.one;

        /// <summary>false — нажатия игнорируются: ни анимации, ни OnClick.</summary>
        public bool Interactable
        {
            get => _interactable;
            set => _interactable = value;
        }

        public void OnPointerClick(PointerEventData pointerEventData)
        {
            if (!_interactable)
                return;
            Debug.Log("CLick");
            ButtonAnimation();
        }

        protected virtual void ButtonAnimation()
        {
            // Масштаб из инспектора, а не Vector3.one: берём его, только когда анимация не идёт.
            if (anim == null || !anim.IsActive())
                _baseScale = transform.localScale;
            anim?.Kill();
            anim = DOTween.Sequence();
            anim.Append(transform.DOScale(_baseScale * _clickScale, _clickDuration))
                .AppendCallback(() =>
                {
                    Click();
                    OnClick?.Invoke();
                })
                .Append(transform.DOScale(_baseScale, _clickDuration))
                .SetUpdate(_ignoreTimeScale);
        }

        private void OnDestroy()
        {
            anim?.Kill();
        }

        protected abstract void Click();
    }
}