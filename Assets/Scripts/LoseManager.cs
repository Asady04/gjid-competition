using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// LoseManager (safe):
/// - Immediately sets player_dead and disables enemy shooting via canEnemyShoot flag
/// - Keeps Time.timeScale == 0 while UI plays and while scene loads
/// - Only restores timeScale after new scene has finished loading
/// - Clears flags after scene load
/// </summary>
public class LoseManager : MonoBehaviour
{
    [Header("Canvas Elements")]
    public CanvasGroup fadePanel;
    public TMP_Text messageText;
    public Image illustrationImage;

    [Header("Lose Config")]
    public float fadeDuration = 1.0f;
    public float messageDelay = 0.8f;
    public float illustrationDelay = 1.0f;
    public float afterIllustrationDelay = 1.2f;
    [Tooltip("Name of scene to load. Must be added to Build Settings.")]
    public string nextSceneName = "MainMenu";

    [Header("Flags")]
    public string deathFlag = "player_dead";      // used elsewhere
    public string canEnemyShootFlag = "canEnemyShoot"; // false when lose sequence active

    [Header("Audio")]
    public AudioClip loseSFX;
    public AudioSource audioSource;

    bool hasLost = false;
    static LoseManager _instance;

    void Awake()
    {
        // singleton guard
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject);

        if (fadePanel != null) fadePanel.alpha = 0f;
        if (messageText != null) messageText.gameObject.SetActive(false);
        if (illustrationImage != null) illustrationImage.gameObject.SetActive(false);

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
    }

    void Update()
    {
        if (!hasLost && GameGlobals.GetFlag(deathFlag))
        {
            TriggerLose();
        }
    }

    public void TriggerLose()
    {
        if (hasLost) return;
        hasLost = true;

        Debug.Log("[LoseManager] TriggerLose() - starting lose sequence");

        // 1) set global flags: lock enemy shooting and mark player dead
        GameGlobals.SetFlag(deathFlag, true);
        GameGlobals.SetFlag(canEnemyShootFlag, false);
        Debug.Log("[LoseManager] Set player_dead=true and canEnemyShoot=false");

        // 2) play SFX
        if (loseSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(loseSFX);
        }

        // 3) freeze gameplay immediately
        Time.timeScale = 0f;
        Debug.Log("[LoseManager] Time.timeScale set to 0");

        // 4) disable enemies (hard stop)
        DisableAllEnemies();

        // 5) run UI sequence using unscaled time, then load next scene while still frozen
        StartCoroutine(LoseSequenceAndLoad());
    }

    private void DisableAllEnemies()
    {
        int disabled = 0;
        MonoBehaviour[] all = FindObjectsOfType<MonoBehaviour>();
        foreach (var m in all)
        {
            if (m == null) continue;
            if (m.GetType().Name == "EnemyAI") // adjust if your enemy script name differs
            {
                m.enabled = false;
                disabled++;
            }
        }
        Debug.Log($"[LoseManager] Disabled {disabled} EnemyAI components (if any).");
    }

    IEnumerator LoseSequenceAndLoad()
    {
        Debug.Log("[LoseManager] LoseSequenceAndLoad started.");

        // fade (unscaled)
        if (fadePanel != null)
        {
            float t = 0f;
            fadePanel.alpha = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                fadePanel.alpha = Mathf.Clamp01(t / fadeDuration);
                yield return null;
            }
            fadePanel.alpha = 1f;
        }

        // show message
        yield return new WaitForSecondsRealtime(messageDelay);
        if (messageText != null && !messageText.gameObject.activeSelf)
            messageText.gameObject.SetActive(true);

        // show illustration
        yield return new WaitForSecondsRealtime(illustrationDelay);
        if (illustrationImage != null && !illustrationImage.gameObject.activeSelf)
        {
            if (illustrationImage.sprite == null)
                Debug.LogWarning("[LoseManager] illustrationImage.sprite is null. Assign sprite in inspector.");
            illustrationImage.gameObject.SetActive(true);
            illustrationImage.transform.SetAsLastSibling();
            illustrationImage.canvasRenderer.SetAlpha(1f);
        }

        // wait a bit
        yield return new WaitForSecondsRealtime(afterIllustrationDelay);

        // begin async load while still frozen (timeScale==0)
        Debug.Log($"[LoseManager] Begin async load of '{nextSceneName}' (still frozen).");
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("[LoseManager] nextSceneName is empty - cannot load scene.");
            yield break;
        }

        // validate build list
        bool found = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == nextSceneName) { found = true; break; }
        }
        if (!found)
        {
            Debug.LogError($"[LoseManager] Scene '{nextSceneName}' not in Build Settings.");
            yield break;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
        if (op == null)
        {
            Debug.LogError($"[LoseManager] LoadSceneAsync returned null for '{nextSceneName}'.");
            yield break;
        }

        // optionally prevent activation until we choose: default allows immediate activation
        // wait until loading completes
        while (!op.isDone)
        {
            Debug.Log($"[LoseManager] Scene load progress: {op.progress:F2}");
            yield return null; // still unscaled because timeScale=0 (coroutines continue)
        }

        Debug.Log("[LoseManager] Scene loaded. Restoring Time.timeScale and clearing flags.");

        // restore time and clear flags AFTER new scene is loaded
        Time.timeScale = 1f;
        GameGlobals.SetFlag(deathFlag, false);
        GameGlobals.SetFlag(canEnemyShootFlag, true);

        // ensure any EnemyAI in the new scene is allowed to shoot again only after they initialize
        yield break;
    }
}
