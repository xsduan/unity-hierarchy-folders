namespace UnityHierarchyFolders.Editor.Prefabs
{
    using System.Collections;
    using Runtime;
    using Unity.EditorCoroutines.Editor;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Assertions;

    public class LabelAdder : AssetModificationProcessor
    {
        private static readonly object _coroutineHolder = new object();
        private static readonly string[] _label = { "folderUser" };

        private static void OnWillCreateAsset(string assetPath)
        {
            if ( ! assetPath.EndsWith(".prefab"))
                return;

            EditorCoroutineUtility.StartCoroutine(SetLabelIfContainsFolder(assetPath), _coroutineHolder);
        }

        private static IEnumerator SetLabelIfContainsFolder(string assetPath)
        {
            // Skip one frame for the editor to create the asset.
            yield return null;

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Assert.IsNotNull(asset);

            if (asset.GetComponentsInChildren<Folder>().Length != 0)
                AssetDatabase.SetLabels(asset, _label);
        }
    }
}