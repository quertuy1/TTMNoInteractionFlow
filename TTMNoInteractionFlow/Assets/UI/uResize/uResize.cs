using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

namespace DigitalLegacy.UI.Sizing
{
    /// <summary>
    /// uResize is a script which allows you to resize any UI element with a RectTransform
    /// by dragging the edges.
    /// uResize works in all Canvas Render modes, on any RectTransform using any anchor and pivot settings.
    /// </summary>
    [RequireComponent(typeof(RectTransform)), DisallowMultipleComponent, AddComponentMenu("UI/uResize")]
    public class uResize : MonoBehaviour
    {
        [SerializeField, Header("Edges")]
        private bool m_AllowResizeFromLeft = false;
        public bool AllowResizeFromLeft
        {
            get { return m_AllowResizeFromLeft; }
            set
            {
                m_AllowResizeFromLeft = value;
                UpdateListeners();
            }
        }

        [SerializeField]
        private bool m_AllowResizeFromRight = true;
        public bool AllowResizeFromRight
        {
            get { return m_AllowResizeFromRight; }
            set
            {
                m_AllowResizeFromRight = value;
                UpdateListeners();
            }
        }

        [SerializeField]
        private bool m_AllowResizeFromTop = false;
        public bool AllowResizeFromTop
        {
            get { return m_AllowResizeFromTop; }
            set
            {
                m_AllowResizeFromTop = value;
                UpdateListeners();
            }
        }

        [SerializeField]
        private bool m_AllowResizeFromBottom = true;
        public bool AllowResizeFromBottom
        {
            get { return m_AllowResizeFromBottom; }
            set
            {
                m_AllowResizeFromBottom = value;
                UpdateListeners();
            }
        }

        [Header("Corners")]
        [SerializeField]
        private bool m_AllowResizeFromTopLeft = false;
        public bool AllowResizeFromTopLeft
        {
            get { return m_AllowResizeFromTopLeft;  }
            set
            {
                m_AllowResizeFromTopLeft = value;
                UpdateListeners();
            }
        }

        [SerializeField]
        private bool m_AllowResizeFromTopRight = false;
        public bool AllowResizeFromTopRight
        {
            get { return m_AllowResizeFromTopRight; }
            set
            {
                m_AllowResizeFromTopRight = value;
                UpdateListeners();
            }
        }

        [SerializeField]
        private bool m_AllowResizeFromBottomLeft = false;
        public bool AllowResizeFromBottomLeft
        {
            get { return m_AllowResizeFromBottomLeft; }
            set
            {
                m_AllowResizeFromBottomLeft = value;
                UpdateListeners();
            }
        }

        [SerializeField]
        private bool m_AllowResizeFromBottomRight = true;
        public bool AllowResizeFromBottomRight
        {
            get { return m_AllowResizeFromBottomRight; }
            set
            {
                m_AllowResizeFromBottomRight = value;
                UpdateListeners();
            }
        }


        [Header("Size Restrictions")]
        public Vector2 MinSize = Vector2.zero;
        public Vector2 MaxSize = Vector2.zero;

        [SerializeField]
        private bool m_KeepWithinParent = true;
        public bool KeepWithinParent
        {
            get { return m_KeepWithinParent; }
            set { m_KeepWithinParent = value; }
        }

        [Header("Aspect Ratio")]
        public eAspectRatioMode AspectRatioControl = eAspectRatioMode.None;
        public Vector2 DesiredAspectRatio = Vector2.one;

        [Header("Pivot")]
        public bool AdjustPivot = true;

        [SerializeField, Range(1, 256), Header("Resize Listeners")]
        private float m_ResizeListenerThickness = 16;
        public float ResizeListenerThickness
        {
            get { return m_ResizeListenerThickness; }
            set
            {
                m_ResizeListenerThickness = value;
                UpdateListeners();
            }
        }

        [SerializeField]
        private Color m_ResizeListenerColor = Color.clear;
        public Color ResizeListenerColor
        {
            get { return m_ResizeListenerColor; }
            set
            {
                m_ResizeListenerColor = value;
                UpdateListeners();
            }
        }

