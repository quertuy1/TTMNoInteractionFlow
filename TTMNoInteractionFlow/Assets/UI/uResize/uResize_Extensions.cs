using System.Collections.Generic;

namespace DigitalLegacy.UI.Sizing
{
    public static class uResize_Extensions
    {
        private static Dictionary<eResizeListenerType, bool> isHorizontalCache = new Dictionary<eResizeListenerType, bool>();
        private static Dictionary<eResizeListenerType, bool> isVerticalCache = new Dictionary<eResizeListenerType, bool>();
        private static Dictionary<eResizeListenerType, bool> isInverseHorizontalCache = new Dictionary<eResizeListenerType, bool>();
        private static Dictionary<eResizeListenerType, bool> isInverseVerticalCache = new Dictionary<eResizeListenerType, bool>();

        /// <summary>
        /// Is this ResizeListenerType Horizontal?
        /// (cached)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsHorizontal(this eResizeListenerType type)
        {
            if (isHorizontalCache.ContainsKey(type)) return isHorizontalCache[type];

            bool isHorizontal = type.ToString().EndsWith("Left") || type.ToString().EndsWith("Right");

            isHorizontalCache.Add(type, isHorizontal);

            return isHorizontal;
        }
        
        /// <summary>
        /// Is this ResizeListenerType Vertical?
        /// (cached)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsVertical(this eResizeListenerType type)
        {
            if (isVerticalCache.ContainsKey(type)) return isVerticalCache[type];

            string typeString = type.ToString();
            bool isVertical = typeString.StartsWith("Top") || typeString.StartsWith("Bottom");

            isVerticalCache.Add(type, isVertical);

            return isVertical;
        }
        
        /// <summary>
        /// Is this ResizeListenerType Inverse in the Horizontal plane?
        /// (cached)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsInverseHorizontal(this eResizeListenerType type)
        {
            if (isInverseHorizontalCache.ContainsKey(type)) return isInverseHorizontalCache[type];

            bool isInverse = type.ToString().EndsWith("Left");

            isInverseHorizontalCache.Add(type, isInverse);

            return isInverse;
        }
        
        /// <summary>
        /// Is this ResizeListenerType Inverse in the Vertical plane?
        /// (cached)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsInverseVertical(this eResizeListenerType type)
        {
            if (isInverseVerticalCache.ContainsKey(type)) return isInverseVerticalCache[type];

            bool isInverse = type.ToString().StartsWith("Top");

            isInverseVerticalCache.Add(type, isInverse);

            return isInverse;
        }

    }
}
