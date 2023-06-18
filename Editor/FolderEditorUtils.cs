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
        private const string _actionName = "Create Hierarchy Folder %#&N";

        /// <summary>Add new folder "prefab".</summary>
        /// <param name="command">Menu command information.</param>
        [MenuItem("GameObject/" + _actionName, isValidateFunction: false, priority: 0)]
        public static void AddFolderPrefab(MenuCommand command)
        {
            var obj = new GameObject { name = "Folder" };
            obj.AddComponent<Folder>();

            GameObjectUtility.SetParentAndAlign(obj, (GameObject)command.context);
            Undo.RegisterCreatedObjectUndo(obj, _actionName);
            Selection.activeObject = obj;
        }
    }

    public class FolderOnBuild : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var strippingMode = report == null ? StripSettings.PlayMode : StripSettings.Build;

            foreach (var folder in Object.FindObjectsOfType<Folder>())
            {
                folder.Flatten(strippingMode, StripSettings.CapitalizeName);
            }
        }
    }
}
