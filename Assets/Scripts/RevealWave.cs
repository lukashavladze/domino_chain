using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RevealWave : MonoBehaviour
{
    [Header("Wave Visual")]
    public float startRadius = 0.03f;
    public float endRadius = 0.35f;
    public float duration = 0.35f;

    public Color waveColor =
        new Color(0.25f, 0.8f, 1f, 0.9f);

    [Range(12, 64)]
    public int segments = 32;

    [Header("Reveal")]
    [Tooltip("How often the expanding wave paints the ground.")]
    public float revealInterval = 0.03f;

    private LineRenderer line;

    private float timer;
    private float revealTimer;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();

        line.loop = true;
        line.useWorldSpace = false;
        line.positionCount = segments;

        line.startWidth = 0.025f;
        line.endWidth = 0.025f;

        line.startColor = waveColor;
        line.endColor = waveColor;

        UpdateCircle(startRadius);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float t = Mathf.Clamp01(
            timer / duration
        );

        float radius = Mathf.Lerp(
            startRadius,
            endRadius,
            t
        );

        // Visual blue circle
        UpdateCircle(radius);

        // Reveal exactly according to current wave radius
        if (RevealPainter.Instance != null)
        {
            RevealPainter.Instance.PaintCircle(
                transform.position,
                radius
            );
        }

        // Fade visual wave
        Color color = waveColor;

        color.a = Mathf.Lerp(
            waveColor.a,
            0f,
            t
        );

        line.startColor = color;
        line.endColor = color;

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private void RevealGround()
    {
        if (RevealPainter.Instance == null)
            return;

        RevealPainter.Instance.Paint(
            transform.position,
            transform.forward
        );
    }

    private void UpdateCircle(float radius)
    {
        for (int i = 0; i < segments; i++)
        {
            float angle =
                i / (float)segments *
                Mathf.PI *
                2f;

            Vector3 position = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            line.SetPosition(i, position);
        }
    }
}