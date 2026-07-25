using System.Collections.Generic;
using UnityEngine;

public enum SFX { PickupBlock, PickupOperator, Add, Subtract, Multiply, Divide, Target}

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
    [SerializeField] private AudioSource musicSource; // drag the AudioSource you just added

    private Queue<AudioSource> pool = new Queue<AudioSource>();
    private Dictionary<SFX, SFXEntry> library = new Dictionary<SFX, SFXEntry>();

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
    }

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
    
    // Music
    public void PlayMusic(AudioClip clip, bool restart = true)
    {
        if (clip == null) return;

        if (musicSource.clip == clip && musicSource.isPlaying && !restart)
            return; // already playing this track, don't restart it

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void RestartMusic()
    {
        musicSource.Stop();
        musicSource.Play(); // same clip, from the top
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }
}