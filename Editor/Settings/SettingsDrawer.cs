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
            { nameof(StripSettings.CapitalizeName), "Capitalize Folder Names" }
        };

        private static readonly GUIContent _buildStrippingName = new GUIContent(_fieldNames[nameof(StripSettings.Build)]);

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Preferences/Hierarchy Folders", SettingsScope.User)
            {
                guiHandler = OnGUI,
                keywords = GetKeywords()
            };

            return provider;
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
    }
}