namespace UnityHierarchyFolders.Editor.Prefabs
{
    using System.IO;
    using System.Linq;
    using Runtime;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEngine;

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
            StripFoldersFromDependentPrefabs();
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
                StripFoldersFromAllPrefabs();
            else if (state == PlayModeStateChange.EnteredEditMode)
                RevertChanges();
        }

        private static void StripFoldersFromDependentPrefabs()
        {
            var scenePaths = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray();
            var dependentAssetsPaths = AssetDatabase.GetDependencies(scenePaths, true);

            var prefabsWithLabel = dependentAssetsPaths.Where(path =>
                    AssetDatabase.GetLabels(AssetDatabase.GUIDFromAssetPath(path)).Contains(LabelHandler.FolderPrefabLabel))
                .ToArray();

            _changedPrefabs = new (string, string)[prefabsWithLabel.Length];

            for (int i = 0; i < prefabsWithLabel.Length; i++)
            {
                string path = prefabsWithLabel[i];
                _changedPrefabs[i] = (path, File.ReadAllText(path));
                StripFoldersFromPrefab(path, StripSettings.Build);
            }
        }

        private static void StripFoldersFromAllPrefabs()
        {
            var prefabGUIDs = AssetDatabase.FindAssets($"l: {LabelHandler.FolderPrefabLabel}");
            _changedPrefabs = new (string, string)[prefabGUIDs.Length];

            for (int i = 0; i < prefabGUIDs.Length; i++)
            {
                string guid = prefabGUIDs[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);

                _changedPrefabs[i] = (path, File.ReadAllText(path));
                StripFoldersFromPrefab(path, StripSettings.PlayMode);
            }
        }

        private static void StripFoldersFromPrefab(string prefabPath, StrippingMode strippingMode)
        {
            using (var temp = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var folders = temp.prefabContentsRoot.GetComponentsInChildren<Folder>();

                foreach (Folder folder in folders)
                {
                    folder.Flatten(strippingMode, StripSettings.CapitalizeName);
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