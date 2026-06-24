using UnityEngine;

public class DominoPixel : MonoBehaviour
{
    public bool IsHeartPixel;

    public Renderer topFace;

    void Start()
    {
        if (IsHeartPixel)
        {
            topFace.material.color = Color.red;
        }
        else
        {
            topFace.material.color = Color.white;
        }
    }
}