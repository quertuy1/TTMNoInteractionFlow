using UnityEngine;
using System;

namespace DigitalLegacy.UI.Sizing.Example
{
    public class ExampleController : MonoBehaviour
    {
        public uResize uResize;

        public void ToggleShowResizeListeners(bool show)
        {
            if (show)
            {
                uResize.ResizeListenerColor = new Color(255, 255, 255, 0.5f);
            }
            else
            {
                uResize.ResizeListenerColor = Color.clear;
            }
        }
    }
}
