using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    public enum TrackType
    {
        None,
        MainMenu,
        Cutscene,
        Gameplay,
        Custom // use PlayCustom(name) for named clips in the dictionary
    }

    [Header("Clips (assign in inspector)")]
    public AudioClip mainMenuClip;
    public AudioClip cutsceneClip;
    public AudioClip gameplayClip;

    [Tooltip("Optional named custom clips")]
    public List<NamedClip> customClips = new List<NamedClip>();

    [Header("Playback")]
    [Range(0f, 1f)] public float volume = 0.8f;
    public bool playOnStart = true;
    public TrackType startTrack = TrackType.MainMenu;

    [Header("Crossfade")]
    [Tooltip("Seconds to crossfade when switching tracks")]
    public float crossfadeTime = 1.0f;

    [Header("Auto behavior")]
    [Tooltip("If true, the manager will automatically switch to the mapped track when a new scene loads")]
    public bool autoSwitchOnSceneLoad = true;

    [Tooltip("Map specific scene names to TrackType. If a scene name is present it will be used to choose a track (case-insensitive).")]
    public List<SceneMapEntry> sceneMappings = new List<SceneMapEntry>();

    // Internal
    AudioSource audioSource;
    Coroutine crossfadeCoroutine;
    Dictionary<string, AudioClip> customClipDict;
    TrackType currentTrack = TrackType.None;

    [System.Serializable]
    public class NamedClip
    {
        public string name;
        public AudioClip clip;
    }

    [System.Serializable]
    public class SceneMapEntry
    {
        public string sceneName;
        public TrackType mappedTrack = TrackType.None;
        [Tooltip("If mappedTrack is Custom, set the customClipName to pick a clip from customClips")]
        public string customClipName;
    }

    void Awake()
    {
        // Singleton handling
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = volume;

        // Build dictionary for quick lookup of custom clips
        customClipDict = new Dictionary<string, AudioClip>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var nc in customClips)
        {
            if (nc != null && !string.IsNullOrEmpty(nc.name) && nc.clip != null)
                customClipDict[nc.name] = nc.clip;
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        if (playOnStart)
        {
            // try to auto-start the requested track
            PlayTrack(startTrack);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!autoSwitchOnSceneLoad) return;

        // find mapping (case-insensitive)
        SceneMapEntry entry = sceneMappings.Find(e => string.Equals(e.sceneName, scene.name, System.StringComparison.OrdinalIgnoreCase));
        if (entry != null)
        {
            if (entry.mappedTrack == TrackType.Custom && !string.IsNullOrEmpty(entry.customClipName))
            {
                PlayCustom(entry.customClipName);
            }
            else
            {
                PlayTrack(entry.mappedTrack);
            }
            return;
        }

        // default behavior if no mapping: attempt sensible default based on scene name keywords
        string lower = scene.name.ToLowerInvariant();
        if (lower.Contains("menu"))
            PlayTrack(TrackType.MainMenu);
        else if (lower.Contains("cutscene") || lower.Contains("cinematic"))
            PlayTrack(TrackType.Cutscene);
        else if (lower.Contains("game") || lower.Contains("level") || lower.Contains("day"))
            PlayTrack(TrackType.Gameplay);
        // else leave current track running
    }

    /// <summary>
    /// Play one of the named track types (crossfades smoothly). TrackType.None stops playback.
    /// </summary>
    public void PlayTrack(TrackType track)
    {
        AudioClip next = null;
        switch (track)
        {
            case TrackType.MainMenu: next = mainMenuClip; break;
            case TrackType.Cutscene: next = cutsceneClip; break;
            case TrackType.Gameplay: next = gameplayClip; break;
            case TrackType.Custom: next = null; break;
            case TrackType.None:
            default:
                Stop();
                currentTrack = TrackType.None;
                return;
        }

        if (next == audioSource.clip && audioSource.isPlaying)
        {
            currentTrack = track;
            return; // already playing the requested clip
        }

        StartCrossfadeTo(next);
        currentTrack = track;
    }

    /// <summary>
    /// Play a custom named clip (from customClips list).
    /// </summary>
    public void PlayCustom(string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return;

        if (customClipDict.TryGetValue(clipName, out AudioClip clip))
        {
            if (clip == audioSource.clip && audioSource.isPlaying) return;
            StartCrossfadeTo(clip);
            currentTrack = TrackType.Custom;
        }
        else
        {
            Debug.LogWarning($"BGMManager: custom clip '{clipName}' not found.");
        }
    }

    /// <summary>
    /// Immediately set a clip and play it (no crossfade).
    /// </summary>
    public void SetClipAndPlayImmediate(AudioClip clip, float vol = -1f, bool loop = true)
    {
        if (crossfadeCoroutine != null) { StopCoroutine(crossfadeCoroutine); crossfadeCoroutine = null; }
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = loop;
        if (vol >= 0f) audioSource.volume = vol;
        audioSource.Play();
    }

    /// <summary>
    /// Stop playback (no destroy). Use StopAndDestroy() to kill the manager.
    /// </summary>
    public void Stop()
    {
        if (crossfadeCoroutine != null) { StopCoroutine(crossfadeCoroutine); crossfadeCoroutine = null; }
        audioSource.Stop();
    }

    /// <summary>
    /// Stop playback and destroy this persistent BGMManager (useful when returning to a fresh main menu).
    /// </summary>
    public void StopAndDestroy()
    {
        Stop();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
        Destroy(gameObject);
    }

    void StartCrossfadeTo(AudioClip nextClip)
    {
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
            crossfadeCoroutine = null;
        }
        crossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(nextClip, crossfadeTime));
    }

    IEnumerator CrossfadeCoroutine(AudioClip nextClip, float duration)
    {
        float startVol = audioSource.isPlaying ? audioSource.volume : 0f;
        float t = 0f;

        // if no clip currently playing, just fade in
        if (!audioSource.isPlaying || audioSource.clip == null)
        {
            audioSource.clip = nextClip;
            audioSource.volume = 0f;
            if (nextClip != null) audioSource.Play();
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                audioSource.volume = Mathf.Lerp(0f, volume, Mathf.Clamp01(t / duration));
                yield return null;
            }
            audioSource.volume = volume;
            crossfadeCoroutine = null;
            yield break;
        }

        // Otherwise crossfade: fade out old while fading in new using a temporary source
        GameObject tempGO = new GameObject("BGM_tempPlayer");
        tempGO.transform.SetParent(transform, false);
        AudioSource temp = tempGO.AddComponent<AudioSource>();
        temp.clip = nextClip;
        temp.loop = true;
        temp.playOnAwake = false;
        temp.volume = 0f;
        temp.spatialBlend = 0f;
        temp.Play();

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / duration);
            // fade out original, fade in temp
            audioSource.volume = Mathf.Lerp(startVol, 0f, a);
            temp.volume = Mathf.Lerp(0f, volume, a);
            yield return null;
        }

        // swap: stop original and make temp the main audio source
        audioSource.Stop();
        audioSource.clip = temp.clip;
        audioSource.volume = temp.volume;
        audioSource.loop = true;
        audioSource.Play();

        // cleanup temp
        Destroy(tempGO);
        crossfadeCoroutine = null;
    }

    // Optional helper: query current track type
    public TrackType GetCurrentTrackType() => currentTrack;
}
