using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float jumpForce = 16f;

    [Header("Jump Control")]
    public float lowJumpMultiplier = 2f;   // released jump -> stronger gravity -> short hop
    public float fallMultiplier = 2.5f;    // falling -> stronger gravity
    public float maxJumpHoldTime = 0.5f;   // Maximum time to hold jump for max height
    public float additionalJumpForce = 10f; // Additional force per second when holding jump

    [Header("Dash")]
    public float dashForce = 20f;
    public float dashDuration = 0.2f;
    private bool isDashing;
    private bool dashUsed;
    private float originalGravityScale;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Wall Slide")]
    public Transform wallCheck;
    public float wallCheckDistance = 0.25f;
    public float wallSlideSpeed = 2f;
    private bool isTouchingWall;
    private bool isWallSliding;

    private Rigidbody2D rb;
    private bool isGrounded;
    private float jumpHoldTime; // Tracks how long jump is held

    private PhysicsMaterial2D noFrictionMat;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;

        noFrictionMat = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
        rb.sharedMaterial = noFrictionMat;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        bool jumpPressed = Input.GetButtonDown("Jump");
        bool jumpHeld = Input.GetButton("Jump"); // Check if jump is still held
        bool dashPressed = Input.GetKeyDown(KeyCode.LeftShift);

        // Ground check
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded)
        {
            dashUsed = false;
            jumpHoldTime = 0f; // Reset jump hold time when grounded
        }

        // Wall checks
        RaycastHit2D hitRight = Physics2D.Raycast(wallCheck.position, Vector2.right, wallCheckDistance, groundLayer);
        RaycastHit2D hitLeft = Physics2D.Raycast(wallCheck.position, Vector2.left, wallCheckDistance, groundLayer);
        bool onRight = hitRight.collider != null;
        bool onLeft = hitLeft.collider != null;
        isTouchingWall = onRight || onLeft;
        int wallDir = onRight ? 1 : (onLeft ? -1 : 0);

        // Dash handling
        if (dashPressed && !dashUsed && !isDashing)
        {
            float facing = transform.localScale.x >= 0 ? 1f : -1f;
            float dashDir = Mathf.Abs(moveInput) > 0.1f ? Mathf.Sign(moveInput) : facing;
            StartDash(dashDir);
        }

        // Wall slide decision
        bool pressingIntoWall = (moveInput > 0f && onRight) || (moveInput < 0f && onLeft);
        bool shouldStick = isTouchingWall && !isGrounded && pressingIntoWall;

        // Movement
        if (!isDashing)
        {
            if (shouldStick)
            {
                if (!isWallSliding)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
                isWallSliding = true;
            }
            else
            {
                isWallSliding = false;
                rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

                if (moveInput > 0) transform.localScale = new Vector3(1, 1, 1);
                else if (moveInput < 0) transform.localScale = new Vector3(-1, 1, 1);
            }
        }

        // Jump logic
        if (jumpPressed && isGrounded && !isDashing)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
        else if (jumpPressed && isWallSliding && wallDir != 0 && !isDashing)
        {
            float jumpDir = -wallDir;
            rb.linearVelocity = new Vector2(jumpDir * moveSpeed * 0.75f, jumpForce);
            transform.localScale = new Vector3(jumpDir, 1, 1);
            isWallSliding = false;
        }

        // Variable jump height
        if (jumpHeld && rb.linearVelocity.y > 0f && !isDashing)
        {
            jumpHoldTime += Time.deltaTime;
            if (jumpHoldTime < maxJumpHoldTime)
            {
                float additionalForce = additionalJumpForce * Time.deltaTime;
                rb.linearVelocity += new Vector2(0f, additionalForce);
            }
        }

        // Better jump
        if (!isDashing)
        {
            if (rb.linearVelocity.y < 0f)
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.deltaTime;
            }
            else if (rb.linearVelocity.y > 0f && !jumpHeld)
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.deltaTime;
            }
        }

        // Wall slide clamp
        if (isWallSliding && rb.linearVelocity.y < -wallSlideSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
    }

    private void FixedUpdate()
    {
        // Intentionally left empty — velocities are set in Update for responsiveness
    }

    private void StartDash(float dashDir)
    {
        isDashing = true;
        dashUsed = true;

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dashDir * dashForce, 0f);

        Invoke(nameof(EndDash), dashDuration);
    }

    private void EndDash()
    {
        rb.gravityScale = originalGravityScale;
        isDashing = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.right * wallCheckDistance);
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.left * wallCheckDistance);
        }
    }
}