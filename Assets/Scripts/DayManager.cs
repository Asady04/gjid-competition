using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

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

    // Public wrapper to start the sequence from other scripts
    public void StartDay()
    {
        StartCoroutine(RunDaySequence());
    }

    private void Start()
    {
        // ensure UI is prepared
        if (dayPanelCanvasGroup != null)
            dayPanelCanvasGroup.gameObject.SetActive(true);

        if (dayText != null)
            dayText.text = dayLabel;

        if (autoStart)
            StartDay();
    }

    public IEnumerator RunDaySequence()
    {
        if (dayPanelCanvasGroup == null)
        {
            Debug.LogError("DayManager: dayPanelCanvasGroup not assigned.");
            yield break;
        }

        // Prepare day text alpha (TextMeshPro) if available
        if (dayText != null)
        {
            Color tc = dayText.color;
            tc.a = 0f;
            dayText.color = tc;
        }

        // Ensure panel starts transparent
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
            // Call Begin() to show objective UI and start tracking
            objectiveManager.Begin();
            onObjectiveShown?.Invoke();
        }

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
