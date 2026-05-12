using UnityEngine;
using UnityEngine.InputSystem;

public class ControlPlayer1 : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;

    [Header("Salto (visual)")]
    public float jumpForce = 5f;
    public float gravity = -15f;

    [Header("Combate")]
    public float attackDuration = 0.3f;

    [Header("Referencias")]
    public Rigidbody2D rb;
    public Animator animator;

    private Vector2 moveInput;

    private enum State { Normal, Attacking }
    private State currentState = State.Normal;

    private float attackTimer;

    // salto fake
    private float verticalVelocity;
    private float height;
    private bool isJumping;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        rb.gravityScale = 0; // MUY IMPORTANTE
    }

    // ================= INPUT =================

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && !isJumping && currentState == State.Normal)
        {
            isJumping = true;
            verticalVelocity = jumpForce;
            animator.SetTrigger("Jump");
        }
    }

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        if (context.performed) TryAttack("LightAttack");
    }

    public void OnMediumAttack(InputAction.CallbackContext context)
    {
        if (context.performed) TryAttack("MediumAttack");
    }

    public void OnHeavyAttack(InputAction.CallbackContext context)
    {
        if (context.performed) TryAttack("HeavyAttack");
    }

    // ================= ATAQUE =================

    void TryAttack(string trigger)
    {
        if (currentState != State.Normal) return;

        if (isJumping)
            trigger = "AirAttack";

        currentState = State.Attacking;
        attackTimer = attackDuration;

        rb.linearVelocity = Vector2.zero;

        animator.SetTrigger(trigger);
    }

    // ================= UPDATE =================

    void Update()
    {
        HandleState();
        HandleJump();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleState()
    {
        if (currentState == State.Attacking)
        {
            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f)
                currentState = State.Normal;
        }
    }

    void HandleMovement()
    {
        if (currentState != State.Normal) return;

        Vector2 velocity = moveInput * moveSpeed;
        rb.linearVelocity = velocity;

        animator.SetFloat("Speed", moveInput.magnitude);

        // girar personaje
        if (moveInput.x != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(moveInput.x), 1, 1);
        }
    }

    // ================= SALTO FAKE =================

    void HandleJump()
    {
        if (!isJumping) return;

        verticalVelocity += gravity * Time.deltaTime;
        height += verticalVelocity * Time.deltaTime;

        if (height <= 0)
        {
            height = 0;
            isJumping = false;
        }

        // aplicamos altura visual
        transform.position = new Vector3(
            transform.position.x,
            transform.position.y,
            -height // eje Z como altura visual
        );
    }
}