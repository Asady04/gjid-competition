using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    public enum PatrolMode { Idle, PatrolHorizontal, PatrolVertical }

    [Header("Behavior")]
    public PatrolMode patrolMode = PatrolMode.Idle;
    public float patrolDistance = 3f; // half distance from start to endpoint
    public float patrolSpeed = 2f;    // movement speed while patrolling
    public float patrolPause = 0.2f;  // small pause at endpoints

    [Header("Detection")]
    public float detectionRadius = 6f;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer; // used for LOS check

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform muzzle; // where bullet spawns
    public float bulletSpeed = 10f;
    public float fireCooldown = 1.5f; // time between shots after aware
    public float awareDelay = 0.8f;    // "aware" delay before the first shot

    [Header("Misc")]
    public float targetPredictAhead = 0f; // optional lead for moving target

    [Header("Sprite / Flip")]
    public Transform spriteTransform; // assign enemy sprite child here (optional)
    public bool originalFacesLeft = true; // true = sprite artwork faces left by default
    public float flipThreshold = 0.01f; // min horizontal speed to flip

    // internal state
    bool isAware = false;
    bool canShoot = true;

    Transform player;

    // movement & patrol
    Rigidbody2D rb;
    Vector3 startPosition;
    float patrolTimer = 0f;
    int patrolDir = 1; // 1 or -1

    // position tracking for fallback velocity
    Vector3 lastPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        lastPosition = transform.position;
    }

    void OnEnable()
    {
        // reset patrol state
        patrolDir = 1;
        patrolTimer = 0f;
    }

    void Update()
    {
        // Detection & Shooting handled in Update (non-physics)
        HandleDetectionAndShooting();

        // flip based on movement (velocity or displacement)
        HandleFlip();

        // remember last position for next frame
        lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        // Handle patrol movement in FixedUpdate for physics
        if (!isAware)
        {
            HandlePatrolMovement();
        }
        // When aware, we keep position steady (or you can add chase logic)
    }

    // ---------------------------
    // Patrol Movement
    // ---------------------------
    void HandlePatrolMovement()
    {
        if (patrolMode == PatrolMode.Idle) return;

        Vector2 moveDelta = Vector2.zero;

        if (patrolMode == PatrolMode.PatrolHorizontal)
        {
            // move along X between startPosition.x - patrolDistance and + patrolDistance
            float left = startPosition.x - patrolDistance;
            float right = startPosition.x + patrolDistance;

            // compute current target using ping-pong or manual flipping
            float nextX = transform.position.x + patrolDir * patrolSpeed * Time.fixedDeltaTime;

            // if will go out of bounds, clamp and reverse after a pause
            if (nextX < left)
            {
                nextX = left;
                StartCoroutine(PatrolEndpointPause(-1));
            }
            else if (nextX > right)
            {
                nextX = right;
                StartCoroutine(PatrolEndpointPause(-1));
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
                StartCoroutine(PatrolEndpointPause(-1));
            }
            else if (nextY > top)
            {
                nextY = top;
                StartCoroutine(PatrolEndpointPause(-1));
            }

            moveDelta = new Vector2(0f, nextY - rb.position.y);
        }

        // apply movement using Rigidbody if available
        if (rb != null)
        {
            Vector2 targetPos = rb.position + moveDelta;
            rb.MovePosition(targetPos);
        }
        else
        {
            // fallback: transform movement
            transform.position += (Vector3)moveDelta;
        }
    }

    IEnumerator PatrolEndpointPause(int flipDir)
    {
        // Prevent multiple coroutines from stacking
        if (patrolTimer > 0f) yield break;

        // pause then flip direction
        patrolTimer = patrolPause;
        yield return new WaitForSeconds(patrolPause);
        patrolDir *= -1;
        patrolTimer = 0f;
    }

    // ---------------------------
    // Detection & Shooting (kept from your version)
    // ---------------------------
    void HandleDetectionAndShooting()
    {
        // find player if not set (cheap, assumes single player)
        if (player == null)
        {
            Collider2D p = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);
            if (p != null) player = p.transform;
        }
        else
        {
            float dist = Vector2.Distance(transform.position, player.position);

            // still in range?
            if (dist <= detectionRadius)
            {
                // check line of sight: raycast from muzzle (or enemy) towards player, see if obstacle in between
                Vector2 origin = (muzzle != null) ? (Vector2)muzzle.position : (Vector2)transform.position;
                Vector2 dir = ((Vector2)player.position - origin).normalized;
                float maxDist = detectionRadius;

                RaycastHit2D hit = Physics2D.Raycast(origin, dir, maxDist, obstacleLayer | playerLayer);

                if (hit.collider != null && ((1 << hit.collider.gameObject.layer) & playerLayer) != 0)
                {
                    // player is visible (first hit is player)
                    if (!isAware)
                    {
                        // go into aware state (this will pause patrol because isAware = true)
                        StartCoroutine(EnterAwareAndFire());
                    }
                }
                else
                {
                    // obstacle blocks view or no hit -> break awareness if was aware
                    if (isAware) StopAware();
                }
            }
            else
            {
                // out of range: reset
                if (isAware) StopAware();
                player = null; // optionally forget player so it can be reacquired
            }
        }
    }

    IEnumerator EnterAwareAndFire()
    {
        isAware = true;

        // Aware delay: show animation/sound here if you like
        yield return new WaitForSeconds(awareDelay);

        // After aware delay, fire and then enter cooldown loop while still aware and player visible
        while (isAware)
        {
            if (canShoot && player != null)
            {
                // check LOS again quickly
                Vector2 origin = (muzzle != null) ? (Vector2)muzzle.position : (Vector2)transform.position;
                Vector2 toPlayer = (Vector2)player.position - origin;
                RaycastHit2D hit = Physics2D.Raycast(origin, toPlayer.normalized, Mathf.Min(detectionRadius, toPlayer.magnitude), obstacleLayer | playerLayer);

                if (hit.collider != null && ((1 << hit.collider.gameObject.layer) & playerLayer) != 0)
                {
                    FireAt(player.position);
                    StartCoroutine(ShootCooldown());
                }
                else
                {
                    // player blocked; break awareness
                    StopAware();
                    yield break;
                }
            }

            // keep checking every frame until cooldown finishes or lost sight
            yield return null;
        }
    }

    void StopAware()
    {
        isAware = false;
        StopAllCoroutines(); // simple approach: stops EnterAwareAndFire coroutine too
        StartCoroutine(ResetCanShoot()); // ensure canShoot resets eventually
    }

    void FireAt(Vector2 targetPos)
    {
        if (bulletPrefab == null || muzzle == null) return;

        Vector2 origin = muzzle.position;
        Vector2 dir = (targetPos - origin).normalized;

        // optional leading: target predictable movement (simple)
        if (targetPredictAhead != 0f)
        {
            Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
            if (prb != null)
            {
                Vector2 lead = prb.velocity * targetPredictAhead;
                dir = ((Vector2)player.position + lead - origin).normalized;
            }
        }

        GameObject b = Instantiate(bulletPrefab, origin, Quaternion.FromToRotation(Vector3.right, dir));
        Rigidbody2D brb = b.GetComponent<Rigidbody2D>();
        if (brb != null)
        {
            brb.velocity = dir * bulletSpeed;
            brb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
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
    // Flip logic (only based on horizontal movement)
    // ---------------------------
    void HandleFlip()
    {
        if (spriteTransform == null) return;

        // Only flip when patrolling horizontally
        if (patrolMode != PatrolMode.PatrolHorizontal) return;
        if (isAware) return; // optional: do not flip when aware/shooting

        Vector2 move = Vector2.zero;

        if (rb != null)
        {
            move = rb.velocity;
        }
        else
        {
            Vector3 delta = transform.position - lastPosition;
            float dt = Mathf.Max(Time.deltaTime, 1e-6f);
            move = new Vector2(delta.x / dt, delta.y / dt);
        }

        // Flip only if moving significantly left/right
        if (move.x > flipThreshold)
        {
            float sx = originalFacesLeft ? -1f : 1f;
            spriteTransform.localScale = new Vector3(sx, Mathf.Abs(spriteTransform.localScale.y), Mathf.Abs(spriteTransform.localScale.z));
        }
        else if (move.x < -flipThreshold)
        {
            float sx = originalFacesLeft ? 1f : -1f;
            spriteTransform.localScale = new Vector3(sx, Mathf.Abs(spriteTransform.localScale.y), Mathf.Abs(spriteTransform.localScale.z));
        }
    }

    // draw detection gizmo and patrol bounds
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // show patrol extents
        if (patrolMode != PatrolMode.Idle)
        {
            Gizmos.color = Color.cyan;
            Vector3 origin = Application.isPlaying ? startPosition : transform.position;
            if (patrolMode == PatrolMode.PatrolHorizontal)
            {
                Gizmos.DrawLine(origin + Vector3.left * patrolDistance, origin + Vector3.right * patrolDistance);
                Gizmos.DrawSphere(origin + Vector3.left * patrolDistance, 0.05f);
                Gizmos.DrawSphere(origin + Vector3.right * patrolDistance, 0.05f);
            }
            else
            {
                Gizmos.DrawLine(origin + Vector3.down * patrolDistance, origin + Vector3.up * patrolDistance);
                Gizmos.DrawSphere(origin + Vector3.down * patrolDistance, 0.05f);
                Gizmos.DrawSphere(origin + Vector3.up * patrolDistance, 0.05f);
            }
        }

        if (muzzle != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(muzzle.position, 0.05f);
        }
    }
}
