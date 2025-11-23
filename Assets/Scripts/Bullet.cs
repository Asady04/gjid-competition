using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bullet with swept collision detection, owner-ignore, short grace period after spawn,
/// and overlap fallback. Call Initialize(velocity, owner) right after Instantiate.
/// When a Player is hit, this will Debug.Log("the player is dead") and destroy the bullet.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet settings")]
    public float damage = 10f;
    public float maxLifetime = 5f;               // auto destroy after this time
    public float collisionGraceSeconds = 0.035f; // skip collision checks for this short period
    public float minRaycastAdvance = 0.01f;      // small offset to advance ray origin
    public LayerMask playerLayer;                // assign Player layer
    public LayerMask obstacleLayer;              // assign Obstacle layer(s)

    // runtime
    private Rigidbody2D rb;
    private Vector2 previousPosition;
    private float spawnTime;
    private GameObject owner;
    private Collider2D[] ownerColliders;
    private Collider2D myCollider;
    private int combinedMask;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        spawnTime = Time.time;
        previousPosition = rb != null ? (Vector2)rb.position : (Vector2)transform.position;

        // prepare combined mask (player + obstacles)
        combinedMask = (playerLayer.value | obstacleLayer.value);

        // gather owner's colliders + ignore collisions physically (best-effort)
        if (owner != null)
        {
            ownerColliders = owner.GetComponentsInChildren<Collider2D>();
            if (myCollider != null && ownerColliders != null)
            {
                foreach (var oc in ownerColliders)
                {
                    if (oc != null)
                    {
                        Physics2D.IgnoreCollision(myCollider, oc, true);
                    }
                }
            }
        }

        // safety: destroy after maxLifetime in case it never hits anything
        Destroy(gameObject, maxLifetime);
    }

    /// <summary>
    /// Call right after Instantiate. Sets initial velocity and owner reference.
    /// </summary>
    public void Initialize(Vector2 initialVelocity, GameObject ownerGameObject)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.velocity = initialVelocity;
        owner = ownerGameObject;
    }

    void FixedUpdate()
    {
        // update current pos
        Vector2 currentPos = rb != null ? rb.position : (Vector2)transform.position;

        // debug drawing (short-lived)
        Debug.DrawLine(previousPosition, currentPos, Color.red, 0.05f);

        // skip hit checks for a brief grace period right after spawn - avoids spawn-overlap hits
        if (Time.time - spawnTime < collisionGraceSeconds)
        {
            previousPosition = currentPos;
            return;
        }

        // compute direction & distance for the swept test
        Vector2 travel = currentPos - previousPosition;
        float dist = travel.magnitude;
        if (dist > 0f)
        {
            Vector2 dir = travel / dist;

            // advance the ray origin slightly forward to avoid hitting overlapping spawner colliders
            float advance = Mathf.Min(minRaycastAdvance, dist * 0.5f);
            Vector2 rayOrigin = previousPosition + dir * advance;
            float rayDist = Mathf.Max(0f, dist - advance);

            if (rayDist > 0f)
            {
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, dir, rayDist, combinedMask);
                if (hit.collider != null)
                {
                    // skip owner's colliders just in case (extra safety)
                    if (!IsOwnerCollider(hit.collider))
                    {
                        Debug.Log($"[Bullet] Raycast hit: {hit.collider.name} (tag={hit.collider.gameObject.tag})");
                        HandleHit(hit.collider);
                        Destroy(gameObject);
                        return;
                    }
                }
            }

            // fallback: overlap at current position
            Collider2D[] overlaps = Physics2D.OverlapPointAll(currentPos, combinedMask);
            foreach (var c in overlaps)
            {
                if (c == null) continue;
                if (IsOwnerCollider(c)) continue; // ignore owner
                Debug.Log($"[Bullet] Overlap hit: {c.name} (tag={c.gameObject.tag})");
                HandleHit(c);
                Destroy(gameObject);
                return;
            }
        }

        previousPosition = currentPos;
    }

    private bool IsOwnerCollider(Collider2D c)
    {
        if (ownerColliders == null) return false;
        foreach (var oc in ownerColliders)
        {
            if (oc == c) return true;
        }
        return false;
    }

    private void HandleHit(Collider2D c)
    {
        if (c == null) return;

        GameObject hitObj = c.gameObject;

        // If it's the Player by tag, log death and attempt to call common damage method(s)
        if (hitObj.CompareTag("Player"))
        {
            // debug message requested: when bullet hits player print exactly this
            GameGlobals.SetFlag("player_dead", true);

            // Try common direct call if PlayerController exists
            var playerController = hitObj.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // try TakeDamage or ApplyDamage (uses SendMessage as fallback)
                try
                {
                    // prefer a strongly-typed call if method exists
                    playerController.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                }
                catch { }
            }
            else
            {
                // fallback: try an IDamageable or SendMessage
                var dmg = hitObj.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    dmg.ApplyDamage(damage);
                }
                else
                {
                    hitObj.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                    hitObj.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
                }
            }

            return;
        }

        // Non-player objects: try IDamageable
        var damageable = hitObj.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.ApplyDamage(damage);
        }

        // Add VFX or other reactions here if needed.
    }

    // Also handle physics collisions (in case project uses triggers or normal collisions)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time - spawnTime < collisionGraceSeconds) return; // still in grace period
        if (IsOwnerCollider(other)) return; // ignore owner

        // only react if in mask
        int layerBit = 1 << other.gameObject.layer;
        if ((layerBit & combinedMask) == 0) return;

        // If player, log and handle
        if (other.gameObject.CompareTag("Player"))
        {
            GameGlobals.SetFlag("player_dead", true);
            HandleHit(other);
            Destroy(gameObject);
            return;
        }

        // otherwise, normal hit handling
        HandleHit(other);
        Destroy(gameObject);
    }
}

// Simple IDamageable interface (optional). If your project already defines one, remove this.
public interface IDamageable
{
    void ApplyDamage(float amount);
}
