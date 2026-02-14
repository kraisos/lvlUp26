using UnityEngine;
using System;
using System.Collections.Generic;

public class StoryAudioManager : MonoBehaviour
{
    public static StoryAudioManager Instance { get; private set; }

    [Header("Narration Source")]
    [SerializeField] private AudioSource narrationSource;

    [Header("Story Entries")]
    [SerializeField] private List<StoryEntry> storyEntries = new List<StoryEntry>();

    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float narrationVolume = 1f;
    [Tooltip("Lower music volume while narration plays")]
    [Range(0f, 1f)]
    [SerializeField] private float musicDuckVolume = 0.2f;

    public event Action<StoryEntry> OnNarrationStarted;
    public event Action<StoryEntry> OnNarrationFinished;

    public bool IsPlaying => narrationSource != null && narrationSource.isPlaying;

    private HashSet<string> playedEntries = new HashSet<string>();
    private StoryEntry currentEntry;
    private float originalMusicVolume;
    private bool isDucking;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (narrationSource == null)
        {
            narrationSource = gameObject.AddComponent<AudioSource>();
            narrationSource.loop = false;
            narrationSource.playOnAwake = false;
            narrationSource.spatialBlend = 0f;
        }
    }

    void Update()
    {
        if (currentEntry != null && !narrationSource.isPlaying)
        {
            FinishNarration();
        }
    }

    public void TriggerStory(StoryTriggerType triggerType)
    {
        List<StoryEntry> candidates = new List<StoryEntry>();

        foreach (var entry in storyEntries)
        {
            if (entry == null || entry.triggerType != triggerType) continue;
            if (entry.audioClip == null) continue;
            if (entry.playOnce && playedEntries.Contains(entry.entryId)) continue;

            candidates.Add(entry);
        }

        if (candidates.Count == 0) return;

        // If narration is playing, only interrupt if new entry has higher priority
        if (IsPlaying && currentEntry != null)
        {
            StoryEntry best = GetHighestPriority(candidates);
            if (best.priority <= currentEntry.priority) return;
            StopNarration();
            PlayEntry(best);
            return;
        }

        // Pick one: if multiple candidates, pick random (for recurring lines)
        if (candidates.Count == 1)
        {
            PlayEntry(candidates[0]);
        }
        else
        {
            // Among highest priority candidates, pick random
            StoryEntry best = GetHighestPriority(candidates);
            List<StoryEntry> topCandidates = candidates.FindAll(e => e.priority == best.priority);
            PlayEntry(topCandidates[UnityEngine.Random.Range(0, topCandidates.Count)]);
        }
    }

    public void PlayEntry(StoryEntry entry)
    {
        if (entry == null || entry.audioClip == null) return;

        if (IsPlaying)
            StopNarration();

        currentEntry = entry;
        narrationSource.clip = entry.audioClip;
        narrationSource.volume = narrationVolume;
        narrationSource.Play();

        if (entry.playOnce)
            playedEntries.Add(entry.entryId);

        DuckMusic();
        OnNarrationStarted?.Invoke(entry);
    }

    public void StopNarration()
    {
        if (narrationSource.isPlaying)
            narrationSource.Stop();

        FinishNarration();
    }

    public void SetNarrationVolume(float volume)
    {
        narrationVolume = Mathf.Clamp01(volume);
        if (narrationSource.isPlaying)
            narrationSource.volume = narrationVolume;
    }

    public void ResetPlayedEntries()
    {
        playedEntries.Clear();
    }

    private void FinishNarration()
    {
        StoryEntry finished = currentEntry;
        currentEntry = null;
        RestoreMusic();

        if (finished != null)
            OnNarrationFinished?.Invoke(finished);
    }

    private void DuckMusic()
    {
        if (AudioManager.Instance == null) return;
        if (isDucking) return;

        isDucking = true;
        originalMusicVolume = AudioManager.Instance.MusicVolume;
        AudioManager.Instance.SetMusicVolume(musicDuckVolume);
    }

    private void RestoreMusic()
    {
        if (AudioManager.Instance == null) return;
        if (!isDucking) return;

        isDucking = false;
        AudioManager.Instance.SetMusicVolume(originalMusicVolume);
    }

    private StoryEntry GetHighestPriority(List<StoryEntry> entries)
    {
        StoryEntry best = entries[0];
        for (int i = 1; i < entries.Count; i++)
        {
            if (entries[i].priority > best.priority)
                best = entries[i];
        }
        return best;
    }
}
