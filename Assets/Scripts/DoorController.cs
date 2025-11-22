using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public bool isOpen = false;

    private Collider2D doorCollider;
    private SpriteRenderer spriteRenderer;

    [Header("Player Settings")]
    public string playerTag = "Player";  // pastikan Player punya tag "Player"

    void Awake()
    {
        doorCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateDoorState();
    }

    public void SetOpen(bool state)
    {
        isOpen = state;
        UpdateDoorState();
    }

    void UpdateDoorState()
    {
        // Collider aktif hanya jika pintu tertutup
        doorCollider.enabled = !isOpen;

        // Opsional: ubah transparansi supaya terlihat beda
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = isOpen ? 0.4f : 1f;
            spriteRenderer.color = c;
        }
    }

    // Ketika Player menabrak pintu tertutup → Player mati
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isOpen && collision.gameObject.CompareTag(playerTag))
        {
            //Destroy(collision.gameObject);
            Debug.Log("[DoorController] Player died by colliding with closed door!");
        }
    }

    // Jika pintu pakai trigger collider, gunakan ini
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isOpen && other.CompareTag(playerTag))
        {
            Destroy(other.gameObject);
            Debug.Log("[DoorController] Player died by entering closed door trigger!");
        }
    }
}
