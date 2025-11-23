using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

[DisallowMultipleComponent]
public class PlayerInteractorUI : MonoBehaviour
{
    [Header("UI (assign the actual prompt child GameObject)")]
    public GameObject promptGameObject;      // the child UI that gets shown/hidden
    public TMP_Text promptText;                  // or TMP field if you use TextMeshPro

    [Header("Canvas")]
    public Canvas parentCanvas;              // optional, auto-find if left blank
    public Vector2 screenOffset = new Vector2(0, 80);

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Movement detection / anti-blink")]
    [Tooltip("Player considered stopped when speed (units/sec) <= this value")]
    public float stopThreshold = 0.04f;         // low threshold for 'stopped'
    [Tooltip("Player must be stopped for this many seconds before showing 'E stay'")]
    public float stopDelay = 0.18f;             // dwell time before showing 'stay'
    [Tooltip("If speed exceeds this value (higher than stopThreshold), hide 'E stay' immediately")]
    public float moveHideThreshold = 0.12f;     // hysteresis upper threshold

    [Header("Fade (optional)")]
    [Tooltip("Duration to fade the prompt in/out. Set to 0 to disable fade.")]
    public float fadeDuration = 0.12f;

    [Header("Debug")]
    public bool debugLogs = false;

    // internals
    PlayerInteractor playerInteractor;
    Transform playerTransform;
    RectTransform promptRect;
    RectTransform canvasRect;
    Camera uiCamera;
    Vector2 lastPlayerPos;
    float stoppedTimer = 0f;
    float playerSpeed = 0f;
    CanvasGroup promptCanvasGroup;
    Coroutine fadeCoroutine = null;

    void Awake()
    {
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            canvasRect = parentCanvas.GetComponent<RectTransform>();
            uiCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceCamera ? parentCanvas.worldCamera : Camera.main;
        }
        else uiCamera = Camera.main;

        if (promptGameObject != null)
            promptRect = promptGameObject.GetComponent<RectTransform>();

        // optionally add CanvasGroup for fade
        if (promptGameObject != null)
        {
            promptCanvasGroup = promptGameObject.GetComponent<CanvasGroup>();
            if (promptCanvasGroup == null)
                promptCanvasGroup = promptGameObject.AddComponent<CanvasGroup>();
        }
    }

    void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("[PlayerInteractorUI] Player (tag='Player') not found.");
            return;
        }

        playerTransform = playerObj.transform;
        playerInteractor = playerObj.GetComponent<PlayerInteractor>();
        lastPlayerPos = playerTransform.position;

        // ensure manager stays active, and prompt child starts hidden
        if (promptGameObject != null) promptGameObject.SetActive(false);
        if (promptCanvasGroup != null) promptCanvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (playerTransform == null || playerInteractor == null) return;

        // compute speed (units/sec) robustly
        Vector2 cur = playerTransform.position;
        float dist = Vector2.Distance(cur, lastPlayerPos);
        playerSpeed = dist / Mathf.Max(0.00001f, Time.deltaTime);
        lastPlayerPos = cur;

        // nearest NPC / canInteract info
        var info = playerInteractor.GetNearestNPCInfo();
        bool npcNearby = info.canInteract;
        bool isFollowing = NPCFollower.IsGlobalFollowing();

        // decide whether to show prompt and what message
        bool shouldShow = false;
        string message = "";

        // If not following AND npc nearby -> "follow me"
        if (!isFollowing && npcNearby)
        {
            shouldShow = true;
            message = $"{interactKey.ToString().ToUpper()} pick up";

            // reset stopped timer (we only use it for "stay")
            stoppedTimer = 0f;
        }
        // If following -> only show "stay" when player is stopped for stopDelay
        else if (isFollowing)
        {
            // if speed is below stopThreshold, accumulate stopped time
            if (playerSpeed <= stopThreshold)
            {
                stoppedTimer += Time.deltaTime;
            }
            else
            {
                // movement above moveHideThreshold resets the timer and hides quickly
                if (playerSpeed >= moveHideThreshold)
                {
                    stoppedTimer = 0f;
                }
                else
                {
                    // in between thresholds: do not immediately reset; be conservative
                    // small jitter zone: leave stoppedTimer unchanged (prevents flicker)
                }
            }

            if (stoppedTimer >= stopDelay)
            {
                shouldShow = true;
                message = $"{interactKey.ToString().ToUpper()} stay";
            }
            else
            {
                shouldShow = false;
            }
        }
        else
        {
            shouldShow = false;
            stoppedTimer = 0f;
        }

        // show/hide prompt child (never disable this manager)
        if (!shouldShow)
        {
            if (promptGameObject != null && promptGameObject.activeSelf)
            {
                if (fadeDuration > 0f && promptCanvasGroup != null)
                    StartFade(0f);
                else
                    promptGameObject.SetActive(false);
            }
            return;
        }
        else
        {
            if (promptGameObject != null && !promptGameObject.activeSelf)
            {
                promptGameObject.SetActive(true);
                if (fadeDuration > 0f && promptCanvasGroup != null)
                    StartFade(1f);
                else if (promptCanvasGroup != null) promptCanvasGroup.alpha = 1f;
            }
        }

        // update text
        if (promptText != null) promptText.text = message;

        // position near player on screen
        if (promptRect != null && canvasRect != null)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera ?? Camera.main, playerTransform.position);
            Vector2 screenWithOffset = screenPoint + screenOffset;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenWithOffset, uiCamera, out Vector2 anchored);
            promptRect.anchoredPosition = anchored;
        }

        if (debugLogs)
            Debug.Log($"[PlayerInteractorUI] speed={playerSpeed:F3} stoppedTimer={stoppedTimer:F2} show={shouldShow} msg={message}");
    }

    void StartFade(float targetAlpha)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(promptCanvasGroup, targetAlpha, fadeDuration));
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float target, float duration)
    {
        if (cg == null) yield break;
        float start = cg.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
            yield return null;
        }
        cg.alpha = target;
        if (Mathf.Approximately(target, 0f))
            promptGameObject.SetActive(false);
        fadeCoroutine = null;
    }
}
