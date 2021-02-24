namespace UnityHierarchyFolders.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// A singleton that contains info about edited prefabs and persists changes between domain reloads.
    /// </summary>
    [Serializable]
    internal class ChangedPrefabs : IEnumerable<ValueTuple<string, string>>
    {
        private const string KeyName = nameof(ChangedPrefabs);

        [SerializeField] private string[] _guids;
        [SerializeField] private string[] _contents;

        private static ChangedPrefabs _instance;

        public static ChangedPrefabs Instance
        {
            get
            {
                // _instance is null only in PrefabFolderStripper.RevertChanges() when Instance is called for the first time.
                // If _instance is null at that point, it means the domain reloaded, so the instance must be retrieved from PlayerPrefs.
                // In all other cases, _instance is created with help of Initialize before operating on it, so FromDeserialized won't be called.
                if (_instance == null)
                {
                    _instance = FromDeserialized();
                }

                return _instance;
            }
        }

        public (string guid, string content) this[int index]
        {
            get => (_guids[index], _contents[index]);
            set
            {
                _guids[index] = value.guid;
                _contents[index] = value.content;
            }
        }

        public static void Initialize(int length)
        {
            _instance = new ChangedPrefabs
            {
                _guids = new string[length],
                _contents = new string[length]
            };
        }

        public static void SerializeIfNeeded()
        {
            // Serialization is only needed if prefabs are edited before entering play mode and the domain will reload.
            // In all other cases, changes to prefabs will be reverted before a domain reload.
#if UNITY_2019_3_OR_NEWER
            if (EditorSettings.enterPlayModeOptionsEnabled && EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload))
                return;
#endif

            string serializedObject = EditorJsonUtility.ToJson(Instance);
            PlayerPrefs.SetString(KeyName, serializedObject);
        }

        private static ChangedPrefabs FromDeserialized()
        {
            string serializedObject = PlayerPrefs.GetString(KeyName);
            PlayerPrefs.DeleteKey(KeyName);
            var instance = new ChangedPrefabs();
            EditorJsonUtility.FromJsonOverwrite(serializedObject, instance);
            return instance;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<(string, string)> IEnumerable<ValueTuple<string, string>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<ValueTuple<string, string>>
        {
            private readonly ChangedPrefabs _instance;
            private int _index;

            public Enumerator(ChangedPrefabs instance)
            {
                _instance = instance;
                _index = -1;
            }

            public bool MoveNext()
            {
                return ++_index < Instance._guids.Length;
            }

            public void Reset() => _index = 0;

            public (string, string) Current => _instance[_index];

            object IEnumerator.Current => Current;

            public void Dispose() { }
        }
    }
}