using System;
using System.Collections;
using System.Collections.Generic;
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
using Object = UnityEngine.Object;

namespace Plugins.UnityHierarchyFolders
{
    public class HierarchyFolderIcon
    {
        private static Texture2D _openFolderTexture;
        private static Texture2D _closedFolderTexture;
        private static Texture2D _openFolderSelectedTexture;
        private static Texture2D _closedFolderSelectedTexture;
        
        private static bool _isInitialized;
        private static bool _hasProcessedFrame; 
        
        // Reflected members
        private static PropertyInfo _sceneHierarchyProperty;
        private static PropertyInfo _treeViewProperty;
        private static PropertyInfo _dataProperty;
        private static MethodInfo _getRowsMethod;
        private static MethodInfo _isExpandedMethod;
        private static PropertyInfo _selectedIconProperty;
        private static PropertyInfo _objectPPTRProperty;
        private static MethodInfo _getAllSceneHierarchyWindowsMethod;


        [InitializeOnLoadMethod] 
        static void Startup()
        { 
            Initialize();
            EditorApplication.update += RefreshFolderIcons;
            EditorApplication.hierarchyWindowItemOnGUI += RefreshFolderIcons;
        }

        private static Texture2D GetTintedTexture(Texture2D original)
        {
            var tinted = new Texture2D(original.width, original.height, 
            original.graphicsFormat, original.mipmapCount, TextureCreationFlags.MipChain);
            
            Graphics.CopyTexture(original, tinted); 
            var data = tinted.GetRawTextureData<Color32>();
            for (int index = 0, len = data.Length; index < len; index++)
            {
                Color32 c = data[index];
                byte a = c.a;

                Color.RGBToHSV(c, out float h, out float s, out float v);
                c = Color.HSVToRGB(0, 0, 1);
                c.a = a;
 
                data[index] = c;
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

        private static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _closedFolderTexture = (Texture2D) EditorGUIUtility.IconContent("Folder Icon").image;
            _openFolderTexture = (Texture2D) EditorGUIUtility.IconContent("FolderEmpty Icon").image;
 
            _openFolderSelectedTexture = GetTintedTexture(_openFolderTexture);
            _closedFolderSelectedTexture = GetTintedTexture(_closedFolderTexture);
            
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

        private static void RefreshFolderIcons()
        {
            Initialize();
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
            foreach (EditorWindow h in windows)
            {
                var sceneHierarchy = _sceneHierarchyProperty.GetValue(h);
                var treeView = _treeViewProperty.GetValue(sceneHierarchy);
                var data = _dataProperty.GetValue(treeView);

                IList<TreeViewItem> rows = (IList<TreeViewItem>) _getRowsMethod.Invoke(data, new object[0]);
                foreach (TreeViewItem item in rows)
                {
                    Object itemObject = (Object) _objectPPTRProperty.GetValue(item);
                    if (!itemObject || !Folder.folders.Contains(itemObject.GetInstanceID()))
                    {
                        continue;
                    }

                    var isExpanded = (bool) _isExpandedMethod.Invoke(data, new object[] {item});
                    
                    item.icon = isExpanded ? _openFolderTexture : _closedFolderTexture;
                    _selectedIconProperty.SetValue(item, isExpanded ? _openFolderSelectedTexture : _closedFolderSelectedTexture);
                }
            }
        }
    }
}
