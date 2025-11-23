using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class CutSceneController : MonoBehaviour
{
    [Header("UI references (assign)")]
    public Image artworkImage;                 // UI image to show illustrations
    public CanvasGroup artworkCanvasGroup;     // for fade; will be auto-created if null
    public TMP_Text captionText;               // TextMeshProUGUI for subtitle text
    public CanvasGroup captionCanvasGroup;     // for subtitle fade (auto-created if null)

    [Header("Slides (lists must align)")]
    public List<Sprite> illustrations = new List<Sprite>();
    public List<string> captions = new List<string>();
    public List<AudioClip> sfxPerSlide = new List<AudioClip>();    // optional SFX per slide (can be null / shorter)
    public List<float> perSlideDurations = new List<float>();      // optional per-slide durations (if missing, uses holdDuration)

    [Header("Audio")]
    public AudioClip backgroundMusic;          // optional BGM
    [Tooltip("Optional: dedicated AudioSource for bgm. Will create one if null.")]
    public AudioSource bgmSource;
    [Tooltip("Optional: AudioSource to play SFX. Will create one if null.")]
    public AudioSource sfxSource;
    [Tooltip("A volume multiplier for SFX (0..1).")]
    public float sfxVolume = 1f;
    [Tooltip("A volume multiplier for BGM (0..1).")]
    public float bgmVolume = 0.7f;

    [Header("Timing & transitions")]
    public float firstFadeInDuration = 1.0f;   // fade-in for the first slide
    public float holdDuration = 3.0f;          // default duration for each slide
    public float transitionDuration = 0.25f;   // fade-out / fade-in between slides
    public float captionFadeDuration = 0.35f;  // fade for caption

    [Header("Behavior")]
    public bool fadeBetweenSlides = true;      // crossfade or instant
    public bool autoStart = true;              // auto play on Start
    public bool clickToAdvance = true;         // click or key to advance to next slide
    public KeyCode advanceKey = KeyCode.Space; // keyboard advance
    public KeyCode skipKey = KeyCode.Escape;   // key to skip entire cutscene
    public Button skipButton;                  // optional UI button to skip entirely

    [Header("Callbacks")]
    public UnityEvent onCutsceneStart;
    public UnityEvent onCutsceneEnd;

    // internal
    List<Sprite> loadedSprites;
    AudioSource internalBgmSource;
    AudioSource internalSfxSource;
    bool isPlaying = false;

    void Awake()
    {
        // ensure artwork image
        if (artworkImage == null)
        {
            Debug.LogError("CutsceneController: artworkImage is not assigned.");
            enabled = false;
            return;
        }

        // ensure canvas groups
        if (artworkCanvasGroup == null)
        {
            artworkCanvasGroup = artworkImage.GetComponent<CanvasGroup>();
            if (artworkCanvasGroup == null)
                artworkCanvasGroup = artworkImage.gameObject.AddComponent<CanvasGroup>();
        }

        if (captionCanvasGroup == null && captionText != null)
        {
            captionCanvasGroup = captionText.GetComponent<CanvasGroup>();
            if (captionCanvasGroup == null)
                captionCanvasGroup = captionText.gameObject.AddComponent<CanvasGroup>();
        }

        // audio sources: create or use supplied
        if (bgmSource == null)
        {
            GameObject bgmGO = new GameObject("Cutscene_BGM");
            bgmGO.transform.SetParent(transform, false);
            internalBgmSource = bgmGO.AddComponent<AudioSource>();
            internalBgmSource.playOnAwake = false;
            internalBgmSource.loop = true;
            internalBgmSource.spatialBlend = 0f;
            bgmSource = internalBgmSource;
        }

        if (sfxSource == null)
        {
            GameObject sfxGO = new GameObject("Cutscene_SFX");
            sfxGO.transform.SetParent(transform, false);
            internalSfxSource = sfxGO.AddComponent<AudioSource>();
            internalSfxSource.playOnAwake = false;
            internalSfxSource.loop = false;
            internalSfxSource.spatialBlend = 0f;
            sfxSource = internalSfxSource;
        }

        // ensure lists align (captions and durations)
        int maxCount = Mathf.Max(illustrations.Count, captions.Count, sfxPerSlide.Count, perSlideDurations.Count);
        // stretch lists to maxCount with defaults to avoid out-of-range checks
        while (captions.Count < maxCount) captions.Add("");
        while (sfxPerSlide.Count < maxCount) sfxPerSlide.Add(null);
        while (perSlideDurations.Count < maxCount) perSlideDurations.Add(holdDuration);

        if (skipButton != null)
            skipButton.onClick.AddListener(() => { if (isPlaying) StopCoroutineAndEnd(); });
    }

    void Start()
    {
        if (autoStart)
            StartCutscene();
    }

    public void StartCutscene()
    {
        if (isPlaying) return;
        StartCoroutine(RunCutscene());
    }

    IEnumerator RunCutscene()
    {
        // basic validation
        if (illustrations.Count == 0)
        {
            Debug.LogWarning("CutsceneController: No illustrations assigned.");
            yield break;
        }

        isPlaying = true;
        onCutsceneStart?.Invoke();

        // start bgm
        if (backgroundMusic != null && bgmSource != null)
        {
            bgmSource.clip = backgroundMusic;
            bgmSource.volume = bgmVolume;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        // ensure initial alpha states
        artworkCanvasGroup.alpha = 0f;
        if (captionCanvasGroup != null) captionCanvasGroup.alpha = 0f;

        // iterate slides
        for (int i = 0; i < illustrations.Count; i++)
        {
            Sprite spr = illustrations[i];
            string caption = i < captions.Count ? captions[i] : "";
            AudioClip sfx = i < sfxPerSlide.Count ? sfxPerSlide[i] : null;
            float slideDuration = (i < perSlideDurations.Count && perSlideDurations[i] > 0f) ? perSlideDurations[i] : holdDuration;

            // set sprite & caption (but keep alpha 0 until fade)
            artworkImage.sprite = spr;
            artworkImage.SetNativeSize();

            if (captionText != null) captionText.text = caption;

            // play slide sfx (non-blocking)
            if (sfx != null && sfxSource != null)
            {
                sfxSource.volume = sfxVolume;
                sfxSource.PlayOneShot(sfx);
            }

            // FIRST SLIDE: fade in from 0 -> 1 using firstFadeInDuration
            if (i == 0 && firstFadeInDuration > 0f)
            {
                yield return StartCoroutine(FadeCanvasGroup(artworkCanvasGroup, 0f, 1f, firstFadeInDuration));
                // caption fade-in
                if (captionCanvasGroup != null && captionFadeDuration > 0f)
                    yield return StartCoroutine(FadeCanvasGroup(captionCanvasGroup, 0f, 1f, captionFadeDuration));
                else if (captionCanvasGroup != null)
                    captionCanvasGroup.alpha = 1f;

                // wait for slide unless user advances
                yield return StartCoroutine(WaitForAdvanceOrTimeout(slideDuration));
            }
            else
            {
                if (fadeBetweenSlides && transitionDuration > 0f)
                {
                    // crossfade: fade out -> swap -> fade in
                    yield return StartCoroutine(FadeCanvasGroup(artworkCanvasGroup, 1f, 0f, transitionDuration * 0.5f));
                    // swap sprite already set above
                    yield return StartCoroutine(FadeCanvasGroup(artworkCanvasGroup, 0f, 1f, transitionDuration * 0.5f));

                    // caption fade
                    if (captionCanvasGroup != null)
                    {
                        yield return StartCoroutine(FadeCanvasGroup(captionCanvasGroup, 0f, 1f, captionFadeDuration));
                    }

                    yield return StartCoroutine(WaitForAdvanceOrTimeout(slideDuration));
                }
                else
                {
                    // instant swap
                    artworkCanvasGroup.alpha = 1f;
                    if (captionCanvasGroup != null) captionCanvasGroup.alpha = 1f;
                    yield return StartCoroutine(WaitForAdvanceOrTimeout(slideDuration));
                }
            }

            // before next slide: fade caption out quickly to prepare
            if (captionCanvasGroup != null)
                yield return StartCoroutine(FadeCanvasGroup(captionCanvasGroup, captionCanvasGroup.alpha, 0f, 0.12f));
        }

        // cutscene finished: fade out and stop BGM
        yield return StartCoroutine(FadeCanvasGroup(artworkCanvasGroup, artworkCanvasGroup.alpha, 0f, 0.6f));
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();

        isPlaying = false;
        onCutsceneEnd?.Invoke();
    }

    // Wait loop that returns early when click/key pressed (advance) or skip key pressed (end cutscene)
    IEnumerator WaitForAdvanceOrTimeout(float timeout)
    {
        float t = 0f;
        bool advanced = false;

        while (t < timeout)
        {
            // advance by click or key
            if (clickToAdvance && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(advanceKey)))
            {
                advanced = true;
                break;
            }
            // skip entire cutscene
            if (Input.GetKeyDown(skipKey))
            {
                StopCoroutineAndEnd();
                yield break;
            }

            t += Time.deltaTime;
            yield return null;
        }

        // If timeout ended naturally, return normally (next slide).
        yield break;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null)
            yield break;
        float t = 0f;
        cg.alpha = from;
        if (duration <= 0f)
        {
            cg.alpha = to;
            yield break;
        }
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        cg.alpha = to;
    }

    void StopCoroutineAndEnd()
    {
        StopAllCoroutines();
        // Hide visuals
        if (artworkCanvasGroup != null) artworkCanvasGroup.alpha = 0f;
        if (captionCanvasGroup != null) captionCanvasGroup.alpha = 0f;
        if (bgmSource != null && bgmSource.isPlaying) bgmSource.Stop();
        isPlaying = false;
        onCutsceneEnd?.Invoke();
    }
}
