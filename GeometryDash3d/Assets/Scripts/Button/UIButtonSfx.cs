using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSfx : MonoBehaviour
{
    public AudioClip clickClip;
    [Range(0f, 1f)] public float volume = 0.8f;

    void Awake()
    {
        var btn = GetComponent<Button>();
        btn.onClick.AddListener(Play);
    }

    void Play()
    {
        // Si SimpleAudioManager existe, utilise-le. Sinon, fallback.
        if (SimpleAudioManager.Instance != null && clickClip)
        {
            // Tu peux ajouter un petit wrapper PlaySfx dans ton AudioManager si tu préfères.
            var src = SimpleAudioManager.Instance.GetComponent<AudioSource>();
            if (src) src.PlayOneShot(clickClip, volume);
        }
        else if (clickClip)
        {
            AudioSource.PlayClipAtPoint(clickClip, Vector3.zero, volume);
        }
    }
}
