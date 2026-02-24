#if UNITY_2019_1_OR_NEWER
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityHierarchyFolders.Runtime;

namespace UnityHierarchyFolders.Editor
{
    [CustomEditor(typeof(Folder))]
    public class FolderEditor : UnityEditor.Editor
    {
        private bool _expanded = false;


        static readonly string[] s_DefaultIconColorNames =
            new[] { "Default", "Yellow", "Blue", "Green", "Red" };

        public override bool RequiresConstantRepaint() => true;
        public override void OnInspectorGUI()
        {
            this._expanded = EditorGUILayout.Foldout(this._expanded, "Icon Color", true);
            if (this._expanded) { this.RenderColorPicker(); }
        }
        void RenderColorPicker()
        {
            serializedObject.Update();

            var colors = HierarchyFolderIcon.IconColors;
            var names = s_DefaultIconColorNames;
            if (names == null || names.Length != colors.Length)
                names = Enumerable.Range(0, colors.Length).Select(i => $"Color {i}").ToArray();

            // Use the actual backing field name
            var pIndex = serializedObject.FindProperty("_colorIndex");
            if (pIndex == null)
            {
                EditorGUILayout.HelpBox("Missing '_colorIndex' property.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            pIndex.intValue = Mathf.Clamp(pIndex.intValue, 0, colors.Length - 1);

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("Folder Color", pIndex.intValue , names);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Change Folder Color");
                pIndex.intValue = newIndex;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                // Force repaint so the icon updates immediately
                HierarchyFolderIcon.ForceRepaint();
                return; // avoid drawing preview with stale value this frame
            }

            var previewRect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(previewRect, colors[pIndex.intValue]);

            serializedObject.ApplyModifiedProperties();
        }

    }
}
#endif