using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NPCFollower : MonoBehaviour
{
    public static readonly List<NPCFollower> All = new List<NPCFollower>();

    [Header("Spacing (meters)")]
    public float initialDistance = 1.2f;   // how far behind the player the first NPC should be
    public float spacing = 1.0f;           // desired meters between successive NPCs

    [Header("Smoothing")]
    public float baseSmoothTime = 0.12f;
    public float extraSmoothPerIndex = 0.06f;
    public float maxSmoothTime = 0.8f;
    public float maxSpeed = 8f;
    public float arriveDistance = 0.08f;

    [Header("Misc")]
    public bool ignorePlayerCollisionAtStart = true;
    public bool makeColliderTrigger = false;

    Rigidbody2D rb;
    PlayerPathRecorder recorder; // ensure this class exposes TryGetPositionAtDistanceBack and (optionally) GetTotalRecordedDistance
    Vector2 velocityRef = Vector2.zero;
    int spawnIndex = -1;
    static int counter = 0;

    // global follow state (UI / interactor will call these)
    static bool globalFollow = false;
    public static void ToggleGlobalFollow(bool newState)
    {
        globalFollow = newState;
    }
    public static bool IsGlobalFollowing() => globalFollow;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
    }

    void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
        if (spawnIndex < 0) spawnIndex = counter++;
    }

    void OnDisable()
    {
        All.Remove(this);

        // If list empty, reset static counter to avoid ever-growing spawnIndex across play sessions
        if (All.Count == 0)
        {
            counter = 0;
        }
    }

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            recorder = player.GetComponent<PlayerPathRecorder>();
            if (recorder == null)
                Debug.LogWarning("[NPCFollower] PlayerPathRecorder missing on Player. Make sure it implements TryGetPositionAtDistanceBack(float, out Vector2).");

            if (ignorePlayerCollisionAtStart)
            {
                var playerCols = player.GetComponentsInChildren<Collider2D>();
                var myCol = GetComponent<Collider2D>();
                if (myCol != null)
                {
                    foreach (var pc in playerCols)
                    {
                        if (pc != null) Physics2D.IgnoreCollision(myCol, pc, true);
                    }
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
        // Respect global follow toggle
        if (!globalFollow) return;

        if (recorder == null) return;

        // compute desired metersBack for this NPC
        float metersBack = initialDistance + spawnIndex * spacing;

        // Optional: clamp metersBack to recorded available distance if recorder provides it
        // This prevents requesting points older than the buffer
        float recordedLength = 0f;
        bool haveRecordedLength = false;
        // try to call GetTotalRecordedDistance() if present on recorder
        try
        {
            var mi = recorder.GetType().GetMethod("GetTotalRecordedDistance");
            if (mi != null)
            {
                recordedLength = (float)mi.Invoke(recorder, null);
                haveRecordedLength = true;
            }
        }
        catch { haveRecordedLength = false; }

        if (haveRecordedLength && metersBack > recordedLength)
        {
            metersBack = recordedLength;
        }

        // query recorder
        if (!recorder.TryGetPositionAtDistanceBack(metersBack, out Vector2 target))
        {
            // no data yet
            return;
        }

        // If other NPCs are very close to us, add tiny lateral offset to avoid exact overlapping (non-physics)
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
            return;
        }

        float smoothTime = Mathf.Min(maxSmoothTime, baseSmoothTime + spawnIndex * extraSmoothPerIndex);
        Vector2 newPos = Vector2.SmoothDamp(cur, target, ref velocityRef, smoothTime, maxSpeed, Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        Vector2 moveDir = (newPos - cur);
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            rb.MoveRotation(angle);
        }
    }
}
