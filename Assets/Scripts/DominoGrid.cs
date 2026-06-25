using UnityEngine;

public class DominoGrid : MonoBehaviour
{
    public GameObject dominoPrefab;

    public int width = 10;
    public int height = 10;

    public float spacing = 0.15f;

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

                Domino d = domino.GetComponent<Domino>();

                Color pixelColor = GetHeartPixel(x, y);

                d.SetColor(pixelColor);
            }
        }
    }

    Color GetHeartPixel(int x, int y)
    {
        if (
            x >= 2 &&
            x <= 7 &&
            y >= 2 &&
            y <= 7
           )
        {
            return Color.red;
        }

        return Color.white;
    }
}