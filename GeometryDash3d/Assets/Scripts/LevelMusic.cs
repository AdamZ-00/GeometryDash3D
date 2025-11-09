using UnityEngine;

public class LevelMusic : MonoBehaviour
{
    public AudioClip music;

    // Ne JAMAIS auto-jouer en Start. Laisse le menu décider.
    public void TryPlay()
    {
        if (SimpleAudioManager.Instance != null && music != null)
            SimpleAudioManager.Instance.PlayMusic(music);
    }
}
