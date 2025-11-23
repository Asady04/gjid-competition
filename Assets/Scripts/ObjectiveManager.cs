using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

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
    public float pollingInterval = 0.25f;

    [Header("Events")]
    public UnityEvent onAllComplete;

    bool running = false;

    void Start()
    {
        RefreshUI();
        if (objectivePanel != null) objectivePanel.SetActive(autoStart);
        if (autoStart) Begin();
    }

    public void Begin()
    {
        if (running) return;
        running = true;
        if (objectivePanel != null) objectivePanel.SetActive(true);
        StartCoroutine(PollRoutine());
        RefreshUI();
    }

    public void Stop()
    {
        running = false;
        StopAllCoroutines();
    }

    IEnumerator PollRoutine()
    {
        while (running)
        {
            bool anyNew = false;
            for (int i = 0; i < objectives.Count; i++)
            {
                var o = objectives[i];
                // skip already completed
                if (o.completed) continue;

                // check global flag
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
                yield break;
            }

            if (anyNew) RefreshUI();
            yield return new WaitForSeconds(pollingInterval);
        }
    }

    void MarkComplete(SimpleObjectiveEntry o)
    {
        o.completed = true;
        o.completedAt = Time.time;

        // update per-object TMP if present
        if (o.perObjectiveTMP != null)
        {
            o.perObjectiveTMP.fontStyle |= FontStyles.Strikethrough;
            // optionally change color or add a checkmark here
        }

        // fire event
        o.onComplete?.Invoke();
    }

    void RefreshUI()
    {
        // update per-object TMPs
        foreach (var o in objectives)
        {
            if (o.perObjectiveTMP != null)
            {
                o.perObjectiveTMP.text = o.description;
                if (o.completed) o.perObjectiveTMP.fontStyle |= FontStyles.Strikethrough;
                else o.perObjectiveTMP.fontStyle &= ~FontStyles.Strikethrough;
            }
        }

        // update combined text
        if (combinedListTMP != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < objectives.Count; i++)
            {
                var o = objectives[i];
                if (o.completed)
                    sb.AppendLine("<s>" + o.description + "</s>"); // use TMP rich text for combined field
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

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        Gizmos.color = Color.cyan;
        foreach (var o in objectives)
        {
            if (o == null) continue;
            // If you want per-object radius gizmos add a radius field to entry and draw it here.
        }
    }
#endif

    // Optional helper to allow other scripts to mark an objective by name
    public bool CompleteByFlagName(string flagName)
    {
        foreach (var o in objectives)
        {
            if (o.globalFlagName == flagName)
            {
                if (!o.completed) MarkComplete(o);
                RefreshUI();
                return true;
            }
        }
        return false;
    }
}
