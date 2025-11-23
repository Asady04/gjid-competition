using UnityEngine;
using System.Collections;

public class EnemyCommander : MonoBehaviour
{
    private EnemyAI enemy;
    private Rigidbody2D rb;
    private Vector3 originalPosition;
    private bool isBusy = false;
    private EnemyAI.PatrolMode savedMode;

    public float moveSpeedMultiplier = 1.2f;

    void Awake()
    {
        enemy = GetComponent<EnemyAI>();
        rb = GetComponent<Rigidbody2D>();
        originalPosition = transform.position;
    }

    public void InvestigateRadio(Vector3 radioPosition, float duration)
    {
        if (isBusy) return;
        StartCoroutine(GoAndReturn(radioPosition, duration));
    }

    private IEnumerator GoAndReturn(Vector3 radioPos, float waitTime)
    {
        isBusy = true;

        savedMode = enemy.patrolMode;
        enemy.patrolMode = EnemyAI.PatrolMode.Idle;
        enemy.enabled = false; // MATIKAN EnemyAI SEMENTARA

        yield return MoveUntilReached(radioPos);

        yield return new WaitForSeconds(waitTime);

        yield return MoveUntilReached(originalPosition);

        enemy.enabled = true;
        enemy.patrolMode = savedMode;

        isBusy = false;
    }

    private IEnumerator MoveUntilReached(Vector3 target)
    {
        while (Vector2.Distance(transform.position, target) > 0.25f)
        {
            Vector2 direction = (target - transform.position).normalized;
            rb.MovePosition(rb.position + direction * enemy.patrolSpeed * moveSpeedMultiplier * Time.deltaTime);
            yield return null;
        }
    }
}
