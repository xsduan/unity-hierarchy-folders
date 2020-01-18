using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityHierarchyFolders.Runtime;

namespace Plugins.UnityHierarchyFolders
{
    [InitializeOnLoad] 
    public class HierarchyFolderIcon
    {
        private static Texture _texture;
        private static Texture _openTexture;
        private static Texture2D _backTexture;
        private static List<int> _markedObjects;
        static HierarchyFolderIcon()
        {
            EditorApplication.update += UpdateCb;
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemCb;
        }

        private static void UpdateCb()
        {
            var go = Object.FindObjectsOfType(typeof(GameObject)) as GameObject[];
            _markedObjects = new List<int>();
            if (go == null)
                return;
            foreach (var g in go)
            {
                if (g.GetComponent<Folder>() != null)
                    _markedObjects.Add(g.GetInstanceID());
            }
        }
        
        private static void HierarchyItemCb(int instanceId, Rect selectionRect)
        {
            if (_markedObjects == null)
                return;
            if (!_markedObjects.Contains(instanceId)) return;
            
            var g = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (!g)
                return;
            if (!_backTexture || !_openTexture || !_texture)
            {
                _texture = EditorGUIUtility.IconContent("Folder Icon").image;
                _openTexture = EditorGUIUtility.IconContent("FolderEmpty Icon").image;
            
                _backTexture = new Texture2D(1,1);
                _backTexture.SetPixel(0,0, EditorGUIUtility.isProSkin
                    ? new Color32(56, 56, 56, 255)
                    : new Color32(194, 194, 194, 255));
                _backTexture.Apply();
            }
            
            GUI.DrawTexture(new Rect(selectionRect)
            {
                y = selectionRect.y + 1,
                width = 15,
                height = 15
            }, _backTexture);
            GUI.DrawTexture(new Rect(selectionRect)
            {
                width = selectionRect.height,
            },g.transform.childCount == 0? _openTexture : _texture);
        }
    }
}
