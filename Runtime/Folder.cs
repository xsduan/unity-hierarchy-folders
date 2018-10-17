#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif
using UnityEngine;

[DisallowMultipleComponent]
[ExecuteInEditMode]
public class Folder : MonoBehaviour {
#if UNITY_EDITOR
    private static bool addedSelectionResetCallback;
    Folder() {
        EditorApplication.hierarchyWindowChanged += HandleAddedComponents;

        // add reset callback first in queue
        if (!addedSelectionResetCallback) {
            Selection.selectionChanged += () => Tools.hidden = false;
            addedSelectionResetCallback = true;
        }

        Selection.selectionChanged += HandleSelection;
    }
#endif

    private void Update() {
        ResetTransform();

        if (Application.isPlaying) {
            FinishFlattening();
        }
    }

    /// <summary>
    /// Resets the transform properties to their identities (i.e. (0, 0, 0), (0˚, 0˚, 0˚), and (100%, 100%, 100%)).
    /// </summary>
    private void ResetTransform() {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(1, 1, 1);
    }

    /// <summary>
    /// Destroy object as it should have been when building.
    /// </summary>
    private void FinishFlattening() {
        Debug.LogError("Folder \"" + name + "\" was supposed to be destroyed on build.");
        Flatten();
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Destroys any new components that were added.
    /// </summary>
    private void HandleAddedComponents() {
        // we are running (if this != null when running something went wrong)
        if (Application.isPlaying || this == null) {
            return;
        }

        var existingComponents = GetComponents<Component>().Where((i) => i != null);
        foreach (var comp in existingComponents) {
            bool shouldDestroy = false, asked = false;

            var type = comp.GetType();
            if (comp != this && type != typeof(Transform)) {
                // Ask if should destroy other components
                if (!asked) {
                    shouldDestroy = EditorUtility.DisplayDialog("Can't add script",
                                                                "Folders shouldn't be used with " +
                                                                "other components. Should the " +
                                                                "folder be added/kept?", 
                                                                "Yes", "No");
                    asked = true;
                }

                if (shouldDestroy) {
                    DestroyImmediate(comp);
                } else {
                    DestroyImmediate(this);
                }
            }
        }
    }

    /// <summary>
    /// Hides the transform gizmo if necessary to avoid accidental editing of transform.
    /// 
    /// (If multiple objects are selected along with a folder, this turns off all of their gizmos.)
    /// </summary>
    private void HandleSelection() {
        if (this != null) {
            Tools.hidden |= Selection.activeGameObject == gameObject;
        }
    }

    /// <summary>
    /// Hide inspector to prevent accidental editing of transform.
    /// </summary>
    private void OnEnable() {
        this.transform.hideFlags = HideFlags.HideInInspector;
    }
#endif

    /// <summary>
    /// Takes direct children and links them to the parent transform or global.
    /// </summary>
    public void Flatten() {
        // gather first-level children
        foreach (Transform child in transform.GetComponentsInChildren<Transform>()) {
            if (child.parent == transform) {
                child.name = name + "/" + child.name;
                child.parent = transform.parent;
            }
        }
    }
}