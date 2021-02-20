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
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("Preferences/Hierarchy Folders", SettingsScope.User)
            {
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = searchContext =>
                {
                    StripSettings.PlayMode = (StrippingMode) EditorGUILayout.EnumPopup(
                        "Play Mode Stripping Type", StripSettings.PlayMode);

                    StripSettings.Build = (StrippingMode) EditorGUILayout.EnumPopup(
                        _buildStrippingName, StripSettings.Build, TypeCanBeInBuild, true);
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Play", "Mode", "Build", "Stripping", "Type" })
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