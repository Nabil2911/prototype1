using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private float damage = 1f;

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!other.gameObject.CompareTag("Player")) return;

        if (other.transform.position.y > transform.position.y + 0.2f) return;

        PlayerHealth playerHealth = other.gameObject.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            Vector2 knockbackDir = new Vector2(
                Mathf.Sign(other.transform.position.x - transform.position.x),
                0f
            );
            playerHealth.TakeDamage(damage, knockbackDir);
        }
    }
}