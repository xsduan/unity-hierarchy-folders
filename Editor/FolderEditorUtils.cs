using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

public static class FolderEditorUtils {
    const string actionName = "Create Heirarchy Folder";

    /// <summary>
    /// Flatten every folder in scene after scene has been processed.
    /// </summary>
    [PostProcessScene]
    public static void Flatten() {
        foreach (var folder in UnityEngine.Object.FindObjectsOfType<Folder>()) {
            folder.Flatten();
        }
    }

    /// <summary>
    /// Add new folder "prefab".
    /// </summary>
    /// <param name="command">Menu command information.</param>
    [MenuItem("GameObject/" + actionName, false, 0)]
    public static void AddFolderPrefab(MenuCommand command) {
        var obj = new GameObject { name = "Folder", tag = "EditorOnly" };
        obj.AddComponent<Folder>();

        GameObjectUtility.SetParentAndAlign(obj, (GameObject)command.context);
        Undo.RegisterCreatedObjectUndo(obj, actionName);
    }
}

// taken from: https://gamedev.stackexchange.com/a/140799
static class CanDestroyExtension {
    private static bool Requires(Type obj, Type req) {
        return Attribute.IsDefined(obj, typeof(RequireComponent)) &&
               Attribute.GetCustomAttributes(obj, typeof(RequireComponent))
                        .OfType<RequireComponent>()
                        .Any(rc => rc.m_Type0.IsAssignableFrom(req));
    }

    /// <summary>
    /// Checks whether the stated component can be destroyed without violating dependencies.
    /// </summary>
    /// <returns>Is component destroyable?</returns>
    /// <param name="g">The GameObject to search.</param>
    /// <param name="t">Component candidate for destruction.</param>
    internal static bool CanDestroy(this GameObject g, Component t) {
        return !g.GetComponents<Component>().Any(c => Requires(c.GetType(), t.GetType()));
    }
}
