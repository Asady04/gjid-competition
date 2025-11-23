using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// DayManager (patched):
/// - Keeps original behavior but also listens to SceneManager.sceneLoaded so it
///   will auto-start the banner each time the scene containing this GameObject loads.
/// - Prevents overlapping runs and resets UI state before playing.
/// </summary>
public class DayManager : MonoBehaviour
{
    [Header("Day banner UI")]
    [Tooltip("CanvasGroup on the full-screen panel that shows 'Day X'")]
    public CanvasGroup dayPanelCanvasGroup;
    [Tooltip("TMP text that displays 'Day 1'")]
    public TMP_Text dayText;

    [Header("Objective / Flow")]
    [Tooltip("Optional ObjectiveManager to start when the Day banner finishes.")]
    public ObjectiveManager objectiveManager;

    [Header("Timing")]
    public float fadeInDuration = 0.8f;
    public float holdDuration = 1.0f;
    public float fadeOutDuration = 0.8f;
    public float delayBeforeObjective = 0.2f;

    [Header("Behavior")]
    [Tooltip("If true, DayManager will auto-start on Start.")]
    public bool autoStart = true;
    [Tooltip("Text to show inside the day banner (e.g. 'Day 1')")]
    public string dayLabel = "Day 1";

    [Header("Events")]
    public UnityEvent onDayBannerFinished;
    public UnityEvent onObjectiveShown;

    // internal
    Coroutine runningCoroutine = null;

    void Awake()
    {
        // Ensure panel exists and in an initial state (hidden) even before Start
        PrepareUIInitialState();
    }

    private void OnEnable()
    {
        // subscribe so we know when scenes load
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Do original Start duties: set text and optionally auto-run first time.
        if (dayText != null)
            dayText.text = dayLabel;

        if (autoStart)
        {
            // Use StartCoroutine with a tiny delay to let other scene initialization finish
            StartCoroutine(DelayedAutoStart());
        }
    }

    IEnumerator DelayedAutoStart()
    {
        // wait one frame so other Awake/Start code runs first
        yield return null;

        // Avoid starting if another run is already active
        if (runningCoroutine == null)
            StartDay();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // When a scene is loaded, if this GameObject belongs to the active scene
        // and autoStart is enabled, start the day banner again.
        // This handles the "go to main menu and come back" case.
        if (!autoStart) return;

        // Only start if this GameObject's scene matches the loaded scene
        if (gameObject.scene != scene) return;

        // small delay to allow scene initialization
        StartCoroutine(StartOnSceneLoadDelay());
    }

    IEnumerator StartOnSceneLoadDelay()
    {
        yield return null; // wait one frame
        if (runningCoroutine == null)
            StartDay();
    }

    // Public wrapper to start the sequence from other scripts
    public void StartDay()
    {
        if (runningCoroutine != null)
        {
            // already running; ignore
            return;
        }
        runningCoroutine = StartCoroutine(RunDaySequence());
    }

    void PrepareUIInitialState()
    {
        // Ensure the CanvasGroup is active and hidden so we can fade in properly.
        if (dayPanelCanvasGroup != null)
        {
            dayPanelCanvasGroup.gameObject.SetActive(true);
            dayPanelCanvasGroup.alpha = 0f;
        }

        if (dayText != null)
        {
            dayText.text = dayLabel;
            Color tc = dayText.color;
            tc.a = 0f;
            dayText.color = tc;
        }
    }

    public IEnumerator RunDaySequence()
    {
        if (dayPanelCanvasGroup == null)
        {
            Debug.LogError("DayManager: dayPanelCanvasGroup not assigned.");
            runningCoroutine = null;
            yield break;
        }

        // reset UI state at start (important if run multiple times)
        if (dayText != null)
        {
            Color tc = dayText.color;
            tc.a = 0f;
            dayText.color = tc;
            dayText.text = dayLabel;
        }

        dayPanelCanvasGroup.alpha = 0f;
        dayPanelCanvasGroup.gameObject.SetActive(true);

        // Fade in panel
        yield return StartCoroutine(FadeCanvasGroup(dayPanelCanvasGroup, 0f, 1f, fadeInDuration));

        // Fade in text slightly after panel for polish
        if (dayText != null)
            yield return StartCoroutine(FadeTMPText(dayText, 0f, 1f, Mathf.Min(0.45f, fadeInDuration)));

        // Hold on screen
        yield return new WaitForSeconds(holdDuration);

        // Fade out text then panel
        if (dayText != null)
            yield return StartCoroutine(FadeTMPText(dayText, 1f, 0f, 0.25f));

        yield return StartCoroutine(FadeCanvasGroup(dayPanelCanvasGroup, 1f, 0f, fadeOutDuration));

        // Optional small delay before showing objective
        if (delayBeforeObjective > 0f) yield return new WaitForSeconds(delayBeforeObjective);

        // Banner finished — raise event
        onDayBannerFinished?.Invoke();

        // Start ObjectiveManager if assigned
        if (objectiveManager != null)
        {
            objectiveManager.Begin();
            onObjectiveShown?.Invoke();
        }

        // finished
        runningCoroutine = null;
        yield break;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
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

    IEnumerator FadeTMPText(TMP_Text txt, float from, float to, float duration)
    {
        if (txt == null) yield break;
        float t = 0f;
        Color c = txt.color;
        c.a = from;
        txt.color = c;
        if (duration <= 0f)
        {
            c.a = to; txt.color = c; yield break;
        }
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            txt.color = c;
            yield return null;
        }
        c.a = to; txt.color = c;
    }
}
