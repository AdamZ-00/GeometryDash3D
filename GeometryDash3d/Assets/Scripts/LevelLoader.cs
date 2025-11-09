using UnityEngine;
using System;

public class LevelLoader : MonoBehaviour
{
    [Header("Prefabs de niveaux (un par index)")]
    public GameObject[] levelPrefabs;

    [Header("Musique par niveau (alignée sur les prefabs)")]
    public AudioClip[] musicPerLevel;

    [Header("Où instancier le niveau")]
    public Transform levelMount; // un empty (0,0,0)

    public const string KEY_SELECTED_LEVEL = "selected_level_index";

    GameObject currentLevelInstance;
    int currentIndex = -1;

    // Notifie quand un niveau est instancié (index, instance)
    public static event Action<int, GameObject> OnLevelInstantiated;

    void Start()
    {
        int idx = Mathf.Clamp(PlayerPrefs.GetInt(KEY_SELECTED_LEVEL, 0), 0, Mathf.Max(0, levelPrefabs.Length - 1));
        LoadLevel(idx);
        TeleportPlayerToSpawn();
    }

    public void LoadLevel(int index)
    {
        if (levelPrefabs == null || levelPrefabs.Length == 0)
        {
            Debug.LogError("[LevelLoader] Aucun prefab assigné.");
            return;
        }
        index = Mathf.Clamp(index, 0, levelPrefabs.Length - 1);

        if (currentLevelInstance) Destroy(currentLevelInstance);

        var parent = levelMount ? levelMount : transform;
        currentLevelInstance = Instantiate(levelPrefabs[index], parent);

        // Sécurité transform locale neutre
        var t = currentLevelInstance.transform;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;

        currentLevelInstance.name = $"LEVEL_{index + 1}";
        currentIndex = index;

        PlayerPrefs.SetInt(KEY_SELECTED_LEVEL, currentIndex);
        PlayerPrefs.Save();

        OnLevelInstantiated?.Invoke(currentIndex, currentLevelInstance);
    }

    public void TeleportPlayerToSpawn()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player || !currentLevelInstance) return;

        var spawn = currentLevelInstance.transform.Find("SpawnPoint");
        if (!spawn) return;

        player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

        var rb = player.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // ---------- MUSIQUE : robuste au timing ----------
    public void PlayCurrentLevelMusic()
    {
        if (SimpleAudioManager.Instance == null) return;

        int idx = (GetCurrentIndex() >= 0)
            ? GetCurrentIndex()
            : Mathf.Clamp(PlayerPrefs.GetInt(KEY_SELECTED_LEVEL, 0), 0, (musicPerLevel != null ? musicPerLevel.Length - 1 : 0));

        AudioClip clip = null;
        if (musicPerLevel != null && idx >= 0 && idx < musicPerLevel.Length)
            clip = musicPerLevel[idx];

        if (clip != null)
            SimpleAudioManager.Instance.PlayMusic(clip);   // <-- FORCE: stop -> set -> play
        else
            Debug.LogWarning($"[LevelLoader] Pas de clip pour index {idx}. Assigne 'Music Per Level'.");
    }

    public void ReloadCurrentLevel()
    {
        if (currentIndex < 0) currentIndex = PlayerPrefs.GetInt(KEY_SELECTED_LEVEL, 0);
        LoadLevel(currentIndex);
        TeleportPlayerToSpawn();
    }

    public int GetCurrentIndex() => currentIndex;

    public void PlayCurrentLevelMusicEndOfFrame(MonoBehaviour runner)
    {
        runner.StartCoroutine(_PlayMusicEOF());
        System.Collections.IEnumerator _PlayMusicEOF()
        {
            yield return null; // fin de frame
            PlayCurrentLevelMusic();
        }
    }
}
