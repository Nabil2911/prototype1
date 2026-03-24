using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Setting")]
    [SerializeField] private float moveSpeed = 5;
    [SerializeField] private float jumpForce = 5;

    [Header("Ground Check")]
    [SerializeField] private Transform groundPoint;
    [SerializeField] private LayerMask groundLayer;

    private bool isGround;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        isGround = Physics2D.OverlapCapsule(groundPoint.position, new Vector2(1, 0.1f),
        CapsuleDirection2D.Horizontal, 0, groundLayer);

        MovementLogic();
        JumpLogic();
    }

    private void MovementLogic()
    {
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            rb.linearVelocity = new Vector2(-moveSpeed, rb.linearVelocity.y);
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private void JumpLogic()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (isGround)
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }
        }
    }
}