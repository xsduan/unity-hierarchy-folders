using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Xml;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityHierarchyFolders.Runtime;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UnityHierarchyFolders.Editor
{
    public class FolderIcon
    {
        private static Texture2D _openFolderTexture;
        private static Texture2D _closedFolderTexture;
        private static Texture2D _openFolderSelectedTexture;
        private static Texture2D _closedFolderSelectedTexture;
        
        private static bool _isInitialized;
        private static bool _hasProcessedFrame = true; 
        
        // Reflected members
        private static PropertyInfo _sceneHierarchyProperty;
        private static PropertyInfo _treeViewProperty;
        private static PropertyInfo _dataProperty;
        private static MethodInfo _getRowsMethod;
        private static MethodInfo _isExpandedMethod;
        private static PropertyInfo _selectedIconProperty;
        private static PropertyInfo _objectPPTRProperty;
        private static MethodInfo _getAllSceneHierarchyWindowsMethod;


        private static (Texture2D, Texture2D)[] _coloredFolderIcons; 
        public static (Texture2D, Texture2D)[] coloredFolderIcons => _coloredFolderIcons;
        
        public static int IconHueCount => IconColors.GetLength(0);
        public static int IconValueCount => IconColors.GetLength(1);

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
            EditorApplication.update += PrepareFrame;
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

                Color.RGBToHSV(c, out float h, out float s, out float v);
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

            var aaa = tinted.width * tinted.height * 4;
            var offset = 0; 
            for (int index = 0; index < tinted.mipmapCount; index++)
            {
                tinted.SetPixelData(data, index, offset);
                aaa >>= 2;
                offset += aaa;
            }
            tinted.Apply();
            
            return tinted;
        }
        
        private static void InitIfNeeded()
        {
            if (_isInitialized)
            {
                return;
            }
            
            _openFolderTexture = (Texture2D) EditorGUIUtility.IconContent("FolderEmpty Icon").image;
            _closedFolderTexture = (Texture2D) EditorGUIUtility.IconContent("Folder Icon").image;
            
            _coloredFolderIcons = new (Texture2D, Texture2D)[] {(_openFolderTexture, _closedFolderTexture)};
 
            _openFolderSelectedTexture = GetWhiteTexture(_openFolderTexture, "FolderEmpty Icon Selected");
            _closedFolderSelectedTexture = GetWhiteTexture(_closedFolderTexture, "Folder Icon Selected");

            float Map(int index, int steps, float outFrom = 0, float outTo = 1)
            {
                return (float)index / steps * (outTo - outFrom) + outFrom;
            }
            
            for (int valueIndex = 0; valueIndex < IconValueCount; valueIndex++)
            {
                for (int hueIndex = 0; hueIndex < IconHueCount; hueIndex++)
                {
                    int index = valueIndex * IconHueCount + hueIndex + 1;
//                    float hue = Map(hueIndex, IconHueCount);
//                    float value = Map(valueIndex, IconValueCount, .77f, .3f);
//                    float saturation = Map(valueIndex, IconValueCount, 1f, .6f);
//                    var color = Color.HSVToRGB(hue, saturation, value);

                    var color = IconColors[hueIndex, valueIndex];
                    
                    var openFolderIcon = GetTintedTexture(_openFolderSelectedTexture, color, "FolderEmpty Icon " + index);
                    var closedFolderIcon = GetTintedTexture(_closedFolderSelectedTexture, color, "Folder Icon " + index);
                    
                    ArrayUtility.Add(ref _coloredFolderIcons, (openFolderIcon, closedFolderIcon));
                }
            }
            
            const BindingFlags BindingAll = BindingFlags.Public 
              | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            
            //public static List<SceneHierarchyWindow> GetAllSceneHierarchyWindows()
            var assembly = typeof(SceneView).Assembly;

            var sceneHierarchyWindowType = assembly.GetType("UnityEditor.SceneHierarchyWindow");
            _getAllSceneHierarchyWindowsMethod = sceneHierarchyWindowType.GetMethod("GetAllSceneHierarchyWindows", BindingAll);
            _sceneHierarchyProperty = sceneHierarchyWindowType.GetProperty("sceneHierarchy");

            var sceneHierarchyType = assembly.GetType("UnityEditor.SceneHierarchy");
            _treeViewProperty = sceneHierarchyType.GetProperty("treeView", BindingAll);
            
            var treeViewControllerType = assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewController");
            _dataProperty = treeViewControllerType.GetProperty("data", BindingAll);

            var iTreeViewDataSourceType = assembly.GetType("UnityEditor.IMGUI.Controls.ITreeViewDataSource");
            _getRowsMethod = iTreeViewDataSourceType.GetMethod("GetRows");
            _isExpandedMethod = iTreeViewDataSourceType.GetMethod("IsExpanded", new Type[] {typeof(TreeViewItem)});
            
            var gameObjectTreeViewItemType = assembly.GetType("UnityEditor.GameObjectTreeViewItem");
            _selectedIconProperty = gameObjectTreeViewItemType.GetProperty("selectedIcon", BindingAll);
            _objectPPTRProperty = gameObjectTreeViewItemType.GetProperty("objectPPTR", BindingAll);

            _isInitialized = true;
        }

        private static void PrepareFrame()
        {
            InitIfNeeded();
            
            _hasProcessedFrame = false;
        }

        private static void RefreshFolderIcons(int instanceid, Rect selectionrect)
        {
            if (_hasProcessedFrame)
            {
                return;
            }
            
            _hasProcessedFrame = true;
            
            var windows = ((IEnumerable)_getAllSceneHierarchyWindowsMethod.Invoke(null, new object[0])).Cast<EditorWindow>().ToList();
            foreach (EditorWindow window in windows)
            {
                var sceneHierarchy = _sceneHierarchyProperty.GetValue(window);
                var treeView = _treeViewProperty.GetValue(sceneHierarchy);
                var data = _dataProperty.GetValue(treeView);

                IList<TreeViewItem> rows = (IList<TreeViewItem>) _getRowsMethod.Invoke(data, new object[0]);
                foreach (TreeViewItem item in rows)
                {
                    Object itemObject = (Object) _objectPPTRProperty.GetValue(item);
                    if (!itemObject || !Folder.folders.TryGetValue(itemObject.GetInstanceID(), out int colorIndex))
                    {
                        continue;
                    }

                    Texture2D open, closed;
                    
                    var count = _coloredFolderIcons.Length;
                    (open, closed) = coloredFolderIcons[Mathf.Clamp(colorIndex, 0, count) % count];
                    
                    var isExpanded = (bool) _isExpandedMethod.Invoke(data, new object[] {item});
                    
                    item.icon = isExpanded ? open : closed;
                    _selectedIconProperty.SetValue(item, isExpanded ? _openFolderSelectedTexture : _closedFolderSelectedTexture);
                }
            }
        }
    }
}
