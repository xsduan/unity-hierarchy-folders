#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
#endif
using UnityEngine;

namespace UnityHierarchyFolders.Runtime
{
#if UNITY_EDITOR
    /// <summary>
    /// <para>Extension to Components to check if there are no dependencies to itself.</para>
    /// <para>
    ///     taken from:
    ///     <see cref="!:https://gamedev.stackexchange.com/a/140799">
    ///         StackOverflow: Check if a game object's component can be destroyed
    ///     </see>
    /// </para>
    /// </summary>
    internal static class CanDestroyExtension
    {
        private static bool Requires(Type obj, Type req) => Attribute.IsDefined(obj, typeof(RequireComponent)) &&
            Attribute.GetCustomAttributes(obj, typeof(RequireComponent))
                .OfType<RequireComponent>()
                // RequireComponent has up to 3 required types per requireComponent, because of course.
                .SelectMany(rc => new Type[] { rc.m_Type0, rc.m_Type1, rc.m_Type2 })
                .Any(t => t != null && t.IsAssignableFrom(req));

        /// <summary>Checks whether the stated component can be destroyed without violating dependencies.</summary>
        /// <returns>Is component destroyable?</returns>
        /// <param name="t">Component candidate for destruction.</param>
        internal static bool CanDestroy(this Component t) => !t.gameObject.GetComponents<Component>()
            .Any(c => Requires(c.GetType(), t.GetType()));
    }
#endif

    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class Folder : MonoBehaviour
    {
#if UNITY_EDITOR
        private static bool _addedSelectionResetCallback;

        private Folder()
        {
            // add reset callback first in queue
            if (!_addedSelectionResetCallback)
            {
                Selection.selectionChanged += () => Tools.hidden = false;
                _addedSelectionResetCallback = true;
            }

            Selection.selectionChanged += this.HandleSelection;
        }

        private static Tool _lastTool;
        private static Folder _toolLock;

        [SerializeField]
        private int _colorIndex = 0;
        public int ColorIndex => this._colorIndex;

        /// <summary>
        /// The set of folder objects.
        /// </summary>
        public static Dictionary<int, int> folders = new Dictionary<int, int>();

        /// <summary>
        /// Gets the icon index associated with the specified object.
        /// </summary>
        /// <param name="obj">Test object.</param>
        /// <param name="index">The icon index.</param>
        /// <returns>True if the specified object is a folder with a registered icon index.</returns>
        public static bool TryGetIconIndex(UnityEngine.Object obj, out int index)
        {
            index = -1;
            return obj && folders.TryGetValue(obj.GetInstanceID(), out index);
        }

        /// <summary>
        /// Test if a Unity object is a folder by way of containing a Folder component.
        /// </summary>
        /// <param name="obj">Test object.</param>
        /// <returns>Is this object a folder?</returns>
        public static bool IsFolder(UnityEngine.Object obj) => folders.ContainsKey(obj.GetInstanceID());

        private void Start() => this.AddFolderData();
        private void OnValidate() => this.AddFolderData();
        private void OnDestroy() => this.RemoveFolderData();

        private void AddFolderData() => folders[this.gameObject.GetInstanceID()] = this._colorIndex;
        private void RemoveFolderData() => folders.Remove(this.gameObject.GetInstanceID());

        /// <summary>Hides all gizmos if selected to avoid accidental editing of the transform.</summary>
        private void HandleSelection()
        {
            // ignore if another folder object is already hiding gizmo
            if (_toolLock != null && _toolLock != this) { return; }

            if (this != null && Selection.Contains(this.gameObject))
            {
                _lastTool = Tools.current;
                _toolLock = this;
                Tools.current = Tool.None;
            }
            else if (_toolLock != null)
            {
                Tools.current = _lastTool;
                _toolLock = null;
            }
        }

        private bool AskDelete() => EditorUtility.DisplayDialog(
            title: "Can't add script",
            message: "Folders shouldn't be used with other components. Which component should be kept?",
            ok: "Folder",
            cancel: "Component"
        );

        /// <summary>Delete all components regardless of dependency hierarchy.</summary>
        /// <param name="comps">Which components to delete.</param>
        private void DeleteComponents(IEnumerable<Component> comps)
        {
            var destroyable = comps.Where(c => c != null && c.CanDestroy());

            // keep cycling through the list of components until all components are gone.
            while (destroyable.Any())
            {
                foreach (var c in destroyable)
                {
                    DestroyImmediate(c);
                }
            }
        }

        /// <summary>Ensure that the folder is the only component.</summary>
        private void EnsureExclusiveComponent()
        {
            // we are running, don't bother the player.
            // also, sometimes `this` might be null for whatever reason.
            if (Application.isPlaying || this == null) { return; }

            var existingComponents = this.GetComponents<Component>()
                .Where(c => c != this && !typeof(Transform).IsAssignableFrom(c.GetType()));

            // no items means no actions anyways
            if (!existingComponents.Any()) { return; }

            if (this.AskDelete())
            {
                this.DeleteComponents(existingComponents);
            }
            else
            {
                DestroyImmediate(this);
            }
        }

        /// <summary>
        /// Hide inspector to prevent accidental editing of transform.
        /// </summary>
        private void OnEnable() => this.transform.hideFlags = HideFlags.HideInInspector;
#endif

        /// <summary>
        /// Resets the transform properties to their identities, i.e. (0, 0, 0), (0˚, 0˚, 0˚), and (100%, 100%, 100%).
        /// </summary>
        private void Update()
        {
            this.transform.localPosition = Vector3.zero;
            this.transform.localRotation = Quaternion.identity;
            this.transform.localScale = Vector3.one;

#if UNITY_EDITOR
            if (!Application.IsPlaying(this.gameObject))
            {
                this.AddFolderData();
            }

            this.EnsureExclusiveComponent();
#endif
        }

        /// <summary>Takes direct children and links them to the parent transform or global.</summary>
        /// <param name="strippingMode">Stripping mode to apply.</param>
        /// <param name="capitalizeFolderName">
        /// Whether to capitalize the folder name when replacing it with a separator.
        /// Applies only if <paramref name="strippingMode"/> is <see cref="StrippingMode.ReplaceWithSeparator"/>
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
            int index = this.transform.GetSiblingIndex(); // keep components in logical order

            foreach (var child in GetComponentsInChildren<Transform>(includeInactive: true))
            {
                // gather only first-level children
                if (child.parent != this.transform)
                    continue;

                if (strippingMode == StrippingMode.PrependWithFolderName)
                {
                    child.name = $"{this.name}/{child.name}";
                }

                child.SetParent(this.transform.parent, true);
                child.SetSiblingIndex(++index);
            }
        }

        private void HandleSelf(StrippingMode strippingMode, bool capitalizeFolderName)
        {
            if (strippingMode == StrippingMode.ReplaceWithSeparator)
            {
                // If the folder name is already a separator, don't change it.
                if ( ! name.StartsWith("--- "))
                {
                    name = $"--- {(capitalizeFolderName ? name.ToUpper() : name)} ---";
                }

                return;
            }

            if (Application.isPlaying)
            {
                Destroy(this.gameObject);
            }
            else
            {
                DestroyImmediate(this.gameObject);
            }
        }
    }
}
