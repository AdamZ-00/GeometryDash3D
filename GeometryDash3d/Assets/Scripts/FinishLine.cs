using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinishLine : MonoBehaviour
{
    [Header("Win SFX (3D)")]
    [SerializeField] private AudioSource winSource;   // AudioSource sur la Finish Line (Output = SFX)
    [SerializeField] private AudioClip winClip;       // ta fanfare / musique de win
    [Range(0f, 1f)] public float winVolume = 1f;

    [Header("3D Settings")]
    [Tooltip("Distance à partir de laquelle le son est à pleine puissance.")]
    public float minDistance = 3f;
    [Tooltip("Distance maximale d'audition (log rolloff).")]
    public float maxDistance = 25f;
    [Tooltip("Désactive l'effet doppler pour un rendu propre.")]
    public float dopplerLevel = 0f;

    private bool played = false;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;

        // Auto-ajoute une source s'il n'y en a pas
        if (winSource == null)
        {
            winSource = GetComponent<AudioSource>();
            if (winSource == null) winSource = gameObject.AddComponent<AudioSource>();
        }

        ConfigureSourceFor3D();
    }

    private void Awake()
    {
        if (winSource == null)
        {
            winSource = GetComponent<AudioSource>();
            if (winSource == null) winSource = gameObject.AddComponent<AudioSource>();
        }

        ConfigureSourceFor3D();
    }

    private void ConfigureSourceFor3D()
    {
        // Réglages 3D propres
        winSource.playOnAwake = false;
        winSource.loop = false;
        winSource.spatialBlend = 1f;              // 3D
        winSource.rolloffMode = AudioRolloffMode.Logarithmic;
        winSource.minDistance = Mathf.Max(0.1f, minDistance);
        winSource.maxDistance = Mathf.Max(winSource.minDistance + 1f, maxDistance);
        winSource.dopplerLevel = dopplerLevel;    // évite l'effet doppler
        // IMPORTANT : dans l’Inspector, mets Output = SFX (ton AudioMixer)
    }

    private void OnTriggerEnter(Collider other)
    {
        if (played) return;
        if (!other.CompareTag("Player")) return;

        played = true;

        // 1) Joue la fanfare depuis le mur (continue de jouer même si Time.timeScale = 0)
        if (winClip != null && winSource != null)
        {
            winSource.transform.position = transform.position; // sécu
            winSource.PlayOneShot(winClip, winVolume);
        }

        // 2) Termine le niveau (stop musica principale + UI + pause)
        var mgr = FindObjectOfType<LevelManagerLogic>();
        if (mgr != null)
            mgr.FinishRun();
    }
}
