using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DominoSplineBuilder))]
public class DominoSplineBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        DominoSplineBuilder builder =
            (DominoSplineBuilder)target;

        if (GUILayout.Button("Build Dominoes"))
        {
            builder.Build();
        }
    }
}