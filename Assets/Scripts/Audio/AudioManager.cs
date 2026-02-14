using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music Sources")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;

    [Header("Music Tracks")]
    [SerializeField] private AudioClip defaultMusic;
    [SerializeField] private AudioClip chaseMusic;
    [SerializeField] private AudioClip explorationMusic;
    [SerializeField] private AudioClip bossMusic;

    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private AudioSource activeSource;
    private AudioSource inactiveSource;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSourceA == null)
        {
            musicSourceA = gameObject.AddComponent<AudioSource>();
            musicSourceA.loop = true;
            musicSourceA.playOnAwake = false;
            musicSourceA.spatialBlend = 0f;
        }

        if (musicSourceB == null)
        {
            musicSourceB = gameObject.AddComponent<AudioSource>();
            musicSourceB.loop = true;
            musicSourceB.playOnAwake = false;
            musicSourceB.spatialBlend = 0f;
        }

        activeSource = musicSourceA;
        inactiveSource = musicSourceB;
    }

    void Start()
    {
        if (defaultMusic != null)
        {
            PlayMusic(defaultMusic);
        }
    }

    public void PlayMusic(AudioClip clip, float fadeDuration = 1f)
    {
        if (clip == null) return;
        if (activeSource.clip == clip && activeSource.isPlaying) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(CrossfadeMusic(clip, fadeDuration));
    }

    public void StopMusic(float fadeDuration = 1f)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOut(activeSource, fadeDuration));
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (activeSource.isPlaying)
            activeSource.volume = musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public float MusicVolume => musicVolume;
    public float SFXVolume => sfxVolume;

    public void PlayChaseMusic(float fadeDuration = 0.5f)
    {
        if (chaseMusic != null)
            PlayMusic(chaseMusic, fadeDuration);
    }

    public void PlayExplorationMusic(float fadeDuration = 1f)
    {
        if (explorationMusic != null)
            PlayMusic(explorationMusic, fadeDuration);
    }

    public void PlayBossMusic(float fadeDuration = 0.5f)
    {
        if (bossMusic != null)
            PlayMusic(bossMusic, fadeDuration);
    }

    public void PlayDefaultMusic(float fadeDuration = 1f)
    {
        if (defaultMusic != null)
            PlayMusic(defaultMusic, fadeDuration);
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip, float duration)
    {
        inactiveSource.clip = newClip;
        inactiveSource.volume = 0f;
        inactiveSource.Play();

        float timer = 0f;
        float startVolume = activeSource.volume;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            activeSource.volume = Mathf.Lerp(startVolume, 0f, t);
            inactiveSource.volume = Mathf.Lerp(0f, musicVolume, t);

            yield return null;
        }

        activeSource.Stop();
        activeSource.volume = 0f;
        inactiveSource.volume = musicVolume;

        // Swap sources
        (activeSource, inactiveSource) = (inactiveSource, activeSource);
        fadeCoroutine = null;
    }

    private IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        source.Stop();
        source.volume = 0f;
        fadeCoroutine = null;
    }
}
