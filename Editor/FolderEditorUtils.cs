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
        const string actionName = "Create Heirarchy Folder";

        /// <summary>Add new folder "prefab".</summary>
        /// <param name="command">Menu command information.</param>
        [MenuItem("GameObject/" + actionName, isValidateFunction: false, priority: 0)]
        public static void AddFolderPrefab(MenuCommand command)
        {
            var obj = new GameObject { name = "Folder" };
            obj.AddComponent<Folder>();

            GameObjectUtility.SetParentAndAlign(obj, (GameObject)command.context);
            Undo.RegisterCreatedObjectUndo(obj, actionName);
        }
    }

    public class FolderOnBuild : IProcessSceneWithReport
    {
        public int callbackOrder { get => 0; }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            foreach (var folder in Object.FindObjectsOfType<Folder>())
            {
                folder.Flatten();
            }
        }
    }
}
