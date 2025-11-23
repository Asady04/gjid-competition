using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LoseManager : MonoBehaviour
{
    [Header("Canvas Elements")]
    public CanvasGroup fadePanel;
    public TMP_Text messageText;
    public Image illustrationImage;

    [Header("Game Over Panel (will be disabled automatically)")]
    public GameObject gameOverPanel; // <-- ADD THIS IN INSPECTOR

    [Header("Lose Config")]
    public float fadeDuration = 1.0f;
    public float messageDelay = 0.8f;
    public float illustrationDelay = 1.0f;
    public float afterIllustrationDelay = 1.2f;
    public string nextSceneName = "MainMenu";

    [Header("Flag Settings")]
    public string deathFlag = "player_dead";
    public string canEnemyShootFlag = "canEnemyShoot";

    [Header("Audio")]
    public AudioClip loseSFX;
    public AudioSource audioSource;

    bool hasLost = false;

    static LoseManager _instance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        EnsureGameOverHidden();

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

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureGameOverHidden();

        Time.timeScale = 1f;
        GameGlobals.SetFlag(deathFlag, false);
        GameGlobals.SetFlag(canEnemyShootFlag, true);

        hasLost = false;
    }

    private void EnsureGameOverHidden()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
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

        EnsureGameOverHidden();

        GameGlobals.SetFlag(deathFlag, true);
        GameGlobals.SetFlag(canEnemyShootFlag, false);

        if (loseSFX != null && audioSource != null)
            audioSource.PlayOneShot(loseSFX);

        Time.timeScale = 0f;

        DisableAllEnemies();

        StartCoroutine(LoseSequence());
    }

    private void DisableAllEnemies()
    {
        foreach (var m in FindObjectsOfType<MonoBehaviour>())
        {
            if (m.GetType().Name == "EnemyAI")
                m.enabled = false;
        }
    }

    IEnumerator LoseSequence()
    {
        if (fadePanel != null)
        {
            float t = 0f;
            fadePanel.alpha = 0f;
            fadePanel.gameObject.SetActive(true);
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                fadePanel.alpha = Mathf.Clamp01(t / fadeDuration);
                yield return null;
            }
        }

        yield return new WaitForSecondsRealtime(messageDelay);
        if (messageText != null)
            messageText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(illustrationDelay);
        if (illustrationImage != null)
        {
            illustrationImage.gameObject.SetActive(true);
            illustrationImage.canvasRenderer.SetAlpha(1);
        }

        // Activate Game Over Panel ONLY here
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        yield return new WaitForSecondsRealtime(afterIllustrationDelay);

        Time.timeScale = 1f;

        SceneManager.LoadScene(nextSceneName);

        EnsureGameOverHidden();
    }
}
