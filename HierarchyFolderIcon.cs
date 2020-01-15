using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityHierarchyFolders.Runtime;

namespace Plugins.UnityHierarchyFolders
{
    [InitializeOnLoad]
    public class HierarchyFolderIcon
    {
        private static readonly Texture Texture;
        private static readonly Texture OpenTexture;
        private static readonly Texture2D BackTexture;
        private static List<int> _markedObjects;
        static HierarchyFolderIcon()
        {
            Texture = EditorGUIUtility.IconContent("Folder Icon").image;
            OpenTexture = EditorGUIUtility.IconContent("FolderEmpty Icon").image;
            
            BackTexture = new Texture2D(1,1);
            BackTexture.SetPixel(0,0, EditorGUIUtility.isProSkin
                ? new Color32(56, 56, 56, 255)
                : new Color32(194, 194, 194, 255));
            BackTexture.Apply();
            
            EditorApplication.update += UpdateCb;
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemCb;
        }

        private static void UpdateCb()
        {
            var go = Object.FindObjectsOfType(typeof(GameObject)) as GameObject[];
            _markedObjects = new List<int>();
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
            if (!BackTexture || !OpenTexture || !Texture)
                return;
            
            GUI.DrawTexture(new Rect(selectionRect)
            {
                y = selectionRect.y + 1,
                width = 15,
                height = 15
            }, BackTexture);
            GUI.DrawTexture(new Rect(selectionRect)
            {
                width = selectionRect.height,
            },g.transform.childCount == 0? OpenTexture : Texture);
        }
    }
}