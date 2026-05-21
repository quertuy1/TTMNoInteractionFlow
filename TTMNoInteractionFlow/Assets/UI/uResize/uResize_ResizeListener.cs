using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace DigitalLegacy.UI.Sizing
{
    /// <summary>
    /// Used by uResize to intercept drag events
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public class uResize_ResizeListener : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Action OnBeginDragEvent;
        public Action<PointerEventData> OnDragEvent;
        public Action OnEndDragEvent;

        public Action OnPointerEnterEvent;
        public Action OnPointerExitEvent;       

        [SerializeField]
        private Image m_ImageComponent;
        public Image ImageComponent
        {
            get
            {
                if (m_ImageComponent == null) m_ImageComponent = this.GetComponent<Image>();
                return m_ImageComponent;
            }
        }     

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (OnBeginDragEvent != null) OnBeginDragEvent();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (OnDragEvent != null) OnDragEvent(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (OnEndDragEvent != null) OnEndDragEvent();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (OnPointerEnterEvent != null) OnPointerEnterEvent();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (OnPointerExitEvent != null) OnPointerExitEvent();
        }
    }
}
