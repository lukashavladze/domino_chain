using UnityEngine;

public class Domino : MonoBehaviour
{
    public bool HasFallen;

    private Renderer topRenderer;

    private void Start()
    {
        //topRenderer = transform.Find("TopFace").GetComponent<Renderer>();
    }

    private void Update()
    {
        if (!HasFallen && transform.up.y < 0.7f)
        {
            HasFallen = true;
        }
    }

    public void SetColor(Color color)
    {
        if (topRenderer != null)
        {
            topRenderer.material.color = color;
        }
    }
}