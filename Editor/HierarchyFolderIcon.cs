#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityHierarchyFolders.Runtime;

namespace UnityHierarchyFolders.Editor
{
    [InitializeOnLoad]
    internal static class HierarchyFolderIcon
    {
        public static readonly Color[] IconColors =
        {
            new Color(0.80f, 0.80f, 0.80f, 1.00f),
            new Color(1.00f, 0.78f, 0.20f, 1.00f),
            new Color(0.35f, 0.65f, 1.00f, 1.00f),
            new Color(0.35f, 0.85f, 0.45f, 1.00f),
            new Color(1.00f, 0.35f, 0.35f, 1.00f),
        };

        static HierarchyFolderIcon()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindowItemGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;
        }

        public static void ForceRepaint()
        {
            EditorApplication.RepaintHierarchyWindow();
        }

        private static void OnHierarchyWindowItemGUI(int instanceID, Rect selectionRect)
        {
            if (!Folder.TryGetIconIndex(instanceID, out int iconIndex))
                return;

            Rect iconRect = selectionRect;
            iconRect.width = 4f;
            iconRect.x += 2f;
            iconIndex = Mathf.Clamp(iconIndex, 0, IconColors.Length - 1);
            EditorGUI.DrawRect(iconRect, IconColors[iconIndex]);
        }
    }
}
#endif
