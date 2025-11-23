using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Robust PauseMenu:
/// - Toggles pause with Escape
/// - Resume() and GoHome() for buttons
/// - Resets itself when scenes load so panel won't remain open across scenes
/// - Uses GameGlobals flag "canEnemyShoot" to stop enemies while paused
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup pausePanel;        // assign CanvasGroup of your pause panel
    public Button resumeButton;           // optional
    public Button homeButton;             // optional

    [Header("Config")]
    public string mainMenuSceneName = "MainMenu";
    public string canEnemyShootFlag = "canEnemyShoot";

    bool isPaused = false;

    void Awake()
    {
        // safety: ensure panel exists
        if (pausePanel == null)
            Debug.LogError("[PauseMenu] pausePanel not assigned!");

        // make sure initial state is consistent
        EnsurePanelClosed();

        // wire buttons safely (avoids duplicates)
        if (resumeButton != null) { resumeButton.onClick.RemoveAllListeners(); resumeButton.onClick.AddListener(Resume); }
        if (homeButton != null)   { homeButton.onClick.RemoveAllListeners();   homeButton.onClick.AddListener(GoHome); }

        // Ensure enemies allowed by default
        if (!GameGlobals.GetFlag(canEnemyShootFlag))
            GameGlobals.SetFlag(canEnemyShootFlag, true);
    }

    void OnEnable()
    {
        // subscribe to scene load so we reset when returning to gameplay or leaving
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // whenever a new scene loads, guarantee the pause UI is closed and time is normal
        // this prevents the panel from remaining open when you re-enter gameplay
        EnsurePanelClosed();

        // make sure gameplay runs normally
        Time.timeScale = 1f;
        GameGlobals.SetFlag(canEnemyShootFlag, true);
        isPaused = false;
    }

    void EnsurePanelClosed()
    {
        if (pausePanel != null)
        {
            pausePanel.gameObject.SetActive(false);
            pausePanel.alpha = 0f;
            pausePanel.interactable = false;
            pausePanel.blocksRaycasts = false;
        }
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;

        if (pausePanel != null)
        {
            pausePanel.gameObject.SetActive(true);
            pausePanel.alpha = 1f;
            pausePanel.interactable = true;
            pausePanel.blocksRaycasts = true;
            pausePanel.transform.SetAsLastSibling(); // bring UI to front
        }

        Time.timeScale = 0f;
        GameGlobals.SetFlag(canEnemyShootFlag, false);
        // optionally pause audio:
        // AudioListener.pause = true;
    }

    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;

        if (pausePanel != null)
        {
            pausePanel.alpha = 0f;
            pausePanel.interactable = false;
            pausePanel.blocksRaycasts = false;
            pausePanel.gameObject.SetActive(false);
        }

        Time.timeScale = 1f;
        GameGlobals.SetFlag(canEnemyShootFlag, true);
        // AudioListener.pause = false;
    }

    public void GoHome()
    {
        // restore normal state and load menu
        Time.timeScale = 1f;
        GameGlobals.SetFlag(canEnemyShootFlag, true);

        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            Debug.LogWarning("[PauseMenu] mainMenuSceneName is empty.");
    }
}
