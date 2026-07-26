using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SFX { PickupBlock, PickupOperator, Add, Subtract, Multiply, Divide, Target, Push, Boom }

[System.Serializable]
public class SFXEntry
{
    public SFX id;
    public AudioClip clip;
    [Range(0, 1)] public float volume = 1f;
    public float pitchJitter = 0.05f;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Pool Setup")]
    [SerializeField] private AudioSource sfxSourcePrefab;
    [SerializeField] private int poolSize = 8;

    [Header("Sound Bank")]
    [SerializeField] private List<SFXEntry> sfxLibrary;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float fadeDuration = 1f;
    [Range(0, 1)] public float musicVolume = 1f;

    [Header("Music Tracks")]
    public AudioClip menuMusic;
    public AudioClip levelMusic;
    public AudioClip winMusic;
    public AudioClip lossMusic;

    private Queue<AudioSource> pool = new Queue<AudioSource>();
    private Dictionary<SFX, SFXEntry> library = new Dictionary<SFX, SFXEntry>();
    private Coroutine fadeRoutine;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource src = Instantiate(sfxSourcePrefab, transform);
            pool.Enqueue(src);
        }

        foreach (var entry in sfxLibrary)
        {
            if (!library.ContainsKey(entry.id))
                library.Add(entry.id, entry);
        }

        musicSource.volume = musicVolume;
    }

    // ---------- SFX ----------

    public void PlaySFX(SFX id)
    {
        if (!library.TryGetValue(id, out SFXEntry entry) || entry.clip == null)
        {
            Debug.LogWarning($"No clip assigned for {id}");
            return;
        }

        AudioSource src = pool.Dequeue();
        pool.Enqueue(src);
        src.pitch = 1f + Random.Range(-entry.pitchJitter, entry.pitchJitter);
        src.PlayOneShot(entry.clip, entry.volume);
    }

    // ---------- Music ----------

    public void PlayMenuMusic() => PlayMusic(menuMusic, true);
    public void PlayLevelMusic() => PlayMusic(levelMusic, true);
    public void PlayWinMusic() => PlayMusic(winMusic, false);
    public void PlayLossMusic() => PlayMusic(lossMusic, false);

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return; // already playing this track

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeToTrack(clip, loop));
    }

    IEnumerator FadeToTrack(AudioClip newClip, bool loop)
    {
        // Fade out current track
        float startVol = musicSource.volume;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
            yield return null;
        }
        musicSource.volume = 0f;

        // Swap clip
        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.Play();

        // Fade in new track
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, t / fadeDuration);
            yield return null;
        }
        musicSource.volume = musicVolume;
        fadeRoutine = null;
    }

    public void StopMusic()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        musicSource.Stop();
    }
}