using System.Collections;
using UnityEngine;

public class GroundEnemyAI : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Patrol Points")]
    [SerializeField] private float pointA;
    [SerializeField] private float pointB;
    [SerializeField] private float boxHeight = 10f;
    public float waitTime = 2f;
    public float reachThreshold = 0.1f;

    [Header("Detection")]
    [SerializeField] private LayerMask playerLayer;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform player;

    private bool isChasing = false;
    private Coroutine patrolCoroutine;

    private float minX;
    private float maxX;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        minX = transform.position.x + pointA;
        maxX = transform.position.x + pointB;

        patrolCoroutine = StartCoroutine(Patrolling());
    }

    private void Update()
    {
        DetectPlayer();
        if (isChasing) ChasePlayer();
    }

    private void DetectPlayer()
    {
        Vector2 center = new Vector2((minX + maxX) / 2f, transform.position.y);
        float width = maxX - minX;

        Collider2D hit = Physics2D.OverlapBox(center, new Vector2(width, boxHeight), 0f, playerLayer);

        if (hit != null && !isChasing)
        {
            isChasing = true;
            player = hit.transform;
            if (patrolCoroutine != null)
            {
                StopCoroutine(patrolCoroutine);
                patrolCoroutine = null;
            }
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else if (hit == null && isChasing)
        {
            isChasing = false;
            player = null;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            patrolCoroutine = StartCoroutine(Patrolling());
        }
    }

    private void ChasePlayer()
    {
        if (player == null) return;

        float distX = player.position.x - transform.position.x;

        if (Mathf.Abs(distX) < 0.1f)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float dirX = distX > 0 ? 1f : -1f;
        Flip(dirX);
        rb.linearVelocity = new Vector2(dirX * moveSpeed, rb.linearVelocity.y);
    }

    private IEnumerator Patrolling()
    {
        while (true)
        {
            float targetX = Random.Range(minX, maxX);

            while (Mathf.Abs(transform.position.x - targetX) > reachThreshold)
            {
                float dirX = targetX > transform.position.x ? 1f : -1f;
                Flip(dirX);
                rb.linearVelocity = new Vector2(moveSpeed * dirX, rb.linearVelocity.y);
                yield return null;
            }

            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private void Flip(float dirX)
    {
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x) * dirX,
            transform.localScale.y,
            transform.localScale.z
        );
    }

    private void OnDrawGizmosSelected()
    {
        float gizmoMinX = Application.isPlaying ? minX : transform.position.x + pointA;
        float gizmoMaxX = Application.isPlaying ? maxX : transform.position.x + pointB;
        Vector2 center = new Vector2((gizmoMinX + gizmoMaxX) / 2f, transform.position.y);

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.15f);
        Gizmos.DrawCube(center, new Vector2(gizmoMaxX - gizmoMinX, boxHeight));
        Gizmos.color = new Color(0f, 0.5f, 1f, 1f);
        Gizmos.DrawWireCube(center, new Vector2(gizmoMaxX - gizmoMinX, boxHeight));

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(new Vector3(gizmoMinX, transform.position.y), 0.15f);
        Gizmos.DrawSphere(new Vector3(gizmoMaxX, transform.position.y), 0.15f);
    }
}