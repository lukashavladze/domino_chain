using UnityEngine;

public class CoverTile : MonoBehaviour
{
    bool revealed;

    public void Reveal()
    {
        if (revealed)
            return;

        revealed = true;

        gameObject.SetActive(false);
    }
}