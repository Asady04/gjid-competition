using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NPCFollower : MonoBehaviour
{
    public static readonly List<NPCFollower> All = new List<NPCFollower>();

    [Header("Spacing (meters)")]
    public float initialDistance = 1.2f;
    public float spacing = 1.0f;

    [Header("Smoothing")]
    public float baseSmoothTime = 0.12f;
    public float extraSmoothPerIndex = 0.06f;
    public float maxSmoothTime = 0.8f;
    public float maxSpeed = 8f;
    public float arriveDistance = 0.08f;

    [Header("Misc")]
    public bool ignorePlayerCollisionAtStart = true;
    public bool makeColliderTrigger = false;

    [Header("Following")]
    public bool isFollowing = false;

    [Header("Animation")]
    public Animator animator;
    public string animSpeedParam = "Speed";

    [Header("Sprite / Flip (alternative)")]
    [Tooltip("If assigned, the script will use SpriteRenderer.flipX to mirror visuals (recommended).")]
    public SpriteRenderer spriteRenderer;
    [Tooltip("If true, the sprite art originally faces left. Set false if your art faces right.")]
    public bool originalFacesLeft = true;
    [Tooltip("Use flipX instead of scaling. Recommended to avoid negative scale issues.")]
    public bool flipUsingFlipX = true;
    [Tooltip("Deadzone for horizontal movement before flipping (prevents jitter).")]
    public float flipDeadzone = 0.05f;

    Rigidbody2D rb;
    PlayerPathRecorder recorder;
    Vector2 velocityRef = Vector2.zero;
    int spawnIndex = -1;
    static int counter = 0;

    static bool globalFollow = false;
    public static void ToggleGlobalFollow(bool newState)
    {
        globalFollow = newState;
        foreach (var fol in All)
        {
            if (fol != null)
                fol.isFollowing = newState;
        }
    }
    public static bool IsGlobalFollowing() => globalFollow;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // auto-find SpriteRenderer if user left it empty
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
        if (spawnIndex < 0) spawnIndex = counter++;
    }

    void OnDisable()
    {
        All.Remove(this);
        if (All.Count == 0) counter = 0;
    }

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            recorder = player.GetComponent<PlayerPathRecorder>();
            if (recorder == null)
                Debug.LogWarning("PlayerPathRecorder missing!");

            if (ignorePlayerCollisionAtStart)
            {
                var playerCols = player.GetComponentsInChildren<Collider2D>();
                var myCol = GetComponent<Collider2D>();
                if (myCol != null)
                {
                    foreach (var pc in playerCols)
                        Physics2D.IgnoreCollision(myCol, pc, true);
                }
            }

            if (makeColliderTrigger)
            {
                var myCol = GetComponent<Collider2D>();
                if (myCol != null) myCol.isTrigger = true;
            }
        }
    }

    void FixedUpdate()
    {
        if (!globalFollow) return;
        if (!isFollowing) return;
        if (recorder == null) return;

        float metersBack = initialDistance + spawnIndex * spacing;

        float recordedLength = 0f;
        bool haveRecordedLength = false;

        try
        {
            var mi = recorder.GetType().GetMethod("GetTotalRecordedDistance");
            if (mi != null)
            {
                recordedLength = (float)mi.Invoke(recorder, null);
                haveRecordedLength = true;
            }
        }
        catch { }

        if (haveRecordedLength && metersBack > recordedLength)
            metersBack = recordedLength;

        if (!recorder.TryGetPositionAtDistanceBack(metersBack, out Vector2 target))
            return;

        // Small separation
        Vector2 separation = Vector2.zero;
        float separationRadius = spacing * 0.6f;
        float separationStrength = 0.5f;

        foreach (var other in All)
        {
            if (other == null || other == this) continue;

            Vector2 diff = (Vector2)transform.position - (Vector2)other.transform.position;
            float d = diff.magnitude;

            if (d > 0f && d < separationRadius)
            {
                separation += diff.normalized * (separationStrength * (separationRadius - d) / separationRadius);
            }
        }

        target += separation * 0.5f;

        MoveToTarget(target);
    }

    void MoveToTarget(Vector2 target)
    {
        Vector2 cur = rb.position;
        float d = Vector2.Distance(cur, target);

        if (d <= arriveDistance)
        {
            velocityRef = Vector2.zero;
            rb.MovePosition(cur);

            if (animator != null) animator.SetFloat(animSpeedParam, 0f);
            return;
        }

        float smoothTime = Mathf.Min(maxSmoothTime, baseSmoothTime + spawnIndex * extraSmoothPerIndex);

        Vector2 newPos = Vector2.SmoothDamp(cur, target, ref velocityRef, smoothTime, maxSpeed, Time.fixedDeltaTime);

        rb.MovePosition(newPos);

        Vector2 move = newPos - cur;
        float speed = move.magnitude / Time.fixedDeltaTime;

        if (animator != null)
            animator.SetFloat(animSpeedParam, speed);

        // --- flip using SpriteRenderer.flipX (instant and safe) ---
        if (flipUsingFlipX && spriteRenderer != null)
        {
            float hx = move.x;
            if (Mathf.Abs(hx) > flipDeadzone)
            {
                bool movingRight = hx > 0f;
                // If art originally faces left, we flip when moving right (so faceRight => flip true)
                // If art originally faces right, invert behaviour.
                bool shouldFlip = originalFacesLeft ? movingRight : !movingRight;
                spriteRenderer.flipX = shouldFlip;
            }
        }
    }
}
