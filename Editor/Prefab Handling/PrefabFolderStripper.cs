namespace UnityHierarchyFolders.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using Runtime;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEngine;
    using Object = UnityEngine.Object;

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
            if (StripSettings.StripFoldersFromPrefabsInBuild)
                StripFoldersFromDependentPrefabs();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (StripSettings.StripFoldersFromPrefabsInBuild)
                RevertChanges();
        }

        private static void HandlePrefabsOnPlayMode(PlayModeStateChange state)
        {
            if ( ! StripSettings.StripFoldersFromPrefabsInPlayMode || StripSettings.PlayMode == StrippingMode.DoNothing)
                return;

            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Stripping folders from all prefabs in the project instead of only the ones referenced in the scenes
                // because a prefab can be hot-swapped in Play Mode.
                StripFoldersFromAllPrefabs();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                RevertChanges();
            }
        }

        private static void StripFoldersFromDependentPrefabs()
        {
            var scenePaths = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray();
            var dependentAssetsPaths = AssetDatabase.GetDependencies(scenePaths, true);

            var prefabsWithLabel = dependentAssetsPaths.Where(path =>
                    AssetDatabase.GetLabels(GetAssetForLabel(path)).Contains(LabelHandler.FolderPrefabLabel))
                .ToArray();

            _changedPrefabs = new (string, string)[prefabsWithLabel.Length];

            for (int i = 0; i < prefabsWithLabel.Length; i++)
            {
                string path = prefabsWithLabel[i];
                _changedPrefabs[i] = (path, File.ReadAllText(path));
                StripFoldersFromPrefab(path, StripSettings.Build);
            }
        }

        private static
#if UNITY_2020_1_OR_NEWER
            GUID
#else
            Object
#endif
            GetAssetForLabel(string path)
        {
#if UNITY_2020_1_OR_NEWER
            return AssetDatabase.GUIDFromAssetPath(path);
#else
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
#endif
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
            using (var temp = new EditPrefabContentsScope(prefabPath))
            {
                var folders = temp.PrefabContentsRoot.GetComponentsInChildren<Folder>();

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

        /// <summary>
        /// A copy of <see cref="PrefabUtility.EditPrefabContentsScope"/> for backwards compatibility with Unity 2019.
        /// </summary>
        private readonly struct EditPrefabContentsScope : IDisposable
        {
            public readonly GameObject PrefabContentsRoot;

            private readonly string _assetPath;

            public EditPrefabContentsScope(string assetPath)
            {
                PrefabContentsRoot = PrefabUtility.LoadPrefabContents(assetPath);
                _assetPath = assetPath;
            }

            public void Dispose()
            {
                PrefabUtility.SaveAsPrefabAsset(PrefabContentsRoot, _assetPath);
                PrefabUtility.UnloadPrefabContents(PrefabContentsRoot);
            }
        }
    }
}