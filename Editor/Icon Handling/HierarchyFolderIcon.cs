#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityEngine;
using UnityHierarchyFolders.Runtime;

namespace UnityHierarchyFolders.Editor
{
    public static class HierarchyFolderIcon
    {
        public static readonly Color[] IconColors =
        {
            new Color(0.91f, 0.91f, 0.91f),
            new Color(0.98f, 0.80f, 0.27f),
            new Color(0.50f, 0.79f, 0.98f),
            new Color(0.63f, 0.90f, 0.68f),
            new Color(0.96f, 0.62f, 0.62f),
        };

        public static int IconCount => IconColors.Length;

        [InitializeOnLoadMethod]
        private static void Startup()
        {
#if UNITY_6000_5_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= RefreshFolderIcons;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += RefreshFolderIcons;
#else
            EditorApplication.hierarchyWindowItemOnGUI -= RefreshFolderIcons;
            EditorApplication.hierarchyWindowItemOnGUI += RefreshFolderIcons;
#endif
        }

        public static void ForceRepaint()
        {
            EditorApplication.RepaintHierarchyWindow();
        }

#if UNITY_6000_5_OR_NEWER
        private static void RefreshFolderIcons(EntityId entityId, Rect selectionRect)
        {
            UnityEngine.Object obj = EditorUtility.EntityIdToObject(entityId);
            if (!Folder.TryGetIconIndex(obj, out int colorIndex))
                return;

            DrawColorMarker(selectionRect, colorIndex);
        }
#else
        private static void RefreshFolderIcons(int instanceId, Rect selectionRect)
        {
            UnityEngine.Object obj = EditorUtility.InstanceIDToObject(instanceId);
            if (!Folder.TryGetIconIndex(obj, out int colorIndex))
                return;

            DrawColorMarker(selectionRect, colorIndex);
        }
#endif

        private static void DrawColorMarker(Rect selectionRect, int colorIndex)
        {
            if (IconColors.Length == 0)
                return;

            int index = Mathf.Clamp(colorIndex, 0, IconColors.Length - 1);

            Rect markerRect = selectionRect;
            markerRect.width = 4f;
            markerRect.x += 2f;

            EditorGUI.DrawRect(markerRect, IconColors[index]);
        }
    }
}
#endif
