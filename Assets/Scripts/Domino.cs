using UnityEngine;

public class Domino : MonoBehaviour
{
    public bool HasFallen;

    void Update()
    {
        if (transform.up.y < 0.7f)
        {
            HasFallen = true;
        }
    }
}