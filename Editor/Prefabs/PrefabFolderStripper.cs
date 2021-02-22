namespace UnityHierarchyFolders.Editor.Prefabs
{
    using System.IO;
    using Runtime;
    using UnityEditor;

    [InitializeOnLoad]
    public static class PrefabFolderStripper
    {
        private static (string path, string assetContent)[] _changedPrefabs;

        static PrefabFolderStripper()
        {
            EditorApplication.playModeStateChanged += HandlePrefabs;
        }

        private static void HandlePrefabs(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                StripFolders();
            else if (state == PlayModeStateChange.EnteredEditMode)
                RevertChanges();
        }

        private static void StripFolders()
        {
            var prefabGUIDs = AssetDatabase.FindAssets($"l: {LabelHandler.FolderPrefabLabel}");
            _changedPrefabs = new (string, string)[prefabGUIDs.Length];

            for (int i = 0; i < prefabGUIDs.Length; i++)
            {
                string guid = prefabGUIDs[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefabRoot = PrefabUtility.LoadPrefabContents(path);

                _changedPrefabs[i] = (path, File.ReadAllText(path));

                var folders = prefabRoot.GetComponentsInChildren<Folder>();

                foreach (Folder folder in folders)
                {
                    folder.Flatten(StripSettings.PlayMode, StripSettings.CapitalizeName);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
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