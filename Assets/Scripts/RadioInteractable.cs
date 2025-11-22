using UnityEngine;

public class RadioInteractable : MonoBehaviour
{
    [Header("Interaction")]
    public float interactRadius = 1.5f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Puzzle Panel")]
    public GameObject puzzlePanel;

    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (puzzlePanel != null)
            puzzlePanel.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= interactRadius && Input.GetKeyDown(interactKey))
        {
            if (puzzlePanel != null)
            {
                puzzlePanel.SetActive(true);
                Time.timeScale = 0f; // pause game
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
