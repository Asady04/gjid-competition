using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet")]
    public float lifeTime = 5f;        // auto-destroy after this time
    public int damage = 1;             // kept for compatibility if you later add HP

    [Header("Collision")]
    public LayerMask playerLayer;      // set to the Player layer(s)
    public LayerMask obstacleLayer;    // set to walls/obstacles layer(s)
    public bool useTagFallback = true; // if true, also accept GameObjects tagged "Player"

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    // Non-trigger collision (recommended: bullet collider NOT set as isTrigger)
    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider);
    }

    // Trigger collision (if you use trigger colliders)
    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    void HandleHit(Collider2D col)
    {
        if (col == null) return;

        int colLayerBit = 1 << col.gameObject.layer;

        // Hit player (by layer)
        if ((playerLayer & colLayerBit) != 0)
        {
            KillPlayer(col.gameObject);
            Destroy(gameObject);
            return;
        }

        // Fallback: Hit player by tag (if enabled)
        if (useTagFallback && col.CompareTag("Player"))
        {
            KillPlayer(col.gameObject);
            Destroy(gameObject);
            return;
        }

        // Hit obstacle — destroy bullet
        if ((obstacleLayer & colLayerBit) != 0)
        {
            // optional: spawn impact effect here
            Destroy(gameObject);
            return;
        }

        // default: destroy on any collision (safe)
        Destroy(gameObject);
    }

    void KillPlayer(GameObject playerObj)
    {
        // Instant kill: remove player object from scene
        // You can replace this with any logic you want (disable, play death animation, etc.)
        Destroy(playerObj);

        // If you prefer disabling instead of destroying:
        // playerObj.SetActive(false);

        // If your player has a separate manager you want to notify, do it here:
        // var mgr = FindObjectOfType<GameManager>();
        // if (mgr != null) mgr.OnPlayerKilled();

        // Optional: spawn death VFX or sound here
    }
}
