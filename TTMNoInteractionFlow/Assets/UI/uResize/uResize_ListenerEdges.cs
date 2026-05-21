using UnityEngine;
using System.Collections.Generic;

namespace DigitalLegacy.UI.Sizing
{
    /// <summary>
    /// Helper class used by uResize associate edges with a particular listener/drag type
    /// (e.g. dragging from the top right requires the top and right edges)
    /// </summary>
    internal class uResize_ListenerEdges
    {
        public bool isCorner = false;
        public Vector2 pivot;
        public RectTransform.Edge edgeA;
        public RectTransform.Edge? edgeB;

        private static Dictionary<eResizeListenerType, uResize_ListenerEdges> listenerEdgesCache = new Dictionary<eResizeListenerType, uResize_ListenerEdges>();

        public uResize_ListenerEdges(bool c, Vector2 p, RectTransform.Edge a, RectTransform.Edge? b = null)
        {
            isCorner = c;
            pivot = p;
            edgeA = a;
            edgeB = b;
        }

        internal static uResize_ListenerEdges GetEdgesForListenerType(eResizeListenerType type)
        {
            if (listenerEdgesCache.ContainsKey(type)) return listenerEdgesCache[type];

            var edges = GetEdgesForListenerTypeUncached(type);

            listenerEdgesCache.Add(type, edges);

            return edges;
        }

        private static uResize_ListenerEdges GetEdgesForListenerTypeUncached(eResizeListenerType type)
        {
            switch (type)
            {
                case eResizeListenerType.Left:
                    return new uResize_ListenerEdges(false, new Vector2(0, 0.5f), RectTransform.Edge.Left);

                case eResizeListenerType.Right:
                    return new uResize_ListenerEdges(false, new Vector2(1, 0.5f), RectTransform.Edge.Right);

                case eResizeListenerType.Top:
                    return new uResize_ListenerEdges(false, new Vector2(0.5f, 1), RectTransform.Edge.Top);

                case eResizeListenerType.Bottom:
                    return new uResize_ListenerEdges(false, new Vector2(0.5f, 0), RectTransform.Edge.Bottom);

                case eResizeListenerType.TopLeft:
                    return new uResize_ListenerEdges(true, new Vector2(0, 1), RectTransform.Edge.Top, RectTransform.Edge.Left);

                case eResizeListenerType.TopRight:
                    return new uResize_ListenerEdges(true, new Vector2(1, 1), RectTransform.Edge.Top, RectTransform.Edge.Right);

                case eResizeListenerType.BottomLeft:
                    return new uResize_ListenerEdges(true, new Vector2(0, 0), RectTransform.Edge.Bottom, RectTransform.Edge.Left);

                case eResizeListenerType.BottomRight:
                    return new uResize_ListenerEdges(true, new Vector2(1, 0), RectTransform.Edge.Bottom, RectTransform.Edge.Right);
            };

            return null;
        }                    
    }
}
