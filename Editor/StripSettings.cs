namespace UnityHierarchyFolders.Editor
{
    using Runtime;
    using UnityEditor;
    using UnityEditor.SettingsManagement;

    internal static class StripSettings
    {
        private const string PackageName = "com.xsduan.hierarchy-folders";

        private static Settings _instance;
        private static UserSetting<StrippingMode> _playModeSetting;
        private static UserSetting<StrippingMode> _buildSetting;
        private static UserSetting<bool> _capitalizeName;

        public static StrippingMode PlayMode
        {
            get
            {
                if (_playModeSetting == null)
                {
                    Initialize();
                }

                return _playModeSetting.value;
            }

            set => _playModeSetting.value = value;
        }

        public static StrippingMode Build
        {
            get
            {
                if (_buildSetting == null)
                {
                    Initialize();
                }

                return _buildSetting.value;
            }

            set => _buildSetting.value = value;
        }

        public static bool CapitalizeName
        {
            get
            {
                if (_capitalizeName == null)
                {
                    Initialize();
                }

                return _capitalizeName.value;
            }

            set => _capitalizeName.value = value;
        }

        private static void Initialize()
        {
            _instance = new Settings(PackageName);

            _playModeSetting = new UserSetting<StrippingMode>(_instance, nameof(_playModeSetting),
                StrippingMode.PrependWithFolderName, SettingsScope.User);

            _buildSetting = new UserSetting<StrippingMode>(_instance, nameof(_buildSetting),
                StrippingMode.PrependWithFolderName, SettingsScope.User);

            _capitalizeName = new UserSetting<bool>(_instance, nameof(_capitalizeName), true, SettingsScope.User);
        }
    }
}