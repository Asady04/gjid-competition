using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f;

    private Rigidbody2D rb;
    private Vector2 input;

    public Transform spriteTransform;

    [Header("Animation")]
    public Animator animator;
    public string animSpeedParam = "Speed";

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        // Set animation speed
        if (animator != null)
            animator.SetFloat(animSpeedParam, input.magnitude * moveSpeed);

        // Flip sprite
        if (input.x > 0.01f)
            spriteTransform.localScale = new Vector3(-1, 1, 1);
        else if (input.x < -0.01f)
            spriteTransform.localScale = new Vector3(1, 1, 1);
    }

    void FixedUpdate()
    {
        Vector2 targetPos = rb.position + input * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPos);

        if (input.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            float newAngle = Mathf.MoveTowardsAngle(rb.rotation, angle, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newAngle);
        }
    }
}
