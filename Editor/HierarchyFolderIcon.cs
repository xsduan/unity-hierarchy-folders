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

namespace Plugins.UnityHierarchyFolders {
    public class HierarchyFolderIcon
    {
        private static Texture2D openFolderTexture;
        private static Texture2D closedFolderTexture;
        private static Texture2D openFolderSelectedTexture;
        private static Texture2D closedFolderSelectedTexture;
        
        private static bool isInitialized;
        private static bool hasProcessedFrame; 
        
        // Reflected members
        private static PropertyInfo prop_sceneHierarchy;
        private static PropertyInfo prop_treeView;
        private static PropertyInfo prop_data;
        private static PropertyInfo prop_selectedIcon;
        private static PropertyInfo prop_objectPPTR;

        private static MethodInfo meth_getRows;
        private static MethodInfo meth_isExpanded;
        private static MethodInfo meth_getAllSceneHierarchyWindows;

        [InitializeOnLoadMethod] 
        static void Startup()
        { 
            Initialize();
            EditorApplication.update += ResetFolderIcons;
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
                c = Color.HSVToRGB(0, 0, 1);
                c.a = a;
 
                data[index] = c;
            }

            var mipmapSize = tinted.width * tinted.height * 4;
            var offset = 0; 
            for (int index = 0; index < tinted.mipmapCount; index++)
            {
                tinted.SetPixelData(data, index, offset);
                mipmapSize >>= 2;
                offset += mipmapSize;
            }
            tinted.Apply();
            
            return tinted;
        }

        private static void Initialize()
        {
            if (isInitialized) { return; }

            closedFolderTexture = (Texture2D) EditorGUIUtility.IconContent("Folder Icon").image;
            openFolderTexture = (Texture2D) EditorGUIUtility.IconContent("FolderEmpty Icon").image;
 
            openFolderSelectedTexture = GetTintedTexture(openFolderTexture);
            closedFolderSelectedTexture = GetTintedTexture(closedFolderTexture);
            
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
            Initialize();
            hasProcessedFrame = false;
        }

        private static void RefreshFolderIcons(int instanceid, Rect selectionrect)
        {
            if (hasProcessedFrame) { return; }
            hasProcessedFrame = true;
            
            var windows = ((IEnumerable)meth_getAllSceneHierarchyWindows.Invoke(null, Array.Empty<object>())).Cast<EditorWindow>().ToList();
            foreach (EditorWindow h in windows)
            {
                var sceneHierarchy = prop_sceneHierarchy.GetValue(h);
                var treeView = prop_treeView.GetValue(sceneHierarchy);
                var data = prop_data.GetValue(treeView);

                var rows = (IList<TreeViewItem>) meth_getRows.Invoke(data, Array.Empty<object>());
                foreach (TreeViewItem item in rows)
                {
                    var itemObject = (Object) prop_objectPPTR.GetValue(item);
                    if (!itemObject || !Folder.IsFolder(itemObject)) { continue; }

                    var isExpanded = (bool) meth_isExpanded.Invoke(data, new object[] { item });
                    
                    item.icon = isExpanded ? openFolderTexture : closedFolderTexture;
                    prop_selectedIcon.SetValue(item, isExpanded ? openFolderSelectedTexture : closedFolderSelectedTexture);
                }
            }
        }
    }
}
