using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralLevelGenerator))]
public class ProceduralLevelGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(15);

        ProceduralLevelGenerator generator =
            (ProceduralLevelGenerator)target;


        if (GUILayout.Button(
            "GENERATE LEVEL",
            GUILayout.Height(40)))
        {
            generator.GenerateLevel();

            EditorUtility.SetDirty(
                generator
            );
        }


        GUILayout.Space(5);


        if (GUILayout.Button(
            "CLEAR LEVEL",
            GUILayout.Height(30)))
        {
            generator.ClearLevel();

            EditorUtility.SetDirty(
                generator
            );
        }
    }
}