using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using UnityHierarchyFolders.Runtime;

namespace UnityHierarchyFolders.Editor
{
    [CustomEditor(typeof(Folder))]
    public class FolderEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        public override void OnInspectorGUI()
        {
            //DrawDefaultInspector();

            GUILayout.Label("Icon Color", EditorStyles.boldLabel);
            
            SerializedProperty colorIndexProperty = serializedObject.FindProperty("_colorIndex");

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var buttonSize = 25f;
            
            var gridRect = EditorGUILayout.GetControlRect(false, buttonSize * FolderIcon.IconValueCount, 
                GUILayout.Width(buttonSize * FolderIcon.IconHueCount));

            int currentIndex = colorIndexProperty.intValue;
            for (int row = 0; row < FolderIcon.IconValueCount; row++)
            {
                for (int column = 0; column < FolderIcon.IconHueCount; column++)
                {
                    int index = 1 + column + row * FolderIcon.IconHueCount;
                    float width = gridRect.width / FolderIcon.IconHueCount;
                    float height = gridRect.height / FolderIcon.IconValueCount;
                    Rect rect = new Rect(gridRect.x + width * column, gridRect.y + height * row, width, height);
                    var (icon, _) = FolderIcon.coloredFolderIcons[index];

                    if (Event.current.type == EventType.Repaint)
                    {
                        if (index == currentIndex)
                        {
                            GUIStyle hover = "TV Selection";
                            hover.Draw(rect, false, false, false, false);
                        }
                        else if (rect.Contains(Event.current.mousePosition))
                        {
                            GUI.backgroundColor = new Color(.7f, .7f, .7f, 1f);
                            GUIStyle white = "WhiteBackground";
                            white.Draw(rect, false, false, true, false);
                            GUI.backgroundColor = Color.white;
                        }
                    }
                    
                    if (GUI.Button(rect, icon, EditorStyles.label))
                    {
                        Undo.RecordObject(target, "Set Folder Color");
                        colorIndexProperty.intValue = currentIndex == index ? 0 : index;
                        serializedObject.ApplyModifiedProperties();
                        EditorApplication.RepaintHierarchyWindow();
                        GUIUtility.ExitGUI();
                    }
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10f);
        }
    }
}