using UnityEngine;

public class LevelMusic : MonoBehaviour
{
    public AudioClip music;

    private void Start()
    {
        if (SimpleAudioManager.Instance != null && music != null)
            SimpleAudioManager.Instance.PlayMusic(music);
    }
}
