using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet")]
    public float lifeTime = 5f;
    public float minRaycastDistance = 0.01f; // small safety

    [Header("Collision")]
    public LayerMask playerLayer;    // set this to your Player layer
    public LayerMask obstacleLayer;  // set this to environment/walls
    public bool useTagFallback = true;

    Rigidbody2D rb;
    Collider2D col;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (col == null) Debug.LogWarning($"{name}: Bullet needs a Collider2D");
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // optional debug: Debug.Log($"Bullet collided (Collision): {collision.gameObject.name} layer={collision.gameObject.layer} tag={collision.gameObject.tag}");
        HandleHit(collision.collider);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // optional debug: Debug.Log($"Bullet collided (Trigger): {other.gameObject.name} layer={other.gameObject.layer} tag={other.gameObject.tag}");
        HandleHit(other);
    }

    void FixedUpdate()
    {
        Vector2 currentPos = transform.position;
        Vector2 moveDelta;

        if (rb != null)
        {
            moveDelta = rb.velocity * Time.fixedDeltaTime;
        }
        else
        {
            // If no rigidbody, assume transform movement
            moveDelta = (Vector2)(transform.position - (Vector3)currentPos);
        }

        float dist = Mathf.Max(moveDelta.magnitude, minRaycastDistance);
        if (dist <= 0f) return;

        Vector2 dir = moveDelta.normalized;
        LayerMask combined = playerLayer | obstacleLayer;

        RaycastHit2D hit = Physics2D.Raycast(currentPos, dir, dist, combined);
        if (hit.collider != null)
        {
            HandleHit(hit.collider);
            if (this != null) Destroy(gameObject);
        }
    }

    void HandleHit(Collider2D target)
    {
        if (target == null) return;

        int targetBit = 1 << target.gameObject.layer;

        // Player by layer
        if ((playerLayer.value & targetBit) != 0)
        {
            Debug.Log("the player is dead");
            Destroy(gameObject);
            return;
        }

        // Tag fallback
        if (useTagFallback && target.CompareTag("Player"))
        {
            Debug.Log("the player is dead");
            Destroy(gameObject);
            return;
        }

        // obstacle
        if ((obstacleLayer.value & targetBit) != 0)
        {
            Destroy(gameObject);
            return;
        }

        // default
        Destroy(gameObject);
    }
}
