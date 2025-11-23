using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneInitializer : MonoBehaviour
{
    [Tooltip("Optional: only reset when loading scenes whose name contains this substring. Leave empty to always reset.")]
    public string sceneNameFilter = ""; // e.g. "day" or leave empty

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
        if (!string.IsNullOrEmpty(sceneNameFilter) &&
            !scene.name.ToLower().Contains(sceneNameFilter.ToLower()))
            return;

        Debug.Log($"[SceneInitializer] Resetting transient state for scene: {scene.name}");

        // 1) Clear all flags (clean slate)
        GameGlobals.ClearAll();

        // 2) Ensure important flags are default
        GameGlobals.SetFlag("canEnemyShoot", true);
        GameGlobals.SetFlag("player_dead", false);

        // 3) Reset global follow and per-follower state
        NPCFollower.ToggleGlobalFollow(false);
        var followers = GameObject.FindObjectsOfType<NPCFollower>();
        foreach (var f in followers)
        {
            // call the reset API we will add to NPCFollower
            f.ResetFollowerState();
        }

        // 4) Reset objectives (if ObjectiveManager exists)
        var om = GameObject.FindObjectOfType<ObjectiveManager>();
        if (om != null)
        {
            om.ResetAllObjectives();
        }

        // 5) Ensure time and UI-related global state is sane
        Time.timeScale = 1f;
        Debug.Log("[SceneInitializer] Reset complete.");
    }
}
