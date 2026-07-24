using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DominoLineBuilder))]
public class DominoLineBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DominoLineBuilder builder =
            (DominoLineBuilder)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Build Line"))
        {
            builder.Build();
        }

        if (GUILayout.Button("Align Existing"))
        {
            builder.Align();
        }
    }
}