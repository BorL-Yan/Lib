using System;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;


namespace Lib.UI
{
    [RequireComponent(typeof(ButtonVisibility))]
    public abstract class UIButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region Data
        
        public event Action OnClick;
        
        private ButtonVisibility _visibility;
        
        private Sequence _anim;
        private bool _clicked = false;
        protected virtual float GetClickDuration => 0.125f;
        #endregion
        
        private void Awake()
        {
            _visibility = GetComponent<ButtonVisibility>();
        }        
        
        private void OnEnable()
        {
            State(_visibility.defaultPanel);
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            State(_visibility.clickedPanel);
            
            if (_clickSound != null)
            {
                Sound.InterfaceSound.Instance.PlayOneShot(_clickSound);
            }
            _anim?.Kill(false);
            _anim = DOTween.Sequence();
            _clicked = true;
            _anim.Append(transform.DOScale(_visibility.clickedScale, GetClickDuration))
                .AppendCallback(() =>
                {
                    OnClick?.Invoke();
                    Click();
                    _clicked = false;
                    State(_visibility.defaultPanel);
                })
                .Append(transform.DOScale(_visibility.selectedScale, 0.125f))
                .SetUpdate(true);
        }

        

        protected abstract void Click();
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if(_clicked) return;
            _anim?.Kill();
            _anim = DOTween.Sequence();
            _anim.Append(transform.DOScale(_visibility.selectedScale, 0.125f).SetUpdate(true));
            State(_visibility.selectedPanel);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if(_clicked) return;
            _anim?.Kill();
            _anim =  DOTween.Sequence();
            _anim.Append(transform.DOScale(_visibility.defoulteScale, 0.125f).SetUpdate(true));
            State(_visibility.defaultPanel);
        }

        private void State(GameObject obj)
        {
            _visibility.State(obj);
        }

        private void OnDestroy()
        {
            _anim.Kill();
        }
    }
}