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

        private static void Initialize()
        {
            _instance = new Settings(PackageName);

            _playModeSetting = new UserSetting<StrippingMode>(_instance, nameof(_playModeSetting),
                StrippingMode.Prepend, SettingsScope.User);

            _buildSetting = new UserSetting<StrippingMode>(_instance, nameof(_buildSetting),
                StrippingMode.Prepend, SettingsScope.User);
        }
    }
}