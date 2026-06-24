using UnityEngine;

public class DominoGrid : MonoBehaviour
{
    public GameObject dominoPrefab;

    public int width = 10;
    public int height = 10;

    public float spacing = 0.25f;

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 pos = new Vector3(
    (x - width / 2f) * spacing,
    0.25f,
    (y - height / 2f) * spacing);

                GameObject domino =
                    Instantiate(
                        dominoPrefab,
                        pos,
                        Quaternion.identity,
                        transform);

                DominoPixel pixel =
                    domino.GetComponent<DominoPixel>();

                if (
                    (x == 3 && y == 8) ||
                    (x == 4 && y == 8) ||
                    (x == 6 && y == 8) ||
                    (x == 7 && y == 8)
                   )
                {
                    pixel.IsHeartPixel = true;
                }
            }
        }
    }
}