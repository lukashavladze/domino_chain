using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DominoLine))]
public class DominoLineEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        if (GUILayout.Button("Auto Connect"))
        {
            DominoLine line = (DominoLine)target;

            Undo.RecordObject(line, "Auto Connect Dominoes");

            line.AutoConnect();

            EditorUtility.SetDirty(line);

            foreach (Domino domino in line.dominoes)
            {
                EditorUtility.SetDirty(domino);
            }
        }
    }
}