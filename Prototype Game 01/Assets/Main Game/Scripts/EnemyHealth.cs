using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 1;
    private float currentHealth;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 5f;

    [Header("Hit Flash")]
    [SerializeField] private float flashDuration = 0.1f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Color originalColor;
    private bool isHit = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        originalColor = sr.color;
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage, Vector2 attackerPosition)
    {
        if (isHit) return;

        currentHealth -= damage;

        Vector2 knockbackDir = ((Vector2)transform.position - attackerPosition).normalized;
        rb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);

        StartCoroutine(HitFlash());

        if (currentHealth <= 0) Die();
    }

    private IEnumerator HitFlash()
    {
        isHit = true;
        sr.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        sr.color = originalColor;
        isHit = false;
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}