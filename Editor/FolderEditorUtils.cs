using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityHierarchyFolders.Runtime;

namespace UnityHierarchyFolders.Editor
{
    public static class FolderEditorUtils
    {
        private const string _actionName = "Create Heirarchy Folder %#&N";

        /// <summary>Add new folder "prefab".</summary>
        /// <param name="command">Menu command information.</param>
        [MenuItem("GameObject/" + _actionName, isValidateFunction: false, priority: 0)]
        public static void AddFolderPrefab(MenuCommand command)
        {
            var obj = new GameObject { name = "Folder" };
            obj.AddComponent<Folder>();

            GameObjectUtility.SetParentAndAlign(obj, (GameObject)command.context);
            Undo.RegisterCreatedObjectUndo(obj, _actionName);
        }
    }

    public class FolderOnBuild : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            foreach (var folder in Object.FindObjectsOfType<Folder>())
            {
                folder.Flatten();
            }
        }
    }
}