        [System.Serializable]
        public class ResizeListenerTypeEvent : UnityEvent<eResizeListenerType> { }

        /// <summary>
        /// Event called when the pointer enters a resize listener
        /// </summary>
        [HideInInspector]
        public ResizeListenerTypeEvent OnPointerEnterResizeListener = new ResizeListenerTypeEvent();
        /// <summary>
        /// Event called when the pointer exits a resize listener
        /// </summary>
        [HideInInspector]
        public ResizeListenerTypeEvent OnPointerExitResizeListener = new ResizeListenerTypeEvent();
        /// <summary>
        /// Event called when a resize begins
        /// </summary>
        [HideInInspector]
        public ResizeListenerTypeEvent OnResizeBegin = new ResizeListenerTypeEvent();
        /// <summary>
        /// Event called when a resize ends
        /// </summary>
        [HideInInspector]
        public UnityEvent OnResizeEnd = new UnityEvent();

        private Dictionary<eResizeListenerType, uResize_ResizeListener> ResizeListeners = new Dictionary<eResizeListenerType, uResize_ResizeListener>();

        [SerializeField, HideInInspector]
        private RectTransform ResizeListenerContainer;

        private RectTransform m_rectTransform;
        private RectTransform rectTransform
        {
            get
            {
                if (m_rectTransform == null) m_rectTransform = this.GetComponent<RectTransform>();
                return m_rectTransform;
            }
        }

        private Canvas m_canvas;
        private Canvas canvas
        {
            get
            {
                if (m_canvas == null) m_canvas = this.GetComponentInParent<Canvas>();
                return m_canvas;
            }
        }

        private LayoutElement m_layoutElement;
        private Vector2 m_pivotBeforeResize;
        private Vector2 m_anchorMinBeforeResize;
        private Vector2 m_anchorMaxBeforeResize;

        private Vector3[] m_parentCorners = new Vector3[4];
        private Vector3[] m_thisCorners = new Vector3[4];

        private void Awake()
        {
            if (ResizeListenerContainer == null) return;

            for (int i = ResizeListenerContainer.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Destroy(ResizeListenerContainer.GetChild(i).gameObject);
                }
                else
                {
                    DestroyImmediate(ResizeListenerContainer.GetChild(i).gameObject);
                }
            }
        }

        private void OnEnable()
        {
            UpdateListeners();

            m_layoutElement = this.GetComponent<LayoutElement>();

            // Show the listeners if need be
            if (ResizeListenerContainer != null && !ResizeListenerContainer.gameObject.activeInHierarchy)
            {
                ResizeListenerContainer.gameObject.SetActive(true);
            }
        }

