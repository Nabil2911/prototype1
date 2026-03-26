using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float bounceForce = 10f;
    [SerializeField] private Rigidbody2D rb;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
                enemyHealth.TakeDamage(1f, knockbackDir);

                if (transform.position.y > other.transform.position.y)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
                }
            }
        }
    }
}