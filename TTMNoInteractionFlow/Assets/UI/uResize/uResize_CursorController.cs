using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;

namespace DigitalLegacy.UI.Sizing
{
    /// <summary>
    /// Add this component to an object with uResize enabled to control cursor events
    /// </summary>
    [RequireComponent(typeof(uResize)), AddComponentMenu("UI/uResize Cursor Controller")]
    public class uResize_CursorController : MonoBehaviour
    {
        [Header("Initialization")]
        public bool SetCursorOnStart = false;

        [Header("Cursor Types")]
        public Texture2D RegularCursor;
        public Texture2D HorizontalCursor;
        public Texture2D VerticalCursor;

        public Texture2D TopLeftCursor;
        public Texture2D TopRightCursor;
        public Texture2D BottomLeftCursor;
        public Texture2D BottomRightCursor;                

        [Header("Modes & Hotspots")]
        public CursorMode CursorMode = CursorMode.Auto;
        public Vector2 RegularCursorHotspot = Vector2.zero;
        public Vector2 ResizeCursorHotspot = new Vector2(16, 16);

        private bool m_resizeInProgress = false;
        private eResizeListenerType m_resizeType;
        private eResizeListenerType? m_pointerOverResizeType;

        public Action OnReturnToRegularCursor;

        private void Start()
        {
            var uResize = this.GetComponent<uResize>();

            uResize.OnPointerEnterResizeListener.AddListener(OnPointerEnterListener);
            uResize.OnPointerExitResizeListener.AddListener(OnPointerExitListener);

            uResize.OnResizeBegin.AddListener(OnResizeBegin);
            uResize.OnResizeEnd.AddListener(OnResizeEnd);

            if (SetCursorOnStart)
            {
                SetCursor(RegularCursor, true);
            }
        }

        private void OnResizeBegin(eResizeListenerType type)
        {
            m_resizeInProgress = true;
            m_resizeType = type;
        }

        private void OnResizeEnd()
        {
            m_resizeInProgress = false;

            if (m_pointerOverResizeType.HasValue)
            {
                SetCursor(GetCursorForType(m_pointerOverResizeType.Value));
            }
            else
            {
                SetCursor(RegularCursor, true);
            }
        }

        private void OnPointerEnterListener(eResizeListenerType type)
        {
            m_pointerOverResizeType = type;

            // Don't change cursors while we are resizing
            if (m_resizeInProgress) return;

            SetCursor(GetCursorForType(type));
        }

        private Texture2D GetCursorForType(eResizeListenerType type)
        {
            var cursor = RegularCursor;

            switch(type)
            {
                case eResizeListenerType.Bottom:
                case eResizeListenerType.Top:
                    cursor = VerticalCursor;
                    break;
                case eResizeListenerType.Left:
                case eResizeListenerType.Right:
                    cursor = HorizontalCursor;
                    break;
                case eResizeListenerType.TopLeft:
                    cursor = TopLeftCursor;
                    break;
                case eResizeListenerType.TopRight:
                    cursor = TopRightCursor;
                    break;
                case eResizeListenerType.BottomLeft:
                    cursor = BottomLeftCursor;
                    break;
                case eResizeListenerType.BottomRight:
                    cursor = BottomRightCursor;
                    break;
            }

            return cursor;
        }

        private void OnPointerExitListener(eResizeListenerType type)
        {
            m_pointerOverResizeType = null;
                        
            if (!m_resizeInProgress)
            {
                SetCursor(RegularCursor, true);
            }
        }        

        private void SetCursor(Texture2D cursor, bool regular = false)
        {
            if (enabled)
            {
                Cursor.SetCursor(cursor, regular ? RegularCursorHotspot : ResizeCursorHotspot, CursorMode);

                // This event will allow users to take control of the cursor again when
                // the controller is done with it, if necessary
                if (regular && OnReturnToRegularCursor != null) OnReturnToRegularCursor();
            }
        }

        private void Update()
        {
            if (m_resizeInProgress)
            {                
                // It's possible for OnPointerExit to trigger before the resize event begins,
                // as such, it is necessary to set the cursor here while a resize event is in progress            
                SetCursor(GetCursorForType(m_resizeType));
            }
        }
    }
}
