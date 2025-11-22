using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel; 

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button creditButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button backButton;  // back from Settings panel

    [Header("Play Scene Settings")]
    [SerializeField] private string playSceneName = "cutscene";
    [SerializeField] private int playSceneIndex = 1;
    [SerializeField] private bool useSceneIndex = false;

    [Header("Credit Scene Settings")]
    [SerializeField] private string creditSceneName = "CreditScene";
    [SerializeField] private int creditSceneIndex = 2;
    [SerializeField] private bool creditUseIndex = false;

    [Header("Transition Delay")]
    [SerializeField] private float transitionDelay = 0.5f;

    private void Start()
    {
        mainMenuPanel.SetActive(true);
        optionsPanel.SetActive(false);

        SetupButtons();
    }

    private void SetupButtons()
    {
        playButton.onClick.AddListener(PlayGame);
        optionsButton.onClick.AddListener(OpenOptionsPanel);
        creditButton.onClick.AddListener(OpenCreditScene);
        quitButton.onClick.AddListener(QuitGame);
        backButton.onClick.AddListener(BackToMainMenu);
    }

    // =========================
    // PLAY → PINDAH SCENE
    // =========================
    public void PlayGame()
    {
        if (useSceneIndex)
            StartCoroutine(LoadSceneWithDelay(playSceneIndex));
        else
            StartCoroutine(LoadSceneWithDelay(playSceneName));
    }

    // =========================
    // SETTINGS / OPTIONS PANEL
    // =========================
    public void OpenOptionsPanel()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        mainMenuPanel.SetActive(true);
        optionsPanel.SetActive(false);
    }

    // =========================
    // CREDIT → SCENE LAIN
    // =========================
    public void OpenCreditScene()
    {
        if (creditUseIndex)
            StartCoroutine(LoadSceneWithDelay(creditSceneIndex));
        else
            StartCoroutine(LoadSceneWithDelay(creditSceneName));
    }

    // =========================
    // QUIT
    // =========================
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // =========================
    // LOAD SCENE WITH DELAY
    // =========================
    private IEnumerator LoadSceneWithDelay(string sceneName)
    {
        yield return new WaitForSeconds(transitionDelay);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadSceneWithDelay(int sceneIndex)
    {
        yield return new WaitForSeconds(transitionDelay);
        SceneManager.LoadScene(sceneIndex);
    }
}
