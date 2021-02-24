#if UNITY_2019_1_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityHierarchyFolders.Runtime;
using Object = UnityEngine.Object;

namespace UnityHierarchyFolders.Editor
{
    public static class HierarchyFolderIcon
    {
#if UNITY_2020_1_OR_NEWER
        private const string _openedFolderPrefix = "FolderOpened";
#else
        private const string _openedFolderPrefix = "OpenedFolder";
#endif
        private const string _closedFolderPrefix = "Folder";

        private static Texture2D _openFolderTexture;
        private static Texture2D _closedFolderTexture;
        private static Texture2D _openFolderSelectedTexture;
        private static Texture2D _closedFolderSelectedTexture;

        private static bool _isInitialized;
        private static bool _hasProcessedFrame = true;

        // Reflected members
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Special naming scheme")]
        private static PropertyInfo prop_sceneHierarchy;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Special naming scheme")]
        private static PropertyInfo prop_treeView;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Special naming scheme")]
        private static PropertyInfo prop_data;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Special naming scheme")]
        private static PropertyInfo prop_selectedIcon;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Special naming scheme")]
        private static PropertyInfo prop_objectPPTR;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Special naming scheme")]
        private static MethodInfo meth_getRows;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Special naming scheme")]
        private static MethodInfo meth_isExpanded;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Special naming scheme")]
        private static MethodInfo meth_getAllSceneHierarchyWindows;

        private static (Texture2D open, Texture2D closed)[] _coloredFolderIcons;
        public static (Texture2D open, Texture2D closed) ColoredFolderIcons(int i) => _coloredFolderIcons[i];

        public static int IconColumnCount => IconColors.GetLength(0);
        public static int IconRowCount => IconColors.GetLength(1);

        private static readonly Color[,] IconColors = {
            {new Color(0.09f, 0.57f, 0.82f), new Color(0.05f, 0.34f, 0.48f),},
            {new Color(0.09f, 0.67f, 0.67f), new Color(0.05f, 0.42f, 0.42f),},
            {new Color(0.23f, 0.73f, 0.36f), new Color(0.15f, 0.41f, 0.22f),},
            {new Color(0.55f, 0.35f, 0.71f), new Color(0.35f, 0.24f, 0.44f),},
            {new Color(0.78f, 0.27f, 0.55f), new Color(0.52f, 0.15f, 0.35f),},
            {new Color(0.80f, 0.66f, 0.10f), new Color(0.56f, 0.46f, 0.02f),},
            {new Color(0.91f, 0.49f, 0.13f), new Color(0.62f, 0.33f, 0.07f),},
            {new Color(0.91f, 0.30f, 0.24f), new Color(0.77f, 0.15f, 0.09f),},
            {new Color(0.35f, 0.49f, 0.63f), new Color(0.24f, 0.33f, 0.42f),},
        };

        [InitializeOnLoadMethod]
        private static void Startup()
        {
            EditorApplication.update += ResetFolderIcons;
            EditorApplication.hierarchyWindowItemOnGUI += RefreshFolderIcons;
        }

        private static void InitIfNeeded()
        {
            if (_isInitialized) { return; }

            _openFolderTexture = (Texture2D)EditorGUIUtility.IconContent($"{_openedFolderPrefix} Icon").image;
            _closedFolderTexture = (Texture2D)EditorGUIUtility.IconContent($"{_closedFolderPrefix} Icon").image;

            // We could use the actual white folder icons but I prefer the look of the tinted white folder icon
            // To use the actual white version:
            // texture = (Texture2D) EditorGUIUtility.IconContent($"{OpenedFolderPrefix | ClosedFolderPrefix} On Icon").image;
            _openFolderSelectedTexture = TextureHelper.GetWhiteTexture(_openFolderTexture, $"{_openedFolderPrefix} Icon White");
            _closedFolderSelectedTexture = TextureHelper.GetWhiteTexture(_closedFolderTexture, $"{_closedFolderPrefix} Icon White");

            _coloredFolderIcons = new (Texture2D, Texture2D)[] { (_openFolderTexture, _closedFolderTexture) };

            for (int row = 0; row < IconRowCount; row++)
            {
                for (int column = 0; column < IconColumnCount; column++)
                {
                    int index = 1 + column + row * IconColumnCount;
                    var color = IconColors[column, row];

                    var openFolderIcon = TextureHelper.GetTintedTexture(_openFolderSelectedTexture,
                        color, $"{_openFolderSelectedTexture.name} {index}");
                    var closedFolderIcon = TextureHelper.GetTintedTexture(_closedFolderSelectedTexture,
                        color, $"{_closedFolderSelectedTexture.name} {index}");

                    ArrayUtility.Add(ref _coloredFolderIcons, (openFolderIcon, closedFolderIcon));
                }
            }

            // reflection

            const BindingFlags BindingAll = BindingFlags.Public
              | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            var assembly = typeof(SceneView).Assembly;

            var type_sceneHierarchyWindow = assembly.GetType("UnityEditor.SceneHierarchyWindow");
            meth_getAllSceneHierarchyWindows = type_sceneHierarchyWindow.GetMethod("GetAllSceneHierarchyWindows", BindingAll);
            prop_sceneHierarchy = type_sceneHierarchyWindow.GetProperty("sceneHierarchy");

            var type_sceneHierarchy = assembly.GetType("UnityEditor.SceneHierarchy");
            prop_treeView = type_sceneHierarchy.GetProperty("treeView", BindingAll);

            var type_treeViewController = assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewController");
            prop_data = type_treeViewController.GetProperty("data", BindingAll);

            var type_iTreeViewDataSource = assembly.GetType("UnityEditor.IMGUI.Controls.ITreeViewDataSource");
            meth_getRows = type_iTreeViewDataSource.GetMethod("GetRows");
            meth_isExpanded = type_iTreeViewDataSource.GetMethod("IsExpanded", new Type[] { typeof(TreeViewItem) });

            var type_gameObjectTreeViewItem = assembly.GetType("UnityEditor.GameObjectTreeViewItem");
            prop_selectedIcon = type_gameObjectTreeViewItem.GetProperty("selectedIcon", BindingAll);
            prop_objectPPTR = type_gameObjectTreeViewItem.GetProperty("objectPPTR", BindingAll);

            _isInitialized = true;
        }

        private static void ResetFolderIcons()
        {
            InitIfNeeded();
            _hasProcessedFrame = false;
        }

        private static void RefreshFolderIcons(int instanceid, Rect selectionrect)
        {
            if (_hasProcessedFrame) { return; }

            _hasProcessedFrame = true;

            var windows = ((IEnumerable)meth_getAllSceneHierarchyWindows.Invoke(null, Array.Empty<object>())).Cast<EditorWindow>().ToList();
            foreach (var window in windows)
            {
                object sceneHierarchy = prop_sceneHierarchy.GetValue(window);
                object treeView = prop_treeView.GetValue(sceneHierarchy);
                object data = prop_data.GetValue(treeView);

                var rows = (IList<TreeViewItem>)meth_getRows.Invoke(data, Array.Empty<object>());
                foreach (var item in rows)
                {
                    var itemObject = (Object)prop_objectPPTR.GetValue(item);
                    if (!Folder.TryGetIconIndex(itemObject, out int colorIndex)) { continue; }

                    bool isExpanded = (bool)meth_isExpanded.Invoke(data, new object[] { item });

                    var icons = ColoredFolderIcons(Mathf.Clamp(colorIndex, 0, _coloredFolderIcons.Length - 1));

                    item.icon = isExpanded ? icons.open : icons.closed;

                    prop_selectedIcon.SetValue(item, isExpanded ? _openFolderSelectedTexture : _closedFolderSelectedTexture);
                }
            }
        }
    }
}
#endif