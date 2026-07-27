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
        public event Action OnClick;
        private Sequence anim;

        public void OnPointerClick(PointerEventData pointerEventData)
        {
            Debug.Log("CLick");
            ButtonAnimation();
        }

        protected virtual void ButtonAnimation()
        {
            anim?.Kill();
            anim = DOTween.Sequence();
            anim.Append(transform.DOScale(Vector3.one * _clickScale, _clickDuration))
                .AppendCallback(() =>
                {
                    Click();
                    OnClick?.Invoke();
                })
                .Append(transform.DOScale(Vector3.one, _clickDuration));
        }

        private void OnDestroy()
        {
            anim?.Kill();
        }

        protected abstract void Click();
    }
}