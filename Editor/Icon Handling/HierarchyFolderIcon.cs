#if UNITY_2019_1_OR_NEWER
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityHierarchyFolders.Runtime; // Folder.TryGetIconIndex
using Object = UnityEngine.Object;

namespace UnityHierarchyFolders.Editor
{
    public static class HierarchyFolderIcon
    {
#if UNITY_2020_1_OR_NEWER
        private const string _openedFolderPrefix = "FolderOpened";
#else
        private const string _openedFolderPrefix = "OpenedFolder";
#endif
        private const string _closedFolderPrefix = "Folder";

        // ===== Palette 1D =====
        // Indexing convention used here:
        // - storedIndex == 0  => default (no color)
        // - storedIndex 1..N  => colors[storedIndex - 1]
        public static readonly Color[] IconColors =
        {
            new Color(0.91f, 0.91f, 0.91f), // Gray
            new Color(0.98f, 0.80f, 0.27f), // Yellow
            new Color(0.50f, 0.79f, 0.98f), // Blue
            new Color(0.63f, 0.90f, 0.68f), // Green
            new Color(0.96f, 0.62f, 0.62f), // Red
        };
        public static int IconCount => IconColors.Length;

        // ===== State =====
        private static Texture2D _openFolderTexture;
        private static Texture2D _closedFolderTexture;
        private static Texture2D _openFolderSelectedTexture;
        private static Texture2D _closedFolderSelectedTexture;

        // Pre-tinted variants (length == IconCount)
        static Texture2D[] _openVariants;
        static Texture2D[] _closedVariants;

        private static bool _isInitialized;
        private static bool _hasProcessedFrame = true;

        // Reflected members we actually need
        private static MethodInfo meth_getAllSceneHierarchyWindows;
        private static PropertyInfo prop_sceneHierarchy;

        // Common flags
        const BindingFlags F =
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.FlattenHierarchy;

        // ===== Helpers =====
        static PropertyInfo GetPropertyExact(Type type, string name, BindingFlags flags, Type returnType)
        {
            var pi = type.GetProperty(name, flags, null, returnType, Type.EmptyTypes, null);
            if (pi != null) return pi;

            return type.GetProperties(flags)
                       .FirstOrDefault(p => p.Name == name &&
                                            p.PropertyType == returnType &&
                                            p.GetIndexParameters().Length == 0);
        }

        static PropertyInfo GetPropertyByName(Type t, string name, BindingFlags flags)
        {
            return t.GetProperties(flags)
                    .FirstOrDefault(p => p.Name == name && p.GetIndexParameters().Length == 0);
        }

        // Cheap CPU tint; builds readable textures once
        static Texture2D Tint(Texture2D src, Color tint)
        {
            if (src == null) return null;
            var w = src.width; var h = src.height;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = src.filterMode,
                wrapMode = src.wrapMode
            };
            // Copy pixels
            var tmp = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, tmp);
            var prev = RenderTexture.active;
            RenderTexture.active = tmp;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply(false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(tmp);

            var px = tex.GetPixels32();
            // multiply RGB by tint, keep original alpha
            byte tr = (byte)Mathf.RoundToInt(Mathf.Clamp01(tint.r) * 255f);
            byte tg = (byte)Mathf.RoundToInt(Mathf.Clamp01(tint.g) * 255f);
            byte tb = (byte)Mathf.RoundToInt(Mathf.Clamp01(tint.b) * 255f);
            for (int i = 0; i < px.Length; i++)
            {
                var p = px[i];
                px[i] = new Color32(
                    (byte)(p.r * tr / 255),
                    (byte)(p.g * tg / 255),
                    (byte)(p.b * tb / 255),
                    p.a);
            }
            tex.SetPixels32(px);
            tex.Apply(false);
            tex.name = src.name + "_tinted";
            return tex;
        }

        static void EnsureVariantsBuilt()
        {
            if (_openVariants != null && _closedVariants != null) return;
            if (_openFolderTexture == null || _closedFolderTexture == null) return;

            int n = IconCount;
            _openVariants = new Texture2D[n];
            _closedVariants = new Texture2D[n];

            for (int i = 0; i < n; i++)
            {
                var c = IconColors[i];
                _openVariants[i] = Tint(_openFolderTexture, c);
                _closedVariants[i] = Tint(_closedFolderTexture, c);
            }
        }

        // Map stored index to textures (0 = default, 1..N = colored)
        static void GetIcons(int storedIndex, out Texture2D openTex, out Texture2D closedTex)
        {
            openTex = _openFolderTexture;
            closedTex = _closedFolderTexture;

            EnsureVariantsBuilt();
            if (_openVariants == null || _closedVariants == null) return;

            if (storedIndex <= 0) return;          // default
            int i = Mathf.Clamp(storedIndex - 1, 0, IconCount - 1);
            if (_openVariants[i] != null) openTex = _openVariants[i];
            if (_closedVariants[i] != null) closedTex = _closedVariants[i];
        }

        // ===== Entry points =====
        [InitializeOnLoadMethod]
        private static void Startup()
        {
            EditorApplication.update += ResetFolderIcons;
            EditorApplication.hierarchyWindowItemOnGUI += RefreshFolderIcons;
        }

        public static void ForceRepaint()
        {
            InitIfNeeded();
            EditorApplication.RepaintHierarchyWindow();
        }

        static void InitIfNeeded()
        {
            if (_isInitialized) return;

            try
            {
                var unityEditorAsm = typeof(UnityEditor.Editor).Assembly;
                var type_sceneHierarchyWindow = unityEditorAsm.GetType("UnityEditor.SceneHierarchyWindow");
                if (type_sceneHierarchyWindow == null)
                {
                    _isInitialized = true;
                    return;
                }

                // Exact 0-arg overload
                meth_getAllSceneHierarchyWindows =
                    type_sceneHierarchyWindow.GetMethod("GetAllSceneHierarchyWindows", F, null, Type.EmptyTypes, null)
                    ?? type_sceneHierarchyWindow.GetMethods(F).FirstOrDefault(m => m.Name == "GetAllSceneHierarchyWindows" && m.GetParameters().Length == 0);

                var type_sceneHierarchy =
                    unityEditorAsm.GetType("UnityEditor.SceneHierarchy")
                    ?? unityEditorAsm.GetType("UnityEditor.IMGUI.Controls.SceneHierarchy");

                prop_sceneHierarchy = (type_sceneHierarchy != null)
                    ? GetPropertyExact(type_sceneHierarchyWindow, "sceneHierarchy", F, type_sceneHierarchy)
                    : GetPropertyByName(type_sceneHierarchyWindow, "sceneHierarchy", F);

                if (meth_getAllSceneHierarchyWindows == null || prop_sceneHierarchy == null)
                {
                    _isInitialized = true;
                    return;
                }

                _isInitialized = true;
            }
            catch
            {
                _isInitialized = true;
            }
        }

        private static void ResetFolderIcons()
        {
            InitIfNeeded();

            if (_openFolderTexture == null || _closedFolderTexture == null)
            {
#if UNITY_2020_1_OR_NEWER
                _openFolderTexture = EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D;
#else
                _openFolderTexture   = EditorGUIUtility.IconContent("OpenedFolder Icon").image as Texture2D;
#endif
                _closedFolderTexture = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;

                _openFolderSelectedTexture = _openFolderTexture;
                _closedFolderSelectedTexture = _closedFolderTexture;

                // Rebuild variants next time GetIcons is called
                _openVariants = _closedVariants = null;
            }

        }

        private static void RefreshFolderIcons(int instanceid, Rect selectionrect)
        {

            if (meth_getAllSceneHierarchyWindows == null || prop_sceneHierarchy == null)
                return;

            try
            {
                var windowsObj = meth_getAllSceneHierarchyWindows.Invoke(null, Array.Empty<object>());
                if (windowsObj is not IEnumerable windows) return;

                foreach (var w in windows)
                {
                    if (w is not EditorWindow window) continue;

                    var sceneHierarchy = prop_sceneHierarchy.GetValue(window);
                    if (sceneHierarchy == null) continue;

                    // Resolve from instances (generic-safe)
                    var shType = sceneHierarchy.GetType();
                    var piTree = GetPropertyByName(shType, "treeView", F);
                    var treeView = piTree?.GetValue(sceneHierarchy);
                    if (treeView == null) continue;

                    var tvType = treeView.GetType();
                    var piData = GetPropertyByName(tvType, "data", F);
                    var data = piData?.GetValue(treeView);
                    if (data == null) continue;

                    var dataType = data.GetType();
                    var miGetRows = dataType.GetMethod("GetRows", F, null, Type.EmptyTypes, null)
                                 ?? dataType.GetMethods(F).FirstOrDefault(m => m.Name == "GetRows" && m.GetParameters().Length == 0);
                    if (miGetRows == null) continue;

                    var miIsExpanded = dataType.GetMethods(F).FirstOrDefault(m => m.Name == "IsExpanded" && m.GetParameters().Length == 1);

                    var rowsObj = miGetRows.Invoke(data, Array.Empty<object>()) as IEnumerable;
                    if (rowsObj == null) continue;

                    foreach (var item in rowsObj)
                    {
                        if (item == null) continue;
                        var itemType = item.GetType();

                        var piObject = GetPropertyExact(itemType, "objectPPTR", F, typeof(Object))
                                    ?? GetPropertyByName(itemType, "objectPPTR", F);
                        var itemObject = (Object)piObject?.GetValue(item);
                        if (itemObject == null) continue;

                        if (!Folder.TryGetIconIndex(itemObject, out int colorIndex))
                            continue; // no colored icon requested

                        bool isExpanded = false;
                        if (miIsExpanded != null)
                        {
                            try { isExpanded = (bool)miIsExpanded.Invoke(data, new object[] { item }); }
                            catch { isExpanded = false; }
                        }

                        GetIcons(colorIndex+1, out var openTex, out var closedTex);

                        var piIcon = GetPropertyExact(itemType, "icon", F, typeof(Texture2D))
                                  ?? GetPropertyByName(itemType, "icon", F);
                        if (piIcon != null)
                            piIcon.SetValue(item, isExpanded ? openTex : closedTex);

                        var piSelIcon = GetPropertyExact(itemType, "selectedIcon", F, typeof(Texture2D))
                                      ?? GetPropertyByName(itemType, "selectedIcon", F);
                        if (piSelIcon != null)
                            piSelIcon.SetValue(item, isExpanded ? _openFolderSelectedTexture : _closedFolderSelectedTexture);
                    }
                }
            }
            catch
            {
                // swallow to avoid IMGUI clip imbalance
            }
        }
    }
}
#endif