        private void OnDisable()
        {
            // Hide the listeners if we're disabled
            if (ResizeListenerContainer != null)
            {
                ResizeListenerContainer.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Update all resize listeners
        /// </summary>
        public void UpdateListeners()
        {
            if (!Application.isPlaying) return;

            // Limpiar referencias destruidas antes de ordenar/actualizar.
            var staleKeys = ResizeListeners
                .Where(rl => rl.Value == null)
                .Select(rl => rl.Key)
                .ToList();

            foreach (var key in staleKeys)
            {
                ResizeListeners.Remove(key);
            }

            UpdateListener(eResizeListenerType.Left, AllowResizeFromLeft);
            UpdateListener(eResizeListenerType.Right, AllowResizeFromRight);
            UpdateListener(eResizeListenerType.Top, AllowResizeFromTop);
            UpdateListener(eResizeListenerType.Bottom, AllowResizeFromBottom);

            UpdateListener(eResizeListenerType.TopLeft, AllowResizeFromTopLeft);
            UpdateListener(eResizeListenerType.TopRight, AllowResizeFromTopRight);

            UpdateListener(eResizeListenerType.BottomLeft, AllowResizeFromBottomLeft);
            UpdateListener(eResizeListenerType.BottomRight, AllowResizeFromBottomRight);

            // Ensure that the listeners are ordered correctly
            // This ensures that the TopLeft/TopRight/BottomLeft/BottomRight listeners are above
            // the others, so they will intercept any corner drags
            var orderedListeners = ResizeListeners
                .Where(rl => rl.Value != null)
                .OrderBy(rl => rl.Key)
                .Select(rl => rl.Value)
                .ToList();
            int i = 0;
            foreach (var listener in orderedListeners)
            {
                if (listener != null)
                    listener.transform.SetSiblingIndex(i++);
            }
        }

        /// <summary>
        /// Update an individual resize listener
        /// (And create it if needed)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="enabled"></param>
        private void UpdateListener(eResizeListenerType type, bool enabled)
        {
            uResize_ResizeListener listener = null;

            if (ResizeListeners.ContainsKey(type))
            {
                listener = ResizeListeners[type];

                // Si la referencia fue destruida, removerla para recrearla de forma segura.
                if (listener == null)
                {
                    ResizeListeners.Remove(type);
                    listener = null;
                }
            }
            else if (enabled) // don't create a listener if one isn't required
            {
                listener = CreateListener(type);
                ResizeListeners.Add(type, listener);
            }

            if (listener != null)
            {
                listener.ImageComponent.color = ResizeListenerColor;
                UpdateResizeListenerPositionAndDimensions(listener, type);
            }

            if (listener != null) listener.gameObject.SetActive(enabled);
        }

        /// <summary>
        /// Create a GameObject to store all resize listener objects
        /// a) This helps keep things neater; and
        /// b) ensures that listeners/etc. aren't affected by any layout groups
        /// </summary>
        private void CreateResizeListenerContainer()
        {
            var gameObject = new GameObject("Resize Listeners", typeof(RectTransform), typeof(LayoutElement));
            var rectTransform = gameObject.GetComponent<RectTransform>();

            rectTransform.SetParent(this.transform);

            // fill the parent container
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition3D = Vector3.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;

            ResizeListenerContainer = rectTransform;

            // Ensure that the container is ignored by (and doesn't interfere with) any layout groups in this element
            var layoutElement = gameObject.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
        }

        /// <summary>
        /// Create a resize listener for the specified listener type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private uResize_ResizeListener CreateListener(eResizeListenerType type)
        {
            var gameObject = new GameObject(type.ToString(), typeof(uResize_ResizeListener));
            var listener = gameObject.GetComponent<uResize_ResizeListener>();
            var listenerRectTransform = gameObject.GetComponent<RectTransform>();

            // Create the container if it does not already exist
            if (ResizeListenerContainer == null)
            {
                CreateResizeListenerContainer();
            }

            listenerRectTransform.SetParent(ResizeListenerContainer);
            listenerRectTransform.anchoredPosition3D = Vector3.zero;
            listenerRectTransform.localScale = Vector3.one;
            listenerRectTransform.localRotation = Quaternion.identity;

            // Set up events - listeners will call the relevant events on this component
            listener.OnBeginDragEvent = () => { BeginResize(type); };
            listener.OnEndDragEvent = EndResize;
            listener.OnDragEvent = (ev) => { Resize(type, ev.delta); };

            // These are used (optionally), by the uResize_CursorController
            listener.OnPointerEnterEvent = () => { OnPointerEnterResizeListener.Invoke(type); };
            listener.OnPointerExitEvent = () => { OnPointerExitResizeListener.Invoke(type); };

            return listener;
        }

        /// <summary>
        /// Adjust the position/size/etc. of the specified listener
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="type"></param>
        private void UpdateResizeListenerPositionAndDimensions(uResize_ResizeListener listener, eResizeListenerType type)
        {
            var listenerRectTransform = listener.GetComponent<RectTransform>();

            var edges = uResize_ListenerEdges.GetEdgesForListenerType(type);

            listenerRectTransform.pivot = edges.pivot;
            listenerRectTransform.SetInsetAndSizeFromParentEdge(edges.edgeA, 0, ResizeListenerThickness);

            if (edges.isCorner)
            {
                // Corners are squares, set the width/height to 'ResizeListenerThickness'
                listenerRectTransform.SetInsetAndSizeFromParentEdge(edges.edgeB.Value, 0, ResizeListenerThickness);
            }
            else
            {
                if (edges.edgeA == RectTransform.Edge.Top || edges.edgeA == RectTransform.Edge.Bottom)
                {
                    // Stretch to the full width of this element
                    listenerRectTransform.anchorMin = new Vector2(0, listenerRectTransform.anchorMin.y);
                    listenerRectTransform.anchorMax = new Vector2(1, listenerRectTransform.anchorMax.y);
                    listenerRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, this.rectTransform.rect.width);
                }
                else
                {
                    // Stretch to the full height of this element
                    listenerRectTransform.anchorMin = new Vector2(listenerRectTransform.anchorMin.x, 0);
                    listenerRectTransform.anchorMax = new Vector2(listenerRectTransform.anchorMax.x, 1);
                    listenerRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, this.rectTransform.rect.height);
                }
            }

            listenerRectTransform.anchoredPosition = Vector2.zero;
        }


