namespace UnityHierarchyFolders.Editor
{
    using System;
    using System.Collections.Generic;
    using Runtime;
    using UnityEditor;
    using UnityEngine;

    internal static class SettingsDrawer
    {
        /// <summary>
        /// Field names of corresponding settings. Each field name can be accessed by the name of the setting variable.
        /// </summary>
        private static readonly Dictionary<string, string> _fieldNames = new Dictionary<string, string>
        {
            { nameof(StripSettings.PlayMode), "Play Mode Stripping Type" },
            { nameof(StripSettings.Build), "Build Stripping Type" },
            { nameof(StripSettings.CapitalizeName), "Capitalize Folder Names" },
            { nameof(StripSettings.StripFoldersFromPrefabsInPlayMode), "Strip folders from prefabs in Play Mode" },
            { nameof(StripSettings.StripFoldersFromPrefabsInBuild), "Strip folders from prefabs in build" },
        };

        private static readonly GUIContent _buildStrippingName = new GUIContent(_fieldNames[nameof(StripSettings.Build)]);

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Preferences/Hierarchy Folders", SettingsScope.User)
            {
                guiHandler = OnGUI,
                keywords = GetKeywords()
            };
        }

        private static void OnGUI(string searchContext)
        {
            StripSettings.PlayMode = (StrippingMode) EditorGUILayout.EnumPopup(
                _fieldNames[nameof(StripSettings.PlayMode)], StripSettings.PlayMode);

            if (StripSettings.PlayMode == StrippingMode.ReplaceWithSeparator)
            {
                StripSettings.CapitalizeName = EditorGUILayout.Toggle(
                    _fieldNames[nameof(StripSettings.CapitalizeName)], StripSettings.CapitalizeName);
            }

            StripSettings.Build = (StrippingMode) EditorGUILayout.EnumPopup(
                _buildStrippingName, StripSettings.Build, TypeCanBeInBuild, true);

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

            if (StripSettings.StripFoldersFromPrefabsInPlayMode)
            {
                EditorGUILayout.HelpBox(
                    "If you notice that entering play mode takes too long, you can try disabling this option. " +
                    "Folders will not be stripped from prefabs that are instantiated at runtime, but if performance in " +
                    "Play Mode does not matter, you will be fine.", MessageType.Info);
            }

            using (new TemporaryLabelWidth(230f))
            {
                StripSettings.StripFoldersFromPrefabsInPlayMode =
                    EditorGUILayout.Toggle(_fieldNames[nameof(StripSettings.StripFoldersFromPrefabsInPlayMode)], StripSettings.StripFoldersFromPrefabsInPlayMode);

                StripSettings.StripFoldersFromPrefabsInBuild =
                    EditorGUILayout.Toggle(_fieldNames[nameof(StripSettings.StripFoldersFromPrefabsInBuild)], StripSettings.StripFoldersFromPrefabsInBuild);
            }
        }

        private static HashSet<string> GetKeywords()
        {
            var keywords = new HashSet<string>();

            foreach (string fieldName in _fieldNames.Values)
            {
                keywords.AddWords(fieldName);
            }

            return keywords;
        }

        private static void AddWords(this HashSet<string> set, string phrase)
        {
            foreach (string word in phrase.Split(' '))
            {
                set.Add(word);
            }
        }

        private static bool TypeCanBeInBuild(Enum enumValue)
        {
            var mode = (StrippingMode) enumValue;
            return mode == StrippingMode.PrependWithFolderName || mode == StrippingMode.Delete;
        }

        /// <summary>
        /// Temporarily sets <see cref="EditorGUIUtility.labelWidth"/> to a certain value, than reverts it.
        /// </summary>
        private readonly struct TemporaryLabelWidth : IDisposable
        {
            private readonly float _oldWidth;

            public TemporaryLabelWidth(float width)
            {
                _oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = width;
            }

            public void Dispose()
            {
                EditorGUIUtility.labelWidth = _oldWidth;
            }
        }
    }
}