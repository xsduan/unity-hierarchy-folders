#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityEngine;
using UnityHierarchyFolders.Runtime;

namespace UnityHierarchyFolders.Editor
{
    [CustomEditor(typeof(Folder))]
    public class FolderEditor : UnityEditor.Editor
    {
        private static readonly string[] s_DefaultIconColorNames =
            { "Default", "Yellow", "Blue", "Green", "Red" };

        private bool _expanded;

        public override void OnInspectorGUI()
        {
            _expanded = EditorGUILayout.Foldout(_expanded, "Icon Color", true);
            if (_expanded)
                RenderColorPicker();
        }

        private void RenderColorPicker()
        {
            serializedObject.Update();

            Color[] colors = HierarchyFolderIcon.IconColors;
            string[] names = s_DefaultIconColorNames.Length == colors.Length
                ? s_DefaultIconColorNames
                : null;

            SerializedProperty pIndex = serializedObject.FindProperty("_colorIndex");
            if (pIndex == null)
            {
                EditorGUILayout.HelpBox("Missing '_colorIndex' property.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            pIndex.intValue = Mathf.Clamp(pIndex.intValue, 0, colors.Length - 1);

            EditorGUI.BeginChangeCheck();
            int newIndex = names != null
                ? EditorGUILayout.Popup("Folder Color", pIndex.intValue, names)
                : EditorGUILayout.IntSlider("Folder Color", pIndex.intValue, 0, colors.Length - 1);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Change Folder Color");
                pIndex.intValue = Mathf.Clamp(newIndex, 0, colors.Length - 1);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                HierarchyFolderIcon.ForceRepaint();
                return;
            }

            Rect previewRect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(previewRect, colors[pIndex.intValue]);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
