using UnityEngine;

public class DominoSoundManager : MonoBehaviour
{
    public static DominoSoundManager Instance { get; private set; }

    [Header("Collision Sounds")]
    [SerializeField] private AudioClip[] collisionClips;

    [Header("Settings")]
    [SerializeField, Range(0f, 1f)]
    private float masterVolume = 0.55f;

    [SerializeField] private float minImpactSpeed = 0.25f;
    [SerializeField] private float maxImpactSpeed = 2.5f;

    [SerializeField] private int poolSize = 16;

    private AudioSource[] sources;
    private int nextSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        sources = new AudioSource[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source =
                gameObject.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.spatialBlend = 0f;

            sources[i] = source;
        }
    }

    public void PlayCollision(float impactSpeed)
    {
        if (collisionClips == null ||
            collisionClips.Length == 0)
            return;

        if (impactSpeed < minImpactSpeed)
            return;

        AudioClip clip =
            collisionClips[
                Random.Range(0, collisionClips.Length)
            ];

        AudioSource source = sources[nextSource];

        nextSource =
            (nextSource + 1) % sources.Length;

        float impactStrength =
            Mathf.InverseLerp(
                minImpactSpeed,
                maxImpactSpeed,
                impactSpeed
            );

        source.Stop();

        source.clip = clip;

        source.pitch =
            Random.Range(0.94f, 1.06f);

        source.volume =
            masterVolume *
            Mathf.Lerp(0.35f, 1f, impactStrength);

        source.Play();
    }
}