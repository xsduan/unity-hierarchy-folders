namespace UnityHierarchyFolders.Editor.Prefabs
{
    using System.IO;
    using Runtime;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;

    [InitializeOnLoad]
    public class PrefabFolderStripper : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static (string path, string assetContent)[] _changedPrefabs;

        static PrefabFolderStripper()
        {
            EditorApplication.playModeStateChanged += HandlePrefabsOnPlayMode;
        }

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            StripFolders(StripSettings.Build);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            RevertChanges();
        }

        private static void HandlePrefabsOnPlayMode(PlayModeStateChange state)
        {
            if (StripSettings.PlayMode == StrippingMode.DoNothing)
                return;

            if (state == PlayModeStateChange.ExitingEditMode)
                StripFolders(StripSettings.PlayMode);
            else if (state == PlayModeStateChange.EnteredEditMode)
                RevertChanges();
        }

        private static void StripFolders(StrippingMode strippingMode)
        {
            var prefabGUIDs = AssetDatabase.FindAssets($"l: {LabelHandler.FolderPrefabLabel}");
            _changedPrefabs = new (string, string)[prefabGUIDs.Length];

            for (int i = 0; i < prefabGUIDs.Length; i++)
            {
                string guid = prefabGUIDs[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);

                using (var temp = new PrefabUtility.EditPrefabContentsScope(path))
                {
                    _changedPrefabs[i] = (path, File.ReadAllText(path));

                    var folders = temp.prefabContentsRoot.GetComponentsInChildren<Folder>();

                    foreach (Folder folder in folders)
                    {
                        folder.Flatten(strippingMode, StripSettings.CapitalizeName);
                    }
                }
            }
        }

        private static void RevertChanges()
        {
            foreach ((string path, string content) in _changedPrefabs)
            {
                File.WriteAllText(path, content);
            }
        }
    }
}