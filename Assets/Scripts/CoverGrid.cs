//using UnityEngine;

//public class CoverGrid : MonoBehaviour
//{
//    public GameObject coverPrefab;

//    public int width = 40;
//    public int height = 40;

//    public float tileSize = 0.25f;

//    public float y = 0.01f;

//    [ContextMenu("Generate")]
//    public void Generate()
//    {
//        while (transform.childCount > 0)
//        {
//#if UNITY_EDITOR
//            DestroyImmediate(transform.GetChild(0).gameObject);
//#else
//            Destroy(transform.GetChild(0).gameObject);
//#endif
//        }

//        float startX = -(width * tileSize) * 0.5f + tileSize * 0.5f;
//        float startZ = -(height * tileSize) * 0.5f + tileSize * 0.5f;

//        for (int x = 0; x < width; x++)
//        {
//            for (int z = 0; z < height; z++)
//            {
//                Vector3 pos = new Vector3(
//                    startX + x * tileSize,
//                    y,
//                    startZ + z * tileSize);

//                GameObject tile =
//                    Instantiate(coverPrefab, pos, Quaternion.identity, transform);

//                tile.name = $"Tile_{x}_{z}";
//            }
//        }
//    }
//}