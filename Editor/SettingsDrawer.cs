namespace UnityHierarchyFolders.Editor
{
    using System;
    using System.Collections.Generic;
    using Runtime;
    using UnityEditor;
    using UnityEngine;

    internal static class SettingsDrawer
    {
        private static readonly GUIContent _buildStrippingName = new GUIContent("Build Stripping Type");

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Preferences/Hierarchy Folders", SettingsScope.User)
            {
                guiHandler = searchContext =>
                {
                    StripSettings.PlayMode = (StrippingMode) EditorGUILayout.EnumPopup(
                        "Play Mode Stripping Type", StripSettings.PlayMode);

                    if (StripSettings.PlayMode == StrippingMode.ReplaceWithSeparator)
                    {
                        StripSettings.CapitalizeName =
                            EditorGUILayout.Toggle("Capitalize Folder Names", StripSettings.CapitalizeName);
                    }

                    StripSettings.Build = (StrippingMode) EditorGUILayout.EnumPopup(
                        _buildStrippingName, StripSettings.Build, TypeCanBeInBuild, true);
                },

                keywords = new HashSet<string>(new[] { "Play", "Mode", "Build", "Stripping", "Type", "Capitalize", "Folder", "Names" })
            };

            return provider;
        }

        private static bool TypeCanBeInBuild(Enum enumValue)
        {
            var mode = (StrippingMode) enumValue;
            return mode == StrippingMode.PrependWithFolderName || mode == StrippingMode.Delete;
        }
    }
}