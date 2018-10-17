using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

public static class FlattenFolderBeforeBuild {
    /// <summary>
    /// Flatten every folder in scene after scene has been processed.
    /// </summary>
    [PostProcessScene]
    public static void Flatten() {
        var folders = Object.FindObjectsOfType<Folder>().Where(i => i != null);

        foreach (var folder in folders) {
            folder.Flatten();
            Object.DestroyImmediate(folder.gameObject);
        }
    }
}

public static class AddFolder {
    /// <summary>
    /// Add new folder "prefab".
    /// </summary>
    /// <param name="command">Menu command information.</param>
    [MenuItem("GameObject/Create Folder", false, 0)]
    public static void AddPrefab(MenuCommand command) {
        var obj = new GameObject { name = "Folder", tag = "EditorOnly" };
        obj.AddComponent<Folder>();
        GameObjectUtility.SetParentAndAlign(obj, (GameObject)command.context);
        Undo.RegisterCreatedObjectUndo(obj, "Create Folder");
    }
}
