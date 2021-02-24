namespace UnityHierarchyFolders.Editor
{
    using System;
    using UnityEditor;

    internal class AssetImportGrouper : IDisposable
    {
        private static AssetImportGrouper _instance;

        private AssetImportGrouper() { }

        public static AssetImportGrouper Init()
        {
            AssetDatabase.StartAssetEditing();

            if (_instance == null)
                _instance = new AssetImportGrouper();

            return _instance;
        }

        public void Dispose()
        {
            AssetDatabase.StopAssetEditing();
        }
    }
}