namespace UnityHierarchyFolders.Editor
{
    using System.Linq;
    using Runtime;
    using UnityEditor;
    using UnityEngine;

    public class LabelHandler : AssetPostprocessor
    {
        public const string FolderPrefabLabel = "FolderUser";

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] _, string[] __, string[] ___)
        {
            // Group imports into one to improve performance in case there are multiple prefabs that need a label change.
            using (AssetImportGrouper.Init())
            {
                foreach (string assetPath in importedAssets)
                {
                    if (assetPath.EndsWith(".prefab"))
                        HandlePrefabLabels(assetPath);
                }
            }
        }

        private static void HandlePrefabLabels(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (asset.GetComponentsInChildren<Folder>().Length == 0)
            {
                RemoveFolderLabel(asset, assetPath);
            }
            else
            {
                AddFolderLabel(asset, assetPath);
            }
        }

        private static void RemoveFolderLabel(GameObject assetObject, string assetPath)
        {
            var labels = AssetDatabase.GetLabels(assetObject);

            if ( ! labels.Contains(FolderPrefabLabel))
                return;

            ArrayUtility.Remove(ref labels, FolderPrefabLabel);
            AssetDatabase.SetLabels(assetObject, labels);
            AssetDatabase.ImportAsset(assetPath);
        }

        private static void AddFolderLabel(GameObject assetObject, string assetPath)
        {
            var labels = AssetDatabase.GetLabels(assetObject);

            if (labels.Contains(FolderPrefabLabel))
                return;

            ArrayUtility.Add(ref labels, FolderPrefabLabel);
            AssetDatabase.SetLabels(assetObject, labels);
            AssetDatabase.ImportAsset(assetPath);
        }
    }
}