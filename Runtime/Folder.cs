#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
#endif
using UnityEngine;

namespace UnityHierarchyFolders.Runtime
{
#if UNITY_EDITOR
    /// <summary>
    /// Editor-only helper for checking whether a component can be removed without
    /// breaking RequireComponent dependencies on the same GameObject.
    /// </summary>
    internal static class CanDestroyExtension
    {
        private static readonly List<Component> s_ComponentBuffer = new List<Component>(16);

        private static bool Requires(Type componentType, Type requiredType)
        {
            object[] attributes = componentType.GetCustomAttributes(typeof(RequireComponent), true);
            for (int i = 0; i < attributes.Length; i++)
            {
                var requireComponent = attributes[i] as RequireComponent;
                if (requireComponent == null)
                    continue;

                if (requireComponent.m_Type0 != null && requireComponent.m_Type0.IsAssignableFrom(requiredType))
                    return true;

                if (requireComponent.m_Type1 != null && requireComponent.m_Type1.IsAssignableFrom(requiredType))
                    return true;

                if (requireComponent.m_Type2 != null && requireComponent.m_Type2.IsAssignableFrom(requiredType))
                    return true;
            }

            return false;
        }

        internal static bool CanDestroy(this Component component)
        {
            if (component == null)
                return false;

            GameObject go = component.gameObject;
            go.GetComponents(s_ComponentBuffer);

            Type requiredType = component.GetType();
            for (int i = 0; i < s_ComponentBuffer.Count; i++)
            {
                Component existing = s_ComponentBuffer[i];
                if (existing == null || existing == component)
                    continue;

                if (Requires(existing.GetType(), requiredType))
                    return false;
            }

            return true;
        }
    }
#endif

    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class Folder : MonoBehaviour
    {
#if UNITY_EDITOR
        private static bool s_AddedSelectionResetCallback;
        private static Tool s_LastTool;
        private static Folder s_ToolLock;
        private static readonly List<Component> s_ComponentBuffer = new List<Component>(8);
        private static readonly List<Component> s_DestroyBuffer = new List<Component>(8);

#if UNITY_6000_5_OR_NEWER
        private static readonly Dictionary<EntityId, int> s_Folders = new Dictionary<EntityId, int>();
        private static readonly Dictionary<int, UnityEngine.Object> s_HierarchyObjectCache = new Dictionary<int, UnityEngine.Object>();
        private static readonly object[] s_InstanceIdToObjectArguments = new object[1];
        private static MethodInfo s_InstanceIdToObjectMethod;
#else
        private static readonly Dictionary<int, int> s_Folders = new Dictionary<int, int>();
#endif

        [SerializeField]
        private int _colorIndex = 0;

        public int ColorIndex => _colorIndex;

        private void OnEnable()
        {
            if (!s_AddedSelectionResetCallback)
            {
                Selection.selectionChanged += ResetHiddenToolState;
                s_AddedSelectionResetCallback = true;
            }

            Selection.selectionChanged -= HandleSelection;
            Selection.selectionChanged += HandleSelection;

            if (transform != null)
                transform.hideFlags = HideFlags.HideInInspector;

            AddFolderData();
            QueueExclusiveComponentCheck();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= HandleSelection;

            if (s_ToolLock == this)
            {
                Tools.current = s_LastTool;
                s_ToolLock = null;
            }

            RemoveFolderData();
        }

        private void OnValidate()
        {
            AddFolderData();
            QueueExclusiveComponentCheck();
        }

        private void OnDestroy()
        {
            Selection.selectionChanged -= HandleSelection;
            RemoveFolderData();
        }

        /// <summary>
        /// Gets the folder icon color index for a Unity object. Unity 6.5+ uses EntityId
        /// so this path avoids Object.GetInstanceID() in new editor versions.
        /// </summary>
        public static bool TryGetIconIndex(UnityEngine.Object obj, out int index)
        {
            index = -1;
            if (obj == null)
                return false;

            return s_Folders.TryGetValue(GetObjectKey(obj), out index);
        }

        /// <summary>
        /// Hierarchy callbacks still provide an int id. Resolve it back to an object,
        /// then use the Object/EntityId path above.
        /// </summary>
        public static bool TryGetIconIndex(int hierarchyInstanceId, out int index)
        {
#if UNITY_6000_5_OR_NEWER
            UnityEngine.Object obj = ResolveHierarchyObject(hierarchyInstanceId);
#else
            UnityEngine.Object obj = EditorUtility.InstanceIDToObject(hierarchyInstanceId);
#endif
            return TryGetIconIndex(obj, out index);
        }

        public static bool IsFolder(UnityEngine.Object obj) => TryGetIconIndex(obj, out _);
        public static bool IsFolder(int hierarchyInstanceId) => TryGetIconIndex(hierarchyInstanceId, out _);

#if UNITY_6000_5_OR_NEWER
        private static EntityId GetObjectKey(UnityEngine.Object obj) => obj.GetEntityId();

        private static UnityEngine.Object ResolveHierarchyObject(int hierarchyInstanceId)
        {
            if (s_HierarchyObjectCache.TryGetValue(hierarchyInstanceId, out UnityEngine.Object cachedObject) && cachedObject != null)
                return cachedObject;

            // Unity 6.5's public conversion API uses EntityId. The hierarchy callback is
            // still int-based in current editor callbacks, so first try the raw EntityId path.
            EntityId entityId = EntityId.FromULong(unchecked((ulong)(uint)hierarchyInstanceId));
            UnityEngine.Object obj = EditorUtility.EntityIdToObject(entityId);

            // Fallback for int-based hierarchy callbacks while avoiding direct compile-time
            // usage of the obsolete EditorUtility.InstanceIDToObject API.
            if (obj == null)
                obj = InstanceIdToObjectReflective(hierarchyInstanceId);

            if (obj != null)
                s_HierarchyObjectCache[hierarchyInstanceId] = obj;

            return obj;
        }

        private static UnityEngine.Object InstanceIdToObjectReflective(int instanceId)
        {
            if (s_InstanceIdToObjectMethod == null)
            {
                s_InstanceIdToObjectMethod = typeof(EditorUtility).GetMethod(
                    "InstanceIDToObject",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(int) },
                    null);
            }

            if (s_InstanceIdToObjectMethod == null)
                return null;

            s_InstanceIdToObjectArguments[0] = instanceId;
            return s_InstanceIdToObjectMethod.Invoke(null, s_InstanceIdToObjectArguments) as UnityEngine.Object;
        }
#else
        private static int GetObjectKey(UnityEngine.Object obj) => obj.GetInstanceID();
#endif

