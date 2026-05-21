using UnityEngine;
using UnityEditor;
using System.Collections;

namespace DigitalLegacy.UI.Sizing
{
    [CustomEditor(typeof(uResize)), CanEditMultipleObjects]
    public class uResize_Editor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            base.OnInspectorGUI();

            if (!EditorGUI.EndChangeCheck()) return;

            ((uResize)target).UpdateListeners();
        }
    }
}
