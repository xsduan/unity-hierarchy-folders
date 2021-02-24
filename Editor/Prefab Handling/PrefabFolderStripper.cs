namespace UnityHierarchyFolders.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using Runtime;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEditor.Callbacks;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [InitializeOnLoad]
    public class PrefabFolderStripper : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        static PrefabFolderStripper()
        {
            EditorApplication.playModeStateChanged += HandlePrefabsOnPlayMode;
        }

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (StripSettings.StripFoldersFromPrefabsInBuild)
            {
                using (AssetImportGrouper.Init())
                {
                    StripFoldersFromDependentPrefabs();
                }
            }
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

            // Calling it not in EnteredPlayMode because scripts may instantiate prefabs in Awake or OnEnable
            // which happens before EnteredPlayMode.
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Stripping folders from all prefabs in the project instead of only the ones referenced in the scenes
                // because a prefab may be hot-swapped in Play Mode.
                using (AssetImportGrouper.Init())
                {
                    StripFoldersFromAllPrefabs();
                }
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
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

            ChangedPrefabs.Initialize(prefabsWithLabel.Length);

            for (int i = 0; i < prefabsWithLabel.Length; i++)
            {
                string path = prefabsWithLabel[i];
                ChangedPrefabs.Instance[i] = (AssetDatabase.AssetPathToGUID(path), File.ReadAllText(path));
                StripFoldersFromPrefab(path, StripSettings.Build);
            }

            // Serialization of ChangedPrefabs is not needed here because domain doesn't reload before changes are reverted.
        }

        private static
#if UNITY_2020_1_OR_NEWER
            GUID
#else
            UnityEngine.Object
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
            ChangedPrefabs.Initialize(prefabGUIDs.Length);

            for (int i = 0; i < prefabGUIDs.Length; i++)
            {
                string guid = prefabGUIDs[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);

                ChangedPrefabs.Instance[i] = (guid, File.ReadAllText(path));
                StripFoldersFromPrefab(path, StripSettings.PlayMode);
            }

            // If domain reload is enabled in Play Mode Options, serialization of the changed prefabs is necessary
            // so that changes can be reverted after leaving play mode.
            ChangedPrefabs.SerializeIfNeeded();
        }

        private static void StripFoldersFromPrefab(string prefabPath, StrippingMode strippingMode)
        {
            using (var temp = new EditPrefabContentsScope(prefabPath))
            {
                var folders = temp.PrefabContentsRoot.GetComponentsInChildren<Folder>();

                foreach (Folder folder in folders)
                {
                    if (folder.gameObject == temp.PrefabContentsRoot)
                    {
                        Debug.LogWarning(
                            $"Hierarchy will not flatten for {prefabPath} because its root is a folder. " +
                            "It's advised to make the root an empty game object.");

                        Object.DestroyImmediate(folder);
                    }
                    else
                    {
                        folder.Flatten(strippingMode, StripSettings.CapitalizeName);
                    }
                }
            }
        }

        private static void RevertChanges()
        {
            foreach ((string guid, string content) in ChangedPrefabs.Instance)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // The asset might have been deleted in Play Mode. Additionally, event if the asset is deleted,
                // AssetDatabase might still hold a reference to it, so a File.Exists check is needed.
                if (string.IsNullOrEmpty(path) || ! File.Exists(path))
                    continue;

                File.WriteAllText(path, content);
            }

            AssetDatabase.Refresh();
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