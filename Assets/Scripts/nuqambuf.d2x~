using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// Controla movimiento, salto y expone la API que necesita PlayerCombat.
/// Requiere: Rigidbody2D, Animator, PlayerInput (Input System).
///
/// Beat 'em Up:
///   · A/D  → movimiento lateral (X), con flip de sprite automático.
///   · W/S  → profundidad de calle (Y de pantalla), solo en suelo.
///   · Espacio/botón Sur → salto con gravedad real (Rigidbody2D).
///
/// Ground check doble:
///   · OverlapCircle en cada FixedUpdate (no depende de tags ni colisionadores perfectos).
///   · OnCollisionEnter/Exit2D como refuerzo adicional.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Inspector — Movement
    // ──────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float runSpeed   = 6f;
    [SerializeField] private float depthSpeed = 3f;

    [Header("Jump")]
    [SerializeField] private float jumpForce       = 12f;
    [SerializeField] private float jumpLaunchForce = 10f;
    [SerializeField] private float coyoteTime      = 0.15f;
    [SerializeField] private float jumpBufferTime  = 0.15f;

    // ──────────────────────────────────────────────
    //  Inspector — Ground check
    // ──────────────────────────────────────────────
    [Header("Ground Check")]
    [Tooltip("Punto de detección de suelo — crea un hijo vacío en los pies del personaje y arrástralo aquí")]
    [SerializeField] private Transform groundCheck;
    [Tooltip("Radio del OverlapCircle de detección")]
    [SerializeField] private float groundCheckRadius = 0.15f;
    [Tooltip("Layer(s) que se consideran suelo. Si se deja en Nothing detecta todos.")]
    [SerializeField] private LayerMask groundLayer;

    // ──────────────────────────────────────────────
    //  Inspector — Scene Bounds
    // ──────────────────────────────────────────────
    [Header("Scene Bounds")]
    [SerializeField] private float xMin = -8f;
    [SerializeField] private float xMax =  8f;
    [SerializeField] private float yMin = -2.5f;
    [SerializeField] private float yMax = -0.5f;

    // ──────────────────────────────────────────────
    //  Input Actions
    // ──────────────────────────────────────────────
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction attackAction;

    // ──────────────────────────────────────────────
    //  Componentes
    // ──────────────────────────────────────────────
    private Rigidbody2D rb;
    private Animator    anim;

    // ──────────────────────────────────────────────
    //  Estado
    // ──────────────────────────────────────────────
    private bool  isJumpPressed;
    private bool  jumpCutApplied;
    private bool  isGrounded;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool  movementLocked;
    private float groundY;  // Y base de profundidad (plano de la calle)

    // ──────────────────────────────────────────────
    //  Eventos / API pública
    // ──────────────────────────────────────────────
    public event Action OnAttackPressed;

    public bool        IsGrounded => isGrounded;
    public Rigidbody2D Rb         => rb;
    public Animator    Anim       => anim;
    public float       JumpLaunch => jumpLaunchForce;

    // ──────────────────────────────────────────────
    //  Unity — Init
    // ──────────────────────────────────────────────
    private void Awake()
    {
        rb   = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        playerInput  = GetComponent<PlayerInput>();
        moveAction   = playerInput.actions.FindAction("Move");
        jumpAction   = playerInput.actions.FindAction("Jump");
        attackAction = playerInput.actions.FindAction("Attack");

        // Evita que el personaje rote al chocar con paredes/suelo
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Start()
    {
        groundY = Mathf.Clamp(transform.position.y, yMin, yMax);
    }

    private void OnEnable()
    {
        // BUG FIX: moveAction SIEMPRE se habilita.
        // Antes solo se habilitaba si autoRun=false, rompiendo el movimiento
        // de profundidad W/S y dejando al jugador sin ningún control.
        moveAction.Enable();

        jumpAction.Enable();
        jumpAction.performed += OnJump;
        jumpAction.canceled  += OnJump;

        attackAction.Enable();
        attackAction.performed += OnAttack;
    }

    private void OnDisable()
    {
        moveAction.Disable();

        jumpAction.performed -= OnJump;
        jumpAction.canceled  -= OnJump;
        jumpAction.Disable();

        attackAction.performed -= OnAttack;
        attackAction.Disable();
    }

    // ──────────────────────────────────────────────
    //  Unity — Loop
    // ──────────────────────────────────────────────
    private void Update()
    {
        jumpBufferCounter = isJumpPressed
            ? jumpBufferTime
            : jumpBufferCounter - Time.deltaTime;

        coyoteCounter = isGrounded
            ? coyoteTime
            : coyoteCounter - Time.deltaTime;

        float speedX = movementLocked ? 0f : Mathf.Abs(rb.linearVelocity.x);
        anim.SetFloat("Speed", speedX);
    }

    private void FixedUpdate()
    {
        CheckGround();
        HandleMovement();
        HandleDepth();
        HandleJump();
    }

    private void LateUpdate()
    {
        // Red de seguridad: impide salir de los límites de escena
        Vector2 pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, xMin, xMax);
        if (isGrounded)
            pos.y = Mathf.Clamp(pos.y, yMin, yMax);
        rb.position = pos;
    }

    // ──────────────────────────────────────────────
    //  Ground check — OverlapCircle
    // ──────────────────────────────────────────────
    /// <summary>
    /// BUG FIX: la detección anterior solo usaba OnCollisionEnter2D,
    /// que falla si el tag "Ground" no está puesto o si el personaje
    /// está en el borde de un collider. OverlapCircle es fiable siempre.
    ///
    /// Si groundCheck no está asignado en el Inspector, se usa un punto
    /// automático justo por debajo del Transform como fallback.
    /// Si groundLayer no está asignado (valor 0), detecta cualquier layer
    /// excepto el del propio personaje.
    /// </summary>
    private void CheckGround()
    {
        Vector2 origin = groundCheck != null
            ? (Vector2)groundCheck.position
            : (Vector2)transform.position + Vector2.down * 0.5f;

        bool wasGrounded = isGrounded;

        // Recoger todos los colliders en el radio
        Collider2D[] hits = groundLayer == 0
            ? Physics2D.OverlapCircleAll(origin, groundCheckRadius)
            : Physics2D.OverlapCircleAll(origin, groundCheckRadius, groundLayer);

        // Ignorar el propio personaje y sus hijos
        isGrounded = false;
        foreach (var c in hits)
        {
            if (c.transform.root != transform.root)
            {
                isGrounded = true;
                break;
            }
        }

        // Sincronizar Animator solo cuando cambia el estado
        if (isGrounded != wasGrounded)
            anim.SetBool("IsGrounded", isGrounded);

        // Al aterrizar: anclar groundY
        if (isGrounded && !wasGrounded)
            groundY = Mathf.Clamp(transform.position.y, yMin, yMax);
    }

    // ──────────────────────────────────────────────
    //  Movimiento lateral (X)
    // ──────────────────────────────────────────────
    private void HandleMovement()
    {
        if (movementLocked)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // BUG FIX: antes "autoRun = true" hacía que el personaje se moviera
        // siempre a la derecha sin que el jugador pudiera controlarlo.
        // Ahora el jugador SIEMPRE controla el movimiento con A/D o el stick.
        Vector2 input     = moveAction.ReadValue<Vector2>();
        float targetSpeed = input.x * runSpeed;

        if (input.x != 0f)
            transform.localScale = new Vector3(Mathf.Sign(input.x), 1f, 1f);

        rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
    }

    // ──────────────────────────────────────────────
    //  Movimiento de profundidad (Y de pantalla)
    // ──────────────────────────────────────────────
    private void HandleDepth()
    {
        if (movementLocked || !isGrounded) return;

        Vector2 input = moveAction.ReadValue<Vector2>();
        if (input.y == 0f) return;

        float newY = groundY + input.y * depthSpeed * Time.fixedDeltaTime;
        groundY = Mathf.Clamp(newY, yMin, yMax);

        // Transform directo, sin Rigidbody, para no interferir con el salto
        Vector3 pos = transform.position;
        pos.y = groundY;
        transform.position = pos;
    }

    // ──────────────────────────────────────────────
    //  Salto
    // ──────────────────────────────────────────────
    private void HandleJump()
    {
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            anim.SetTrigger("Jump");

            isGrounded        = false;
            jumpBufferCounter = 0f;
            coyoteCounter     = 0f;
            jumpCutApplied    = false;
        }

        // Jump cut: soltar el botón acorta el salto. Se aplica UNA sola vez.
        if (!isJumpPressed && rb.linearVelocity.y > 0f && !jumpCutApplied)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            jumpCutApplied    = true;
        }
    }

    // ──────────────────────────────────────────────
    //  Callbacks de Input
    // ──────────────────────────────────────────────
    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            isJumpPressed  = true;
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

    // ──────────────────────────────────────────────
    //  Colisiones — refuerzo al OverlapCircle
    // ──────────────────────────────────────────────
    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            anim.SetBool("IsGrounded", true);
            groundY = Mathf.Clamp(transform.position.y, yMin, yMax);
        }
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
            anim.SetBool("IsGrounded", false);
        }
    }

    // ──────────────────────────────────────────────
    //  API pública para PlayerCombat
    // ──────────────────────────────────────────────
    public void LaunchUpward()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpLaunchForce);
        anim.SetBool("IsGrounded", false);
    }

    public void SetMovementLocked(bool locked) => movementLocked = locked;

    // ──────────────────────────────────────────────
    //  Gizmos
    // ──────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // Rectángulo de movimiento válido
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3((xMin + xMax) / 2f, (yMin + yMax) / 2f, 0f);
        Vector3 size   = new Vector3(xMax - xMin, yMax - yMin, 0f);
        Gizmos.DrawWireCube(center, size);

        // Punto de ground check
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