        /// <summary>
        /// Called by resize listeners when the user starts dragging
        /// </summary>
        /// <param name="resizeType"></param>
        private void BeginResize(eResizeListenerType resizeType)
        {
            if (!enabled) return;

            if (AdjustPivot)
            {
                // store the pivot prior to the resize event
                m_pivotBeforeResize = rectTransform.pivot;

                // Adjust the pivot so that when resizing edges, the other edges of the element remain in place
                // (If we do't do this, then the element will be resized around its existing pivot)
                SetPivot(GetResizePivot(resizeType));
            }

            // Preserve the anchor values so we can reset them later
            m_anchorMinBeforeResize = rectTransform.anchorMin;
            m_anchorMaxBeforeResize = rectTransform.anchorMax;

            // Set the new anchor values
            SetAnchors(uResize_Vectors.MiddleCenter, uResize_Vectors.MiddleCenter);

            OnResizeBegin.Invoke(resizeType);
        }

        /// <summary>
        /// Called by resize listeners when the user stops dragging
        /// </summary>
        private void EndResize()
        {
            if (!enabled) return;

            // Restore the original anchors
            SetAnchors(m_anchorMinBeforeResize, m_anchorMaxBeforeResize);

            // Restore the original pivot value
            if (AdjustPivot) SetPivot(m_pivotBeforeResize);

            OnResizeEnd.Invoke();
        }

