using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityHierarchyFolders.Runtime;

namespace UnityHierarchyFolders.Editor
{
    public static class FolderEditorUtils
    {
        const string actionName = "Create Heirarchy Folder";

        /// <summary>
        /// Flatten every folder in scene after scene has been processed.
        /// </summary>
        [PostProcessScene]
        public static void Flatten()
        {
            foreach (var folder in UnityEngine.Object.FindObjectsOfType<Folder>())
            {
                folder.Flatten();
            }
        }

        /// <summary>
        /// Add new folder "prefab".
        /// </summary>
        /// <param name="command">Menu command information.</param>
        [MenuItem("GameObject/" + actionName, false, 0)]
        public static void AddFolderPrefab(MenuCommand command)
        {
            var obj = new GameObject {name = "Folder", tag = "EditorOnly"};
            obj.AddComponent<Folder>();

            GameObjectUtility.SetParentAndAlign(obj, (GameObject) command.context);
            Undo.RegisterCreatedObjectUndo(obj, actionName);
        }
    }
}