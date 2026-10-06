using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralLevelGenerator))]
public class ProceduralLevelGeneratorEditor : Editor
{
    private const string LevelFolder =
    "Assets/Resources/Levels";


    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(15);

        ProceduralLevelGenerator generator =
            (ProceduralLevelGenerator)target;


        // ==========================================
        // GENERATE
        // ==========================================

        if (GUILayout.Button(
            "GENERATE LEVEL",
            GUILayout.Height(40)))
        {
            generator.GenerateLevel();

            EditorUtility.SetDirty(generator);
        }


        GUILayout.Space(5);


        // ==========================================
        // CLEAR
        // ==========================================

        if (GUILayout.Button(
            "CLEAR LEVEL",
            GUILayout.Height(30)))
        {
            generator.ClearLevel();

            EditorUtility.SetDirty(generator);
        }


        GUILayout.Space(15);

        EditorGUILayout.LabelField(
            "Saved Level Prefabs",
            EditorStyles.boldLabel
        );


        // ==========================================
        // SAVE CURRENT GENERATED LEVEL
        // ==========================================

        if (GUILayout.Button(
            "SAVE LEVEL AS PREFAB",
            GUILayout.Height(40)))
        {
            SaveGeneratedLevel(generator);
        }


        GUILayout.Space(5);


        // ==========================================
        // GENERATE + SAVE
        // ==========================================

        if (GUILayout.Button(
            "GENERATE + SAVE PREFAB",
            GUILayout.Height(40)))
        {
            generator.GenerateLevel();

            SaveGeneratedLevel(generator);
        }


        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Saved to:\n" + LevelFolder,
            MessageType.Info
        );
    }


    // =========================================================
    // SAVE GENERATED LEVEL
    // =========================================================

    private void SaveGeneratedLevel(
        ProceduralLevelGenerator generator)
    {
        // Find the root created by your generator.
        Transform generatedRoot =
            generator.transform.Find(
                "_GeneratedLevel"
            );


        if (generatedRoot == null)
        {
            Debug.LogError(
                "No generated level found. " +
                "Generate a level first."
            );

            return;
        }


        // ==========================================
        // CREATE FOLDERS
        // ==========================================

        EnsureFolderExists(
            "Assets/Prefabs"
        );

        EnsureFolderExists(
            LevelFolder
        );


        // ==========================================
        // CREATE FILE NAME
        // ==========================================

        string prefabName =
            $"Level_{generator.levelNumber:0000}.prefab";


        string prefabPath =
            $"{LevelFolder}/{prefabName}";


        // ==========================================
        // CHECK IF ALREADY EXISTS
        // ==========================================

        GameObject existingPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                prefabPath
            );


        if (existingPrefab != null)
        {
            bool replace =
                EditorUtility.DisplayDialog(
                    "Level Already Exists",
                    $"Level {generator.levelNumber} " +
                    "already exists.\n\nReplace it?",
                    "Replace",
                    "Cancel"
                );


            if (!replace)
                return;
        }

        // ==========================================
        // LEVEL DATA
        // ==========================================

        SavedLevelData levelData =
            generatedRoot.GetComponent<SavedLevelData>();

        if (levelData == null)
        {
            levelData =
                generatedRoot.gameObject.AddComponent<SavedLevelData>();
        }

        levelData.levelNumber =
            generator.levelNumber;

        levelData.groundSize =
            generator.CalculateGroundSize();

        EditorUtility.SetDirty(levelData);


        // ==========================================
        // SAVE
        // ==========================================

        GameObject savedPrefab =
            PrefabUtility.SaveAsPrefabAsset(
                generatedRoot.gameObject,
                prefabPath
            );


        if (savedPrefab == null)
        {
            Debug.LogError(
                $"Failed to save Level " +
                $"{generator.levelNumber}"
            );

            return;
        }


        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();


        Debug.Log(
            $"LEVEL {generator.levelNumber} " +
            $"SAVED AS PREFAB: {prefabPath}"
        );


        // Select the newly saved prefab.
        Selection.activeObject =
            savedPrefab;

        EditorGUIUtility.PingObject(
            savedPrefab
        );
    }


    // =========================================================
    // CREATE FOLDER IF NEEDED
    // =========================================================

    private void EnsureFolderExists(
        string folderPath)
    {
        if (AssetDatabase.IsValidFolder(
                folderPath))
        {
            return;
        }


        string parent =
            Path.GetDirectoryName(
                folderPath
            );

        string folderName =
            Path.GetFileName(
                folderPath
            );


        // Unity paths use /
        parent =
            parent.Replace("\\", "/");


        if (!AssetDatabase.IsValidFolder(
                parent))
        {
            EnsureFolderExists(parent);
        }


        AssetDatabase.CreateFolder(
            parent,
            folderName
        );
    }
}