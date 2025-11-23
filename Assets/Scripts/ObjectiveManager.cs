using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SimpleObjectiveEntry
{
    [Tooltip("Text shown in UI for this objective.")]
    public string description = "New objective";

    [Tooltip("Name of the global boolean flag to watch in GameGlobals (case-sensitive).")]
    public string globalFlagName = "flag_name_here";

    [Tooltip("Optional per-object TMP field. If set, the manager will update this field individually.")]
    public TMP_Text perObjectiveTMP;

    [HideInInspector] public bool completed = false;
    [HideInInspector] public float completedAt = 0f;

    public UnityEvent onComplete;
}

public class ObjectiveManager : MonoBehaviour
{
    [Header("Core")]
    public Transform player; // optional, only for debug gizmos
    [Tooltip("If you prefer one combined text field, assign it here. Otherwise assign per-object TMPs on each entry.")]
    public TMP_Text combinedListTMP;
    public GameObject objectivePanel;

    [Header("Objectives")]
    public List<SimpleObjectiveEntry> objectives = new List<SimpleObjectiveEntry>();

    [Header("Behavior")]
    public bool autoStart = true;
    public bool hidePanelWhenAllDone = false;
    public float pollingInterval = 0.25f; // seconds (uses unscaled waiting now)

    [Header("Events")]
    public UnityEvent onAllComplete;

    // runtime
    bool running = false;
    Coroutine pollCoroutine = null;

    void Awake()
    {
        // ensure UI initial state (hidden if not autoStart)
        if (objectivePanel != null)
            objectivePanel.SetActive(autoStart);
        RefreshUI();
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
        // preserve original behavior: auto start on first scene entry
        if (autoStart)
        {
            // slight delay to allow scene initialization
            StartCoroutine(DelayedBeginOneFrame());
        }
    }

    IEnumerator DelayedBeginOneFrame()
    {
        yield return null;
        if (!running) Begin();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // If this object belongs to the scene that was just loaded and autoStart is true,
        // ensure the objective panel is set and Begin() is called.
        if (!autoStart) return;
        if (gameObject.scene != scene) return;

        // ensure panel shows the correct initial state
        if (objectivePanel != null)
            objectivePanel.SetActive(true);

        // small delay to allow other initialization
        StartCoroutine(DelayedBeginOneFrame());
    }

    public void Begin()
    {
        if (running) return;
        running = true;
        if (objectivePanel != null) objectivePanel.SetActive(true);
        RefreshUI();
        pollCoroutine = StartCoroutine(PollRoutineUnscaled());
    }

    public void Stop()
    {
        running = false;
        if (pollCoroutine != null)
        {
            StopCoroutine(pollCoroutine);
            pollCoroutine = null;
        }
    }

    // Use unscaled waits so polling continues even if Time.timeScale == 0 during transitions
    IEnumerator PollRoutineUnscaled()
    {
        while (running)
        {
            bool anyNew = false;
            for (int i = 0; i < objectives.Count; i++)
            {
                var o = objectives[i];
                if (o == null) continue;
                if (o.completed) continue;

                if (!string.IsNullOrEmpty(o.globalFlagName) && GameGlobals.GetFlag(o.globalFlagName))
                {
                    MarkComplete(o);
                    anyNew = true;
                }
            }

            if (AllComplete())
            {
                running = false;
                onAllComplete?.Invoke();
                if (hidePanelWhenAllDone && objectivePanel != null) objectivePanel.SetActive(false);
                pollCoroutine = null;
                yield break;
            }

            if (anyNew) RefreshUI();

            // wait unscaled so pause/scene transitions don't stop objective checks
            float elapsed = 0f;
            while (elapsed < pollingInterval)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        pollCoroutine = null;
    }

    void MarkComplete(SimpleObjectiveEntry o)
    {
        if (o == null) return;
        o.completed = true;
        o.completedAt = Time.time;

        if (o.perObjectiveTMP != null)
        {
            o.perObjectiveTMP.fontStyle |= TMPro.FontStyles.Strikethrough;
        }

        o.onComplete?.Invoke();
    }

    void RefreshUI()
    {
        foreach (var o in objectives)
        {
            if (o == null) continue;
            if (o.perObjectiveTMP != null)
            {
                o.perObjectiveTMP.text = o.description;
                if (o.completed) o.perObjectiveTMP.fontStyle |= TMPro.FontStyles.Strikethrough;
                else o.perObjectiveTMP.fontStyle &= ~TMPro.FontStyles.Strikethrough;
            }
        }

        if (combinedListTMP != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < objectives.Count; i++)
            {
                var o = objectives[i];
                if (o == null) continue;
                if (o.completed)
                    sb.AppendLine("<s>" + o.description + "</s>");
                else
                    sb.AppendLine(o.description);
            }
            combinedListTMP.text = sb.ToString().TrimEnd('\n');
        }
    }

    bool AllComplete()
    {
        foreach (var o in objectives) if (!o.completed) return false;
        return true;
    }

    // Optional helper to allow other scripts to mark an objective by name
    public bool CompleteByFlagName(string flagName)
    {
        foreach (var o in objectives)
        {
            if (o == null) continue;
            if (o.globalFlagName == flagName)
            {
                if (!o.completed) MarkComplete(o);
                RefreshUI();
                return true;
            }
        }
        return false;
    }
    public void ResetAllObjectives()
    {
        if (objectives == null) return;
        foreach (var o in objectives)
        {
            if (o == null) continue;
            o.completed = false;
            o.completedAt = 0f;
            if (o.perObjectiveTMP != null)
            {
                // remove strikethrough if applied
                o.perObjectiveTMP.fontStyle &= ~TMPro.FontStyles.Strikethrough;
                o.perObjectiveTMP.text = o.description;
            }
        }
        RefreshUI();
        // stop any running poll and mark not running — Begin() will start fresh if needed
        Stop();
        // Show the panel ready for a new run if appropriate
        if (objectivePanel != null) objectivePanel.SetActive(true);
    }
}

