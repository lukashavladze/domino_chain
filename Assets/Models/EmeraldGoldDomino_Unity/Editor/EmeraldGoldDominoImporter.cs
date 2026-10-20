using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public static class EmeraldGoldDominoImporter
{
    [MenuItem("Tools/Domino/Create Emerald Gold Prefab")]
    public static void CreatePrefab()
    {
        string[] guids = AssetDatabase.FindAssets("EmeraldGoldDomino t:Model");
        if (guids.Length == 0) { Debug.LogError("Import EmeraldGoldDomino.obj first."); return; }
        string modelPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        string dir = Path.GetDirectoryName(modelPath).Replace('\\', '/');
        string matDir = dir + "/GeneratedMaterials";
        if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder(dir, "GeneratedMaterials");
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) { Debug.LogError("URP Lit shader unavailable."); return; }
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "/Textures/Emerald_Marble.png");
        string[] names = {"EmeraldFront", "EmeraldBack", "GoldFrontTrim", "GoldBackTrim", "DarkSides", "GoldBevel"};
        Material[] materials = new Material[names.Length];
        for(int i=0;i<names.Length;i++)
        {
            string path = matDir + "/" + names[i] + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat,path); }
            mat.shader = shader;
            mat.SetFloat("_Metallic", i==0 || i==1 ? 0.05f : (i==4 ? 0.45f : 0.9f));
            mat.SetFloat("_Smoothness", i==0 || i==1 ? 0.72f : (i==4 ? 0.48f : 0.82f));
            mat.SetColor("_BaseColor", i==0 || i==1 ? Color.white :
                (i==4 ? new Color(.22f,.16f,.10f) : new Color(.83f,.59f,.27f)));
            if((i==0 || i==1) && tex != null) mat.SetTexture("_BaseMap", tex);
            EditorUtility.SetDirty(mat);
            materials[i]=mat;
        }
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        GameObject root = new GameObject("Domino_EmeraldGold");
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
        visual.name = "Visual_EmeraldGold";
        visual.transform.SetParent(root.transform, false);
        // OBJ uses X thickness, Y height, Z width; no transform changes needed.
        foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>())
        {
            // Mesh submesh order follows usemtl order in OBJ; use imported names when available.
            var old = renderer.sharedMaterials;
            Material[] mapped = new Material[old.Length];
            for(int i=0;i<old.Length;i++)
            {
                string n=old[i] != null ? old[i].name : "";
                int ix=System.Array.FindIndex(names, v=>n.Contains(v));
                mapped[i] = materials[ix>=0 ? ix : Mathf.Min(i,materials.Length-1)];
            }
            renderer.sharedMaterials = mapped;
        }
        var collider = root.AddComponent<BoxCollider>();
        collider.size = new Vector3(.1f,1f,.4f);
        root.AddComponent<Rigidbody>();
        string prefabPath = dir + "/Domino_EmeraldGold.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("Created " + prefabPath + ". For your game, replace ONLY the visual child of your existing gameplay prefab to preserve Domino scripts and physics.");
    }
}