        private void AddFolderData()
        {
            if (this == null || gameObject == null)
                return;

            int colorIndex = Mathf.Max(0, _colorIndex);
            var key = GetObjectKey(gameObject);

            bool changed = !s_Folders.TryGetValue(key, out int oldColorIndex) || oldColorIndex != colorIndex;
            s_Folders[key] = colorIndex;

            if (changed)
                EditorApplication.RepaintHierarchyWindow();
        }

        private void RemoveFolderData()
        {
            if (gameObject == null)
                return;

            if (s_Folders.Remove(GetObjectKey(gameObject)))
                EditorApplication.RepaintHierarchyWindow();
        }

        private static void ResetHiddenToolState()
        {
            Tools.hidden = false;
        }

        private void HandleSelection()
        {
            if (s_ToolLock != null && s_ToolLock != this)
                return;

            if (this != null && Selection.Contains(gameObject))
            {
                s_LastTool = Tools.current;
                s_ToolLock = this;
                Tools.current = Tool.None;
            }
            else if (s_ToolLock != null)
            {
                Tools.current = s_LastTool;
                s_ToolLock = null;
            }
        }

        private bool AskDelete() => EditorUtility.DisplayDialog(
            title: "Can't add script",
            message: "Folders shouldn't be used with other components. Which component should be kept?",
            ok: "Folder",
            cancel: "Component"
        );

        private void DeleteComponents(List<Component> components)
        {
            bool destroyedAny;
            do
            {
                destroyedAny = false;
                s_DestroyBuffer.Clear();

                for (int i = 0; i < components.Count; i++)
                {
                    Component component = components[i];
                    if (component != null && component.CanDestroy())
                        s_DestroyBuffer.Add(component);
                }

                for (int i = 0; i < s_DestroyBuffer.Count; i++)
                {
                    Component component = s_DestroyBuffer[i];
                    if (component == null)
                        continue;

                    DestroyImmediate(component);
                    destroyedAny = true;
                }

                if (destroyedAny && gameObject != null)
                    gameObject.GetComponents(s_ComponentBuffer);
            }
            while (destroyedAny);
        }

        private void QueueExclusiveComponentCheck()
        {
            if (Application.isPlaying || this == null)
                return;

            EditorApplication.delayCall -= EnsureExclusiveComponent;
            EditorApplication.delayCall += EnsureExclusiveComponent;
        }

        private void EnsureExclusiveComponent()
        {
            if (Application.isPlaying || this == null || gameObject == null)
                return;

            gameObject.GetComponents(s_ComponentBuffer);

            bool hasExtraComponent = false;
            for (int i = 0; i < s_ComponentBuffer.Count; i++)
            {
                Component component = s_ComponentBuffer[i];
                if (component == null || component == this || component is Transform)
                    continue;

                hasExtraComponent = true;
                break;
            }

            if (!hasExtraComponent)
                return;

            if (AskDelete())
                DeleteComponents(s_ComponentBuffer);
            else
                DestroyImmediate(this);
        }
#endif

        /// <summary>
        /// Resets the transform properties to their identities: (0,0,0), identity rotation and unit scale.
        /// </summary>
        private void Update()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

#if UNITY_EDITOR
            if (!Application.IsPlaying(gameObject))
                AddFolderData();
#endif
        }

        /// <summary>Takes direct children and links them to the parent transform or global.</summary>
        /// <param name="strippingMode">Stripping mode to apply.</param>
        /// <param name="capitalizeFolderName">
        /// Whether to capitalize the folder name when replacing it with a separator.
        /// Applies only if <paramref name="strippingMode"/> is <see cref="StrippingMode.ReplaceWithSeparator"/>.
        /// </param>
        public void Flatten(StrippingMode strippingMode, bool capitalizeFolderName)
        {
            if (strippingMode == StrippingMode.DoNothing)
                return;

            MoveChildrenOut(strippingMode);
            HandleSelf(strippingMode, capitalizeFolderName);
        }

        private void MoveChildrenOut(StrippingMode strippingMode)
        {
            int index = transform.GetSiblingIndex();

            foreach (Transform child in GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (child.parent != transform)
                    continue;

                if (strippingMode == StrippingMode.PrependWithFolderName)
                    child.name = $"{name}/{child.name}";

                child.SetParent(transform.parent, true);
                child.SetSiblingIndex(++index);
            }
        }

        private void HandleSelf(StrippingMode strippingMode, bool capitalizeFolderName)
        {
            if (strippingMode == StrippingMode.ReplaceWithSeparator)
            {
                if (!name.StartsWith("--- "))
                    name = $"--- {(capitalizeFolderName ? name.ToUpper() : name)} ---";

                return;
            }

            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }
    }
}
