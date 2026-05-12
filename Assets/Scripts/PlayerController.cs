using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// Controlador Beat 'em Up con Rigidbody2D KINÉMÁTICO.
///
/// MODELO DE 3 EJES (clave del género):
///   · posX       → posición horizontal (A/D).
///   · floorY     → profundidad de calle (W/S). Es la Y "del suelo".
///   · jumpHeight → altura del salto sobre floorY. Gravedad simulada a mano.
///
/// La Y visible del sprite es siempre:  floorY + jumpHeight
/// Por eso saltar nunca pisa el movimiento de profundidad y al revés.
///
/// Requiere: Rigidbody2D (Body Type = Kinematic), Animator, PlayerInput.
/// El Move del InputAction debe ser un 2D Vector Composite (W/A/S/D).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float depthSpeed = 3f;

    [Header("Jump (gravedad simulada)")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float jumpLaunchForce = 14f;
    [SerializeField] private float gravity = 35f;
    [SerializeField] private float jumpCutFactor = 0.5f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("Scene Bounds")]
    [SerializeField] private float xMin = -8f;
    [SerializeField] private float xMax = 8f;
    [SerializeField] private float floorMin = -2.5f;
    [SerializeField] private float floorMax = -0.5f;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction attackAction;

    private Rigidbody2D rb;
    private Animator anim;

    private float floorY;
    private float jumpHeight;
    private float jumpVelocity;
    private bool isGrounded => jumpHeight <= 0.001f;

    private bool isJumpPressed;
    private bool jumpCutApplied;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool movementLocked;

    public event Action OnAttackPressed;

    public bool IsGrounded => isGrounded;
    public Rigidbody2D Rb => rb;
    public Animator Anim => anim;
    public float JumpLaunch => jumpLaunchForce;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions.FindAction("Move");
        jumpAction = playerInput.actions.FindAction("Jump");
        attackAction = playerInput.actions.FindAction("Attack");
    }

    private void Start()
    {
        floorY = Mathf.Clamp(transform.position.y, floorMin, floorMax);
        jumpHeight = 0f;
    }

    private void OnEnable()
    {
        moveAction.Enable();

        jumpAction.Enable();
        jumpAction.performed += OnJump;
        jumpAction.canceled += OnJump;

        attackAction.Enable();
        attackAction.performed += OnAttack;
    }

    private void OnDisable()
    {
        moveAction.Disable();

        jumpAction.performed -= OnJump;
        jumpAction.canceled -= OnJump;
        jumpAction.Disable();

        attackAction.performed -= OnAttack;
        attackAction.Disable();
    }

    private void Update()
    {
        jumpBufferCounter = isJumpPressed
            ? jumpBufferTime
            : jumpBufferCounter - Time.deltaTime;

        coyoteCounter = isGrounded
            ? coyoteTime
            : coyoteCounter - Time.deltaTime;

        Vector2 input = movementLocked ? Vector2.zero : moveAction.ReadValue<Vector2>();
        anim.SetFloat("Speed", Mathf.Abs(input.x));
        anim.SetBool("IsGrounded", isGrounded);
    }

    private void FixedUpdate()
    {
        Vector2 input = movementLocked ? Vector2.zero : moveAction.ReadValue<Vector2>();
        float dt = Time.fixedDeltaTime;

        float posX = HandleHorizontal(input, dt);
        HandleDepth(input, dt);
        HandleJump(dt);

        Vector2 finalPos = new Vector2(posX, floorY + jumpHeight);
        rb.MovePosition(finalPos);
    }

    private float HandleHorizontal(Vector2 input, float dt)
    {
        float newX = rb.position.x + input.x * runSpeed * dt;
        newX = Mathf.Clamp(newX, xMin, xMax);

        if (input.x != 0f)
            transform.localScale = new Vector3(Mathf.Sign(input.x), 1f, 1f);

        return newX;
    }

    private void HandleDepth(Vector2 input, float dt)
    {
        if (!isGrounded || input.y == 0f) return;

        floorY += input.y * depthSpeed * dt;
        floorY = Mathf.Clamp(floorY, floorMin, floorMax);
    }

    private void HandleJump(float dt)
    {
        if (jumpBufferCounter > 0f && coyoteCounter > 0f && isGrounded)
        {
            jumpVelocity = jumpForce;
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            jumpCutApplied = false;
            anim.SetTrigger("Jump");
        }

        if (!isJumpPressed && jumpVelocity > 0f && !jumpCutApplied)
        {
            jumpVelocity *= jumpCutFactor;
            jumpCutApplied = true;
        }

        if (!isGrounded || jumpVelocity > 0f)
        {
            jumpVelocity -= gravity * dt;
            jumpHeight += jumpVelocity * dt;

            if (jumpHeight <= 0f)
            {
                jumpHeight = 0f;
                jumpVelocity = 0f;
            }
        }
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            isJumpPressed = true;
            jumpCutApplied = false;
        }
        else if (ctx.canceled)
        {
            isJumpPressed = false;
        }
    }

    private void OnAttack(InputAction.CallbackContext ctx)
    {
        OnAttackPressed?.Invoke();
    }

    public void LaunchUpward()
    {
        jumpVelocity = jumpLaunchForce;
    }

    public void SetMovementLocked(bool locked) => movementLocked = locked;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3((xMin + xMax) / 2f, (floorMin + floorMax) / 2f, 0f);
        Vector3 size = new Vector3(xMax - xMin, floorMax - floorMin, 0f);
        Gizmos.DrawWireCube(center, size);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(xMin, floorY, 0f), new Vector3(xMax, floorY, 0f));
        }
    }
}