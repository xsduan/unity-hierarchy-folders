using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityHierarchyFolders.Runtime;
using Object = UnityEngine.Object;

namespace UnityHierarchyFolders.Editor
{
    public class HierarchyFolderIcon
    {
#if UNITY_2020_1_OR_NEWER
        private const string OpenedFolderPrefix = "FolderOpened";
#else
        private const string OpenedFolderPrefix = "OpenedFolder";
#endif
        private const string ClosedFolderPrefix = "Folder";
        
        private static Texture2D openFolderTexture;
        private static Texture2D closedFolderTexture;
        private static Texture2D openFolderSelectedTexture;
        private static Texture2D closedFolderSelectedTexture;
        
        private static bool isInitialized;
        private static bool hasProcessedFrame = true; 
        
        // Reflected members
        private static PropertyInfo prop_sceneHierarchy;
        private static PropertyInfo prop_treeView;
        private static PropertyInfo prop_data;
        private static PropertyInfo prop_selectedIcon;
        private static PropertyInfo prop_objectPPTR;

        private static MethodInfo meth_getRows;
        private static MethodInfo meth_isExpanded;
        private static MethodInfo meth_getAllSceneHierarchyWindows;

        private static (Texture2D, Texture2D)[] _coloredFolderIcons; 
        public static (Texture2D, Texture2D)[] coloredFolderIcons => _coloredFolderIcons;
        
        public static int IconColumnCount => IconColors.GetLength(0);
        public static int IconRowCount => IconColors.GetLength(1);

        public static readonly Color[,] IconColors = {
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
        static void Startup()
        { 
            EditorApplication.update += ResetFolderIcons;
            EditorApplication.hierarchyWindowItemOnGUI += RefreshFolderIcons;
        }

        private static Texture2D GetTintedTexture(Texture2D original, Color tint, string name = "")
        {
            Color32 TintColor(Color32 c)
            {
                return c * tint;
            }

            return GetColorizedTexture(original, TintColor, name);
        }
        
        private static Texture2D GetWhiteTexture(Texture2D original, string name = "")
        {
            Color32 MakeColorsWhite(Color32 c)
            {
                byte a = c.a;
                c = Color.HSVToRGB(0, 0, 1);
                c.a = a;
 
                return  c;
            }

            return GetColorizedTexture(original, MakeColorsWhite, name);
        }

        private static Texture2D GetColorizedTexture(Texture2D original, Func<Color32, Color32> colorManipulator, string name = "")
        {
            var tinted = new Texture2D(original.width, original.height, 
                original.graphicsFormat, original.mipmapCount, TextureCreationFlags.MipChain);

            tinted.name = name;
            
            Graphics.CopyTexture(original, tinted);
            
            var data = tinted.GetRawTextureData<Color32>();
            for (int index = 0, len = data.Length; index < len; index++)
            {
                data[index] = colorManipulator(data[index]);
            }

            var mipmapSize = tinted.width * tinted.height * 4;
            var offset = 0; 
            for (int index = 0; index < tinted.mipmapCount; index++)
            {
                tinted.SetPixelData(data, index, offset);
                mipmapSize >>= 2;
                offset += mipmapSize;
            }
            
            tinted.hideFlags = HideFlags.DontSave;
            tinted.Apply();
            
            return tinted;
        }
        
        private static void InitIfNeeded()
        {
            if (isInitialized) { return; }
            
            openFolderTexture = (Texture2D) EditorGUIUtility.IconContent($"{OpenedFolderPrefix} Icon").image;
            closedFolderTexture = (Texture2D) EditorGUIUtility.IconContent($"{ClosedFolderPrefix} Icon").image;
            
            // We could use the actual white folder icons but I prefer the look of the tinted folder icon
            // so I'm leaving this as a documented alternative.
            //openFolderSelectedTexture = (Texture2D) EditorGUIUtility.IconContent($"{OpenedFolderPrefix} On Icon").image;
            //closedFolderSelectedTexture = (Texture2D) EditorGUIUtility.IconContent($"{ClosedFolderPrefix} On Icon").image;
            openFolderSelectedTexture = GetWhiteTexture(openFolderTexture, $"{OpenedFolderPrefix} Icon White");
            closedFolderSelectedTexture = GetWhiteTexture(closedFolderTexture, $"{ClosedFolderPrefix} Icon White");
            
            _coloredFolderIcons = new (Texture2D, Texture2D)[] {(openFolderTexture, closedFolderTexture)};
            
            for (int row = 0; row < IconRowCount; row++)
            {
                for (int column = 0; column < IconColumnCount; column++)
                {
                    int index = 1 + column + row * IconColumnCount;
                    var color = IconColors[column, row];
                    
                    var openFolderIcon = GetTintedTexture(openFolderSelectedTexture, 
                        color, $"{openFolderSelectedTexture.name} {index}");
                    var closedFolderIcon = GetTintedTexture(closedFolderSelectedTexture, 
                        color, $"{closedFolderSelectedTexture.name} {index}");
                    
                    ArrayUtility.Add(ref _coloredFolderIcons, (openFolderIcon, closedFolderIcon));
                }
            }
            
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
            meth_isExpanded = type_iTreeViewDataSource.GetMethod("IsExpanded", new Type[] {typeof(TreeViewItem)});
            
            var type_gameObjectTreeViewItem = assembly.GetType("UnityEditor.GameObjectTreeViewItem");
            prop_selectedIcon = type_gameObjectTreeViewItem.GetProperty("selectedIcon", BindingAll);
            prop_objectPPTR = type_gameObjectTreeViewItem.GetProperty("objectPPTR", BindingAll);

            isInitialized = true;
        }

        private static void ResetFolderIcons()
        {
            InitIfNeeded();
            hasProcessedFrame = false;
        }

        private static void RefreshFolderIcons(int instanceid, Rect selectionrect)
        {
            if (hasProcessedFrame) { return; }
            
            hasProcessedFrame = true;
            
            var windows = ((IEnumerable)meth_getAllSceneHierarchyWindows.Invoke(null, Array.Empty<object>())).Cast<EditorWindow>().ToList();
            foreach (EditorWindow window in windows)
            {
                var sceneHierarchy = prop_sceneHierarchy.GetValue(window);
                var treeView = prop_treeView.GetValue(sceneHierarchy);
                var data = prop_data.GetValue(treeView);

                var rows = (IList<TreeViewItem>) meth_getRows.Invoke(data, Array.Empty<object>());
                foreach (TreeViewItem item in rows)
                {
                    var itemObject = (Object) prop_objectPPTR.GetValue(item);
                    if (!Folder.TryGetIconIndex(itemObject, out int colorIndex)) { continue; }

                    
                    var isExpanded = (bool) meth_isExpanded.Invoke(data, new object[] { item });
                    
                    Texture2D open, closed;
                    var count = _coloredFolderIcons.Length;
                    (open, closed) = coloredFolderIcons[Mathf.Clamp(colorIndex, 0, count) % count];
                    item.icon = isExpanded ? open : closed;
                    
                    prop_selectedIcon.SetValue(item, isExpanded ? openFolderSelectedTexture : closedFolderSelectedTexture);
                }
            }
        }
    }
}
