using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[ExecuteInEditMode]
public class Folder : MonoBehaviour {
#if UNITY_EDITOR
    Folder() {
        EditorApplication.hierarchyWindowChanged += HandleAddedComponents;
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
            var type = comp.GetType();
            if (comp != this && type != typeof(Transform)) {
                DestroyImmediate(comp);
                EditorUtility.DisplayDialog("Can't add script",
                                            "Can't add additional scripts to Folder objects, " +
                                            "not like you would need to.", "OK");
            }
        }
    }

    /// <summary>
    /// Hide gizmos and inspector to prevent accidental editing of transform.
    /// 
    /// (If multiple objects are selected along with a folder, this turns off all of their gizmos.)
    /// </summary>
    private void OnEnable() {
        this.transform.hideFlags = HideFlags.HideInInspector;
        Tools.hidden = true;
    }

    /// <summary>
    /// Return gizmos to their normal state.
    /// </summary>
    private void OnDisable() {
        Tools.hidden = false;
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