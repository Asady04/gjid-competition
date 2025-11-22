using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameMusic;
    [SerializeField] private AudioClip buttonClickSFX;

    private static AudioManager instance;

    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SFXVolume";

    private float musicVolume = 0.5f;
    private float sfxVolume = 0.7f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Load volume dari PlayerPrefs
        musicVolume = PlayerPrefs.GetFloat(MUSIC_KEY, 0.5f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_KEY, 0.7f);

        // Apply ke AudioSource
        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.volume = musicVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.loop = false;
            sfxSource.volume = sfxVolume;
        }
    }

    private void Start()
    {
        if (mainMenuMusic != null)
            PlayMusic(mainMenuMusic);
    }

    // =============================
    // MUSIC CONTROL
    // =============================
    public static void PlayMusic(AudioClip clip)
    {
        if (instance == null || instance.musicSource == null || clip == null)
            return;

        if (instance.musicSource.clip == clip && instance.musicSource.isPlaying)
            return;

        instance.musicSource.clip = clip;
        instance.musicSource.Play();
    }

    public static void StopMusic()
    {
        if (instance != null)
            instance.musicSource.Stop();
    }

    // =============================
    // SFX CONTROL
    // =============================
    public static void PlaySFX(AudioClip clip)
    {
        if (instance != null && instance.sfxSource != null)
            instance.sfxSource.PlayOneShot(clip);
    }

    public static void PlayButtonClick()
    {
        PlaySFX(instance.buttonClickSFX);
    }

    // =============================
    // VOLUME CONTROL + SAVE
    // =============================
    public static void SetMusicVolume(float volume)
    {
        if (instance == null)
            return;

        instance.musicVolume = Mathf.Clamp01(volume);
        instance.musicSource.volume = instance.musicVolume;

        PlayerPrefs.SetFloat(MUSIC_KEY, instance.musicVolume);
        PlayerPrefs.Save();
    }

    public static void SetSFXVolume(float volume)
    {
        if (instance == null)
            return;

        instance.sfxVolume = Mathf.Clamp01(volume);
        instance.sfxSource.volume = instance.sfxVolume;

        PlayerPrefs.SetFloat(SFX_KEY, instance.sfxVolume);
        PlayerPrefs.Save();
    }

    // Getter untuk Slider
    public static float GetMusicVolume() => instance ? instance.musicVolume : 0.5f;
    public static float GetSFXVolume() => instance ? instance.sfxVolume : 0.7f;
}
