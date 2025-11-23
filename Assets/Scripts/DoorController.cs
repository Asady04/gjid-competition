using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public bool isOpen = false;
    public bool isPermanentOpen = false;

    private Collider2D doorCollider;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        doorCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateDoorState();
    }

    public void SetOpen(bool state)
    {
        if (isPermanentOpen)
            return;

        isOpen = state;
        UpdateDoorState();
    }

    public void SetPermanentOpen()
    {
        isPermanentOpen = true;
        isOpen = true;
        UpdateDoorState();
    }

    private void UpdateDoorState()
    {
        doorCollider.enabled = !isOpen;

        if (spriteRenderer != null)
        {
            var c = spriteRenderer.color;
            c.a = isOpen ? 0.4f : 1f;
            spriteRenderer.color = c;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isOpen && collision.collider.CompareTag("Player"))
        {
            Debug.Log("Player died by door");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isOpen && other.CompareTag("Player"))
        {
            Destroy(other.gameObject);
            Debug.Log("Player died by trigger door");
        }
    }
}
