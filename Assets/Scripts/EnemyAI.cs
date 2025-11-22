using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyType { Idle, PatrolHorizontal, PatrolVertical }

    [Header("General")]
    public EnemyType type = EnemyType.Idle;
    public float speed = 1.2f;
    public float chaseSpeed = 2.5f;
    public float arriveDistance = 0.1f;

    [Header("Detection")]
    // NOTE: this version focuses on detecting followers; when a follower is seen, the enemy will chase the PLAYER.
    public string detectFollowerTag = "NPC"; // tag for follower NPCs
    public string playerTag = "Player";
    public LayerMask detectionMask; // optional
    public float detectionRadius = 3.0f;
    [Range(0f, 180f)] public float detectionHalfAngle = 60f;
    public float timeToLoseTarget = 0.8f; // seconds to wait before returning to patrol/idle when losing sight

    [Header("Patrol")]
    public float patrolDistance = 3f; // how far from start position to patrol
    public bool startFacingRight = true;

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public Color gizmoColor = Color.red;

    Rigidbody2D rb;
    Vector2 startPos;
    Vector2 patrolDir;
    int patrolSign = 1;
    Transform currentTarget;      // the transform currently being chased (in this version it's the PLAYER when triggered)
    float lostTargetTimer = 0f;
    bool facingRight;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        startPos = transform.position;
        facingRight = startFacingRight;
        patrolDir = (type == EnemyType.PatrolVertical) ? Vector2.up : Vector2.right;
        if (!startFacingRight) patrolSign = -1;
    }

    void FixedUpdate()
    {
        // 1) Look for a follower in front; if found -> set target to the player and chase the player
        Transform seenFollower = DetectFollowerInFront();
        if (seenFollower != null)
        {
            var playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                currentTarget = playerObj.transform;
                lostTargetTimer = 0f;
                ChaseTarget();
                return;
            }
        }

        // 2) If we previously had a target (player) keep chasing for a moment (grace time)
        if (currentTarget != null)
        {
            lostTargetTimer += Time.fixedDeltaTime;
            if (lostTargetTimer < timeToLoseTarget)
            {
                ChaseTarget();
                return;
            }
            else
            {
                // lost player for good -> clear target and resume patrol/idle
                currentTarget = null;
            }
        }

        // 3) default behaviour when no followers seen and no recent player target
        if (type == EnemyType.Idle)
        {
            rb.MovePosition(rb.position);
        }
        else
        {
            PatrolMove();
        }
    }

    // Finds a follower (tagged detectFollowerTag) that lies inside the forward cone and is within detectionRadius.
    Transform DetectFollowerInFront()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, detectionMask == 0 ? ~0 : detectionMask);
        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (var c in hits)
        {
            if (c == null) continue;
            if (!c.CompareTag(detectFollowerTag)) continue;

            Vector2 dir = (c.transform.position - transform.position);
            float dist = dir.magnitude;
            if (dist <= 0.001f) continue;

            // compute forward vector (use right as forward for horizontal enemies, up for vertical)
            Vector2 forward = (type == EnemyType.PatrolVertical) ? (facingRight ? transform.up : -transform.up) : (facingRight ? transform.right : -transform.right);

            float angle = Vector2.Angle(forward, dir.normalized);
            if (angle > detectionHalfAngle) continue;

            // optional: simple LOS check; adjust mask if you want occluders
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir.normalized, dist, detectionMask == 0 ? ~0 : detectionMask);
            if (hit.collider != null && hit.collider != c)
            {
                // blocked by some collider — ignore this follower
                continue;
            }

            if (dist < bestDist)
            {
                bestDist = dist;
                best = c.transform;
            }
        }

        return best;
    }

    void ChaseTarget()
    {
        if (currentTarget == null) return;
        Vector2 pos = rb.position;
        Vector2 targetPos = currentTarget.position;
        Vector2 dir = (targetPos - pos);
        float dist = dir.magnitude;
        if (dist <= arriveDistance)
        {
            rb.MovePosition(pos);
            return;
        }

        Vector2 vel = dir.normalized * chaseSpeed * Time.fixedDeltaTime;
        rb.MovePosition(pos + vel);

        // set facing
        if (Mathf.Abs(vel.x) > 0.01f) facingRight = vel.x > 0;
        if (Mathf.Abs(vel.y) > 0.01f && type == EnemyType.PatrolVertical) facingRight = vel.y > 0;
        ApplyFlip();
    }

    void PatrolMove()
    {
        Vector2 goal = startPos + patrolDir * patrolDistance * patrolSign;
        Vector2 pos = rb.position;
        Vector2 dir = (goal - pos);
        float dist = dir.magnitude;

        if (dist <= 0.05f)
        {
            patrolSign *= -1;
            return;
        }

        Vector2 vel = dir.normalized * speed * Time.fixedDeltaTime;
        rb.MovePosition(pos + vel);

        if (type == EnemyType.PatrolHorizontal)
        {
            if (Mathf.Abs(vel.x) > 0.01f) facingRight = vel.x > 0;
        }
        else
        {
            if (Mathf.Abs(vel.y) > 0.01f) facingRight = vel.y > 0;
        }
        ApplyFlip();
    }

    void ApplyFlip()
    {
        // flip sprite by scale.x if you use that method:
        // Vector3 s = transform.localScale;
        // s.x = facingRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        // transform.localScale = s;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Vector3 fwd = Application.isPlaying ? ((type == EnemyType.PatrolVertical) ? (facingRight ? transform.up : -transform.up) : (facingRight ? transform.right : -transform.right))
                                           : transform.right;

        float half = detectionHalfAngle;
        Quaternion rot1 = Quaternion.AngleAxis(half, Vector3.forward);
        Quaternion rot2 = Quaternion.AngleAxis(-half, Vector3.forward);
        Vector3 v1 = rot1 * fwd;
        Vector3 v2 = rot2 * fwd;

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.25f);
        Gizmos.DrawLine(transform.position, transform.position + v1 * detectionRadius);
        Gizmos.DrawLine(transform.position, transform.position + v2 * detectionRadius);

        if (type != EnemyType.Idle)
        {
            Vector2 p1 = (Vector2)transform.position + (type == EnemyType.PatrolVertical ? Vector2.up : Vector2.right) * patrolDistance;
            Vector2 p2 = (Vector2)transform.position + (type == EnemyType.PatrolVertical ? Vector2.down : Vector2.left) * patrolDistance;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(p1, 0.1f);
            Gizmos.DrawWireSphere(p2, 0.1f);
        }
    }
}
