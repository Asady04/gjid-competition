
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditScroller : MonoBehaviour
{
    public float scrollSpeed = 50f;
    public string sceneToLoadAfter;

    private RectTransform creditsRectTransform;
    private bool isLoading = false;

    void Start()
    {
        creditsRectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // Move the credits up.
        creditsRectTransform.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);

        // Check if credits have finished and we are not already loading.
        if (creditsRectTransform.anchoredPosition.y > creditsRectTransform.rect.height && !isLoading)
        {
            Debug.Log("Credits finished. Starting scene load.");
            if (!string.IsNullOrEmpty(sceneToLoadAfter))
            {
                isLoading = true; // Set flag to prevent multiple load calls.
                StartCoroutine(LoadYourAsyncScene());
            }
            else
            {
                this.enabled = false;
            }
        }
    }

    IEnumerator LoadYourAsyncScene()
    {
        // The AsyncOperation handles the loading process.
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoadAfter);

        // Keep the scene from activating until we are ready.
        asyncLoad.allowSceneActivation = false;

        // Wait until the scene is fully loaded in the background.
        while (!asyncLoad.isDone)
        {
            // This is a great place to update a loading bar.
            Debug.Log($"Loading progress: {asyncLoad.progress * 100}%");

            // Check if the scene is almost loaded (typically at 0.9 progress).
            if (asyncLoad.progress >= 0.9f)
            {
                // Allow the scene to activate now.
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
