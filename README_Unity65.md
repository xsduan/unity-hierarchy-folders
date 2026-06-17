# Unity 6.5 compatibility notes

This branch updates Hierarchy Folders for Unity 6.5 editor compatibility.

## What changed

- Uses `Object.GetEntityId()` on Unity 6.5 and newer.
- Keeps a compatibility path for hierarchy callbacks that still receive an `int instanceID`.
- Moves editor selection callback registration from the MonoBehaviour constructor to `OnEnable` / `OnDisable`.
- Removes constant inspector repaint from the folder color inspector.
- Adds a simple hierarchy color overlay that repaints when the folder color changes.
- Removes LINQ from frequent editor paths in `Folder.cs`.

## Unity 6.5 smoke test

1. Import the package in Unity 6.5.
2. Create a folder from the GameObject menu.
3. Select the folder and change its color in the inspector.
4. Verify the hierarchy color marker updates immediately.
5. Select the folder and verify transform tools are hidden.
6. Select a normal GameObject and verify tools are restored.
7. Confirm the package emits no obsolete `GetInstanceID` warnings on Unity 6.5.