        /// <summary>
        /// Called repeatedly during a drag event
        /// </summary>
        /// <param name="resizeType"></param>
        /// <param name="delta"></param>
        private void Resize(eResizeListenerType resizeType, Vector2 delta)
        {
            if (!enabled) return;

            var size = rectTransform.rect.size;

            bool isHorizontal = resizeType.IsHorizontal();
            bool isVertical = resizeType.IsVertical();

            bool inverseHorizontal = false;
            bool inverseVertical = false;

            if (isHorizontal)
            {
                // If we're dragging the left side, then invert delta.x
                inverseHorizontal = resizeType.IsInverseHorizontal();
            }

            if (isVertical)
            {
                // If we're dragging the right side, then invert delta.y
                inverseVertical = resizeType.IsInverseVertical();
            }

            size += new Vector2
                (
                    isHorizontal ? inverseHorizontal ? -delta.x : delta.x : 0,
                    isVertical ? inverseVertical ? delta.y : -delta.y : 0
                );

            if (AspectRatioControl != eAspectRatioMode.None)
            {
                var ratio = DesiredAspectRatio.x / DesiredAspectRatio.y;

                ePlane primaryPlane = ePlane.x;
                switch(AspectRatioControl)
                {
                    case eAspectRatioMode.Auto:
                        if (isHorizontal)
                        {
                            primaryPlane = ePlane.x;
                        }
                        else
                        {
                            primaryPlane = ePlane.y;
                        }
                        break;
                    case eAspectRatioMode.HeightControlsWidth:
                        primaryPlane = ePlane.y;
                        break;
                }

                if (primaryPlane == ePlane.x)
                {
                    size = new Vector2(size.x, size.x / ratio);
                }
                else
                {
                    size = new Vector2(size.y * ratio, size.y);
                }
            }

            // Clamp the updated size within MinSize/MaxSize
            size = new Vector2
                (
                    Mathf.Clamp(size.x, MinSize.x, MaxSize.x > 0 ? MaxSize.x : float.MaxValue),
                    Mathf.Clamp(size.y, MinSize.y, MaxSize.y > 0 ? MaxSize.y : float.MaxValue)
                );

            // Apply the new size to the RectTransform
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

            if (KeepWithinParent)
            {
                var parent = rectTransform.parent as RectTransform;
                parent.GetWorldCorners(m_parentCorners);
                // convert the parent corners into the same space as the local corners
                for (int i = 0; i < 4; i++)
                {
                    m_parentCorners[i] = rectTransform.InverseTransformPoint(m_parentCorners[i]);
                }

                rectTransform.GetLocalCorners(m_thisCorners);

                float left, right, top, bottom;

                // Calculate the distance from each parent edge
                left = m_thisCorners[0].x - m_parentCorners[0].x;
                right = m_parentCorners[2].x - m_thisCorners[2].x;
                top = m_parentCorners[2].y - m_thisCorners[2].y;
                bottom = m_thisCorners[0].y - m_parentCorners[0].y;

                if (left < 0 || right < 0 || top < 0 || bottom < 0)
                {
                    if (AspectRatioControl != eAspectRatioMode.None)
                    {
                        bool onlyHorizontal = isHorizontal && !isVertical;
                        bool onlyVertical = isVertical && !isHorizontal;

                        // If we're using an Aspect Ratio, and only resizing in one direction,
                        // then the element will be increasing in size from both sides (equally)
                        // so we need to double the amount we're reducing the size by
                        if (onlyHorizontal)
                        {
                            top *= 2;
                            bottom *= 2;
                        }
                        else if (onlyVertical)
                        {
                            left *= 2;
                            right *= 2;
                        }
                    }

                    // Adjust size so that this fits within the parent
                    if (left < 0) size.x += left;
                    if (right < 0) size.x += right;
                    if (top < 0) size.y += top;
                    if (bottom < 0) size.y += bottom;

                    // Apply the adjusted size to the RectTransform
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                }
            }

            // If we have a layout element, set its preferred width/height to match
            if (m_layoutElement != null)
            {
                m_layoutElement.preferredWidth = size.x;
                m_layoutElement.preferredHeight = size.y;
            }
        }

        private Vector2 GetResizePivot(eResizeListenerType resizeListenerType)
        {
            // We'll be setting the pivot to the opposite side of whatever edge/corner we are resizing from
            switch (resizeListenerType)
            {
                case eResizeListenerType.Right:
                    return uResize_Vectors.Left;

                case eResizeListenerType.Left:
                    return uResize_Vectors.Right;

                case eResizeListenerType.Bottom:
                    return uResize_Vectors.Top;

                case eResizeListenerType.BottomRight:
                    return uResize_Vectors.TopLeft;

                case eResizeListenerType.BottomLeft:
                    return uResize_Vectors.TopRight;

                case eResizeListenerType.TopLeft:
                    return uResize_Vectors.BottomRight;

                case eResizeListenerType.TopRight:
                    return uResize_Vectors.BottomLeft;

                case eResizeListenerType.Top:
                    return uResize_Vectors.Bottom;
            }

            return Vector2.zero;
        }

        private void SetPivot(Vector2 pivot)
        {
            if (rectTransform == null) return;

            SetPivotSmart(pivot.x, 0);
            SetPivotSmart(pivot.y, 1);
        }

        /// <summary>
        /// Set a new pivot value without the RectTransform appearing to move
        /// Based on UnityEditor.RectTransformEditor.SetPivotSmart
        /// </summary>
        /// <param name="value"></param>
        /// <param name="axis"></param>
        private void SetPivotSmart(float value, int axis)
        {
            Vector3 cornerBefore = GetRectReferenceCorner();

            Vector2 rectPivot = rectTransform.pivot;
            rectPivot[axis] = value;
            rectTransform.pivot = rectPivot;

            Vector3 cornerAfter = GetRectReferenceCorner();
            Vector3 cornerOffset = cornerAfter - cornerBefore;
            rectTransform.anchoredPosition -= (Vector2)cornerOffset;

            Vector3 pos = rectTransform.transform.position;
            pos.z -= cornerOffset.z;
            rectTransform.transform.position = pos;
        }

        private Vector3[] s_Corners = new Vector3[4];
        private Vector3 GetRectReferenceCorner()
        {
            rectTransform.GetWorldCorners(s_Corners);
            if (rectTransform.parent)
                return rectTransform.parent.InverseTransformPoint(s_Corners[0]);
            else
                return s_Corners[0];

        }

        private void SetAnchors(Vector2 newAnchorMin, Vector2 newAnchorMax)
        {
            SetAnchorSmart(newAnchorMin.x, 0, false);
            SetAnchorSmart(newAnchorMin.y, 1, false);

            SetAnchorSmart(newAnchorMax.x, 0, true);
            SetAnchorSmart(newAnchorMax.y, 1, true);
        }

        private bool ShouldDoIntSnapping()
        {
            return (canvas != null && canvas.renderMode != RenderMode.WorldSpace);
        }

        static float Round(float value) { return Mathf.Floor(0.5f + value); }

        /// <summary>
        /// Set a new anchor value without the RectTransform appearing to move
        /// Based on UnityEditor.RectTransformEditor.SetAnchorSmart
        /// </summary>
        /// <param name="value"></param>
        /// <param name="axis"></param>
        /// <param name="isMax"></param>
        public void SetAnchorSmart(float value, int axis, bool isMax)
        {
            RectTransform parent = rectTransform.parent as RectTransform;

            value = Mathf.Clamp01(value);

            float offsetSizePixels = 0;
            float offsetPositionPixels = 0;

            float oldValue = isMax ? rectTransform.anchorMax[axis] : rectTransform.anchorMin[axis];

            // Calculate the difference in position based on the old anchor value and the new
            offsetSizePixels = (value - oldValue) * parent.rect.size[axis];

            float roundingDelta = 0;
            if (ShouldDoIntSnapping())
            {
                roundingDelta = Mathf.Round(offsetSizePixels) - offsetSizePixels;
            }

            offsetSizePixels += roundingDelta;
            offsetPositionPixels = (isMax ? offsetSizePixels * rectTransform.pivot[axis] : (offsetSizePixels * (1 - rectTransform.pivot[axis])));

            if (isMax)
            {
                Vector2 rectAnchorMax = rectTransform.anchorMax;
                rectAnchorMax[axis] = value;
                rectTransform.anchorMax = rectAnchorMax;

                Vector2 other = rectTransform.anchorMin;
                rectTransform.anchorMin = other;
            }
            else
            {
                Vector2 rectAnchorMin = rectTransform.anchorMin;
                rectAnchorMin[axis] = value;
                rectTransform.anchorMin = rectAnchorMin;

                Vector2 other = rectTransform.anchorMax;
                rectTransform.anchorMax = other;
            }

            // Adjust the anchored position to account for the changed anchor
            Vector2 rectPosition = rectTransform.anchoredPosition;
            rectPosition[axis] -= offsetPositionPixels;
            rectTransform.anchoredPosition = rectPosition;

            // Adjust the  size delta to account for the changed anchor
            Vector2 rectSizeDelta = rectTransform.sizeDelta;
            rectSizeDelta[axis] += offsetSizePixels * (isMax ? -1 : 1);
            rectTransform.sizeDelta = rectSizeDelta;
        }
    }
}
