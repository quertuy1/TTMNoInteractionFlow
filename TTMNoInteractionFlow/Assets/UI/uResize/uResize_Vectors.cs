using UnityEngine;
using UnityEngine.UI;

namespace DigitalLegacy.UI.Sizing
{
    /// <summary>
    /// Helper class containing commonly-used vectors.
    /// This avoids instantiating identical vectors
    /// </summary>
    public static class uResize_Vectors
    {
        public static Vector2 MiddleCenter = new Vector2(0.5f, 0.5f);

        public static Vector2 Left = new Vector2(0, 0.5f);
        public static Vector2 Right = new Vector2(1, 0.5f);

        public static Vector2 Top = new Vector2(0.5f, 1);
        public static Vector2 Bottom = new Vector2(0.5f, 0);

        public static Vector2 BottomRight = new Vector2(1, 0);
        public static Vector2 BottomLeft = new Vector2(0, 0);

        public static Vector2 TopRight = new Vector2(1, 1);
        public static Vector2 TopLeft = new Vector2(0, 1);     
    }
}
