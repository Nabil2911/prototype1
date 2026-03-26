using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 5f;
    private float currentHealth;

    [Header("Knockback")]
    [SerializeField] private float knockbackX = 5f;
    [SerializeField] private float knockbackY = 3f;

    [Header("Hit Flash")]
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private float invincibleDuration = 0.5f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Color originalColor;

    public bool isHit = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        originalColor = sr.color;
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage, Vector2 knockbackDir)
    {
        if (isHit) return;

        currentHealth -= damage;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(knockbackDir.x * knockbackX, knockbackY), ForceMode2D.Impulse);
        StartCoroutine(HitFlash());

        if (currentHealth <= 0) Die();
    }

    private IEnumerator HitFlash()
    {
        isHit = true;
        sr.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        sr.color = originalColor;
        yield return new WaitForSeconds(invincibleDuration - flashDuration);
        isHit = false;
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}