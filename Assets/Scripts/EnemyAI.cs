using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    public enum PatrolMode { Idle, PatrolHorizontal, PatrolVertical }

    [Header("Behavior")]
    public PatrolMode patrolMode = PatrolMode.Idle;
    public float patrolDistance = 3f;
    public float patrolSpeed = 2f;
    public float patrolPause = 0.2f;

    [Header("Detection")]
    public float detectionRadius = 6f;
    public LayerMask playerLayer;   // prefer Tag="Player", but use layer for fallback
    public LayerMask obstacleLayer; // used for LOS

    [Header("Companion / Requirement")]
    [Tooltip("If true, enemy will only become 'aware' and shoot when the player has a companion near them.")]
    public bool requireCompanion = true;             // enforce companion requirement
    [Tooltip("Distance around the player to consider a companion 'with' the player")]
    public float companionRadius = 1.5f;

    [Tooltip("If true, will also consider objects with the specified companionTagName as companions (tag fallback).")]
    public bool companionTagFallback = false; // recommended: false for strict behavior
    [Tooltip("Tag name used as fallback for companions (e.g., 'Companion' or 'NPC').")]
    public string companionTagName = "Companion";

    [Header("Shooting")]
    public GameObject bulletPrefab;      // assign prefab with Bullet.cs on it
    public Transform muzzle;             // assign muzzle transform (can be child)
    public float bulletSpeed = 12f;
    public float muzzleOffset = 0.25f;   // small forward offset so spawn is slightly ahead
    public float fireCooldown = 1.5f;
    public float awareDelay = 0.8f;
    public float targetPredictAhead = 0f;

    [Header("Sprite / Flip")]
    public Transform spriteRoot;
    public bool originalFacesLeft = true;
    public float flipSmoothSpeed = 720f;

    [Header("Animator (optional)")]
    [Tooltip("Animator with parameters: 'Idle' (bool), 'Horizontal' (float), 'Vertical' (float).")]
    public Animator animator;

    // internal state
    bool isAware = false;
    bool canShoot = true;

    Transform player;

    // movement & patrol
    Rigidbody2D rb;
    Vector3 startPosition;
    float patrolTimer = 0f;
    int patrolDir = 1;

    Coroutine awareCoroutine = null; // reference to aware coroutine so we can stop only it

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;

        if (spriteRoot == null)
            Debug.LogWarning($"{name}: spriteRoot not assigned. Assign a child transform for visuals so flip works properly.");
        if (muzzle == null)
            Debug.LogWarning($"{name}: muzzle not assigned.");

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        patrolDir = 1;
        patrolTimer = 0f;
    }

    void Update()
    {
        HandleDetectionAndShooting();
        HandleSmoothFlip();

        if (isAware)
            UpdateAnimator(Vector2.zero);
    }

    void FixedUpdate()
    {
        if (!isAware)
            HandlePatrolMovement();
    }

    // ---------------------------
    // Patrol Movement
    // ---------------------------
    void HandlePatrolMovement()
    {
        if (patrolMode == PatrolMode.Idle)
        {
            UpdateAnimator(Vector2.zero);
            return;
        }

        Vector2 moveDelta = Vector2.zero;

        if (patrolMode == PatrolMode.PatrolHorizontal)
        {
            float left = startPosition.x - patrolDistance;
            float right = startPosition.x + patrolDistance;
            float nextX = transform.position.x + patrolDir * patrolSpeed * Time.fixedDeltaTime;

            if (nextX < left)
            {
                nextX = left;
                StartCoroutine(PatrolEndpointPause());
            }
            else if (nextX > right)
            {
                nextX = right;
                StartCoroutine(PatrolEndpointPause());
            }

            moveDelta = new Vector2(nextX - rb.position.x, 0f);
        }
        else if (patrolMode == PatrolMode.PatrolVertical)
        {
            float bottom = startPosition.y - patrolDistance;
            float top = startPosition.y + patrolDistance;
            float nextY = transform.position.y + patrolDir * patrolSpeed * Time.fixedDeltaTime;

            if (nextY < bottom)
            {
                nextY = bottom;
                StartCoroutine(PatrolEndpointPause());
            }
            else if (nextY > top)
            {
                nextY = top;
                StartCoroutine(PatrolEndpointPause());
            }

            moveDelta = new Vector2(0f, nextY - rb.position.y);
        }

        Vector2 velocity = moveDelta / Time.fixedDeltaTime;
        if (rb != null)
            rb.MovePosition(rb.position + moveDelta);
        else
            transform.position += (Vector3)moveDelta;

        UpdateAnimator(velocity);
    }

    IEnumerator PatrolEndpointPause()
    {
        if (patrolTimer > 0f) yield break;
        patrolTimer = patrolPause;
        yield return new WaitForSeconds(patrolPause);
        patrolDir *= -1;
        patrolTimer = 0f;
    }

    // ---------------------------
    // Detection & Shooting
    // ---------------------------
    void HandleDetectionAndShooting()
    {
        // find candidate player(s) inside detection radius
        Collider2D[] candidates = Physics2D.OverlapCircleAll(transform.position, detectionRadius, playerLayer);

        Transform foundPlayer = null;
        float bestDist = float.MaxValue;

        // Prefer tag "Player"
        foreach (var c in candidates)
        {
            if (c == null) continue;
            if (c.CompareTag("Player"))
            {
                float d = Vector2.SqrMagnitude(c.transform.position - transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    foundPlayer = c.transform;
                }
            }
        }

        // Fallback: nearest collider in mask
        if (foundPlayer == null && candidates.Length > 0)
        {
            foreach (var c in candidates)
            {
                if (c == null) continue;
                float d = Vector2.SqrMagnitude(c.transform.position - transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    foundPlayer = c.transform;
                }
            }
        }

        if (foundPlayer == null)
        {
            if (isAware) StopAware();
            player = null;
            return;
        }

        player = foundPlayer;

        // distance safety
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectionRadius)
        {
            if (isAware) StopAware();
            player = null;
            return;
        }

        // line of sight
        Vector2 origin = muzzle != null ? (Vector2)muzzle.position : (Vector2)transform.position;
        Vector2 dir = ((Vector2)player.position - origin).normalized;
        float maxDist = detectionRadius;
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, maxDist, obstacleLayer | playerLayer);

        if (hit.collider == null)
        {
            if (isAware) StopAware();
            return;
        }

        bool rayHitPlayer = (hit.transform == player) || hit.transform.CompareTag("Player");
        if (!rayHitPlayer)
        {
            if (isAware) StopAware();
            return;
        }

        // companion requirement: STRICT check (requires NPCFollower.isFollowing + proximity)
        if (requireCompanion)
        {
            bool hasCompanion = PlayerHasCompanionStrict(player);
            if (!hasCompanion)
            {
                if (isAware) StopAware();
                return;
            }
        }

        // all checks passed — start aware/shoot if not already
        if (!isAware)
        {
            StopAware(); // ensure stale coroutine removed
            awareCoroutine = StartCoroutine(EnterAwareAndFire());
        }
    }

    IEnumerator EnterAwareAndFire()
    {
        isAware = true;
        yield return new WaitForSeconds(awareDelay);

        while (isAware)
        {
            if (player == null)
            {
                Debug.Log($"{name}: player lost during aware loop -> stopping aware.");
                StopAware();
                yield break;
            }

            // re-evaluate LOS and companion status each tick
            Vector2 origin = muzzle != null ? (Vector2)muzzle.position : (Vector2)transform.position;
            Vector2 toPlayer = (Vector2)player.position - origin;
            RaycastHit2D hit = Physics2D.Raycast(origin, toPlayer.normalized, Mathf.Min(detectionRadius, toPlayer.magnitude), obstacleLayer | playerLayer);

            bool hitPlayerNow = (hit.collider != null && (hit.transform == player || hit.transform.CompareTag("Player")));
            bool hasCompanionNow = !requireCompanion ? true : PlayerHasCompanionStrict(player);

            Debug.Log($"{name}: AwareLoop -> hitPlayerNow={hitPlayerNow}, hasCompanionNow={hasCompanionNow}");

            if (!hitPlayerNow || !hasCompanionNow)
            {
                Debug.Log($"{name}: Conditions failed during aware loop -> stopping aware.");
                StopAware();
                yield break;
            }

            if (canShoot)
            {
                // final defensive guard just before firing (ensures companion still true)
                if (requireCompanion && player != null && !PlayerHasCompanionStrict(player))
                {
                    Debug.Log($"{name}: Fire prevented — companion check failed at moment of firing.");
                }
                else
                {
                    Debug.Log($"{name}: Firing at player '{player.name}' — requireCompanion={requireCompanion}, hasCompanionNow={PlayerHasCompanionStrict(player)}");
                    FireAt(player.position);
                    StartCoroutine(ShootCooldown());
                }
            }

            yield return null;
        }
    }

    void StopAware()
    {
        isAware = false;
        if (awareCoroutine != null)
        {
            try { StopCoroutine(awareCoroutine); } catch { }
            awareCoroutine = null;
        }
        StartCoroutine(ResetCanShoot());
    }

    public void FireAt(Vector2 targetPosition)
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("[EnemyAI] bulletPrefab is not assigned.");
            return;
        }

        if (muzzle == null)
        {
            Debug.LogWarning("[EnemyAI] muzzle is not assigned. Using enemy transform.");
        }

        Vector2 origin = (muzzle != null) ? (Vector2)muzzle.position : (Vector2)transform.position;
        Vector2 dir = (targetPosition - origin);
        if (dir.sqrMagnitude <= 0.0001f)
        {
            Debug.LogWarning("[EnemyAI] FireAt called with zero direction.");
            dir = transform.right; // fallback
        }
        dir.Normalize();

        // spawn a bit forward from the muzzle to reduce overlap with shooter
        Vector2 spawnPos = origin + dir * muzzleOffset;

        GameObject b = Instantiate(bulletPrefab, spawnPos, Quaternion.FromToRotation(Vector3.right, dir));
        Bullet bulletComp = b.GetComponent<Bullet>();
        if (bulletComp != null)
        {
            // set the owner so bullet can ignore shooter colliders and know who fired it
            bulletComp.Initialize(dir * bulletSpeed, this.gameObject);

            // Make sure bullet layer masks are configured in inspector (playerLayer/obstacleLayer)
            // Optionally set bullet damage here:
            // bulletComp.damage = 15f;
        }
        else
        {
            // if the prefab doesn't have Bullet, try setting RB velocity directly
            var rb = b.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = dir * bulletSpeed;
            }
        }
    }

    IEnumerator ShootCooldown()
    {
        canShoot = false;
        yield return new WaitForSeconds(fireCooldown);
        canShoot = true;
    }

    IEnumerator ResetCanShoot()
    {
        canShoot = true;
        yield return null;
    }

    // ---------------------------
    // Flip logic
    // ---------------------------
    void HandleSmoothFlip()
    {
        if (spriteRoot == null) return;

        bool? desiredFaceRight = null;

        if (isAware && player != null)
        {
            Vector2 origin = muzzle != null ? (Vector2)muzzle.position : (Vector2)transform.position;
            Vector2 toPlayer = (Vector2)player.position - origin;
            if (Mathf.Abs(toPlayer.x) > 0.001f) desiredFaceRight = toPlayer.x > 0f;
        }

        if (desiredFaceRight == null && patrolMode == PatrolMode.PatrolHorizontal)
            desiredFaceRight = patrolDir > 0;

        if (desiredFaceRight == null) return;

        float targetY = originalFacesLeft ? (desiredFaceRight.Value ? 180f : 0f)
                                          : (desiredFaceRight.Value ? 0f : 180f);

        Vector3 curEuler = spriteRoot.localEulerAngles;
        float curY = curEuler.y;
        float newY = Mathf.MoveTowardsAngle(curY, targetY, flipSmoothSpeed * Time.deltaTime);
        spriteRoot.localEulerAngles = new Vector3(curEuler.x, newY, curEuler.z);

        if (muzzle != null && muzzle.parent != spriteRoot)
        {
            Vector3 mscale = muzzle.localScale;
            bool faceRight = desiredFaceRight.Value;
            float sign = (originalFacesLeft ? (faceRight ? -1f : 1f) : (faceRight ? 1f : -1f));
            muzzle.localScale = new Vector3(Mathf.Sign(sign) * Mathf.Abs(mscale.x), mscale.y, mscale.z);

            Vector3 mpos = muzzle.localPosition;
            mpos.x = Mathf.Abs(mpos.x) * (faceRight ? 1f : -1f);
            muzzle.localPosition = mpos;
        }
    }

    // ---------------------------
    // Companion detection (STRICT)
    // ---------------------------
    bool PlayerHasCompanionStrict(Transform playerTransform)
    {
        if (!requireCompanion) return true;
        if (playerTransform == null) return false;

        // Require global follow toggled on; if it's off, no one is following.
        if (!NPCFollower.IsGlobalFollowing())
        {
            Debug.Log($"{name}: Global follow is OFF -> player considered alone.");
            return false;
        }

        float sqrR = companionRadius * companionRadius;

        // Only accept NPCFollower instances that explicitly report following and are within radius
        foreach (var fol in NPCFollower.All)
        {
            if (fol == null) continue;
            if (!fol.gameObject.activeInHierarchy) continue;
            if (!fol.isFollowing) continue; // must be explicitly following
            if (fol.transform == playerTransform) continue;

            float d2 = (fol.transform.position - playerTransform.position).sqrMagnitude;
            Debug.Log($"{name}: follower '{fol.gameObject.name}' dist={(Mathf.Sqrt(d2)):F2} isFollowing={fol.isFollowing}");
            if (d2 <= sqrR)
            {
                Debug.Log($"{name}: Companion found -> '{fol.gameObject.name}' dist={(Mathf.Sqrt(d2)):F2}");
                return true;
            }
        }

        // Fallbacks are intentionally disabled by default; enable only if necessary.
        if (companionTagFallback)
        {
            Collider2D[] nearby = Physics2D.OverlapCircleAll(playerTransform.position, companionRadius);
            foreach (var c in nearby)
            {
                if (c == null) continue;
                if (c.transform == playerTransform) continue;
                if (c.CompareTag(companionTagName))
                {
                    Debug.Log($"{name}: Companion detected via tag fallback -> {c.name}");
                    return true;
                }
            }
        }

        Debug.Log($"{name}: No active following companion found -> player considered ALONE (will NOT be targeted).");
        return false;
    }

    // Helper: update animator parameters
    void UpdateAnimator(Vector2 velocity)
    {
        if (animator == null) return;

        float speed = velocity.magnitude;
        animator.SetFloat("Speed", speed);
    }

    // draw gizmos for debugging
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

#if UNITY_EDITOR
        if (requireCompanion && player != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(player.position, companionRadius);
        }
#endif
    }
}
