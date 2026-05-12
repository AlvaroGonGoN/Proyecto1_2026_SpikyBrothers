using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de combos Beat em Up con Hit Stop.
///
/// HIT STOP: al conectar un golpe se congela Time.timeScale brevemente
/// para dar sensación de impacto. Los golpes fuertes duran más.
/// El Animator del jugador corre en UnscaledTime para que su animación
/// no se congele junto con el mundo.
///
/// CÓMO ACTIVAR EL HIT STOP:
///   Opción A (recomendada) – Animation Event en el frame de impacto:
///     · Llama a TriggerHitStop(0) → duración ligera
///     · Llama a TriggerHitStop(1) → duración fuerte
///   Opción B – automático al lanzar el ataque (sin eventos de animación):
///     Activa 'triggerHitStopOnAttack' en el Inspector.
///
/// COMBOS:
///   Tierra 1 hit  → Attack1
///   Tierra 2 hits → Attack2
///   Tierra 3 hits → Attack3Strong
///   Tierra 3 hits + Jump → AirAttackStrong (lanzamiento)
///   Aire 1 hit    → AirAttack1
///   Aire 2 hits   → AirAttack2Strong
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerCombat : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Inspector – tiempos de ataque
    // ──────────────────────────────────────────────
    [Header("Duración de ataques (segundos)")]
    [SerializeField] private float lightGroundDuration = 0.30f;
    [SerializeField] private float strongGroundDuration = 0.50f;
    [SerializeField] private float lightAirDuration = 0.35f;
    [SerializeField] private float strongAirDuration = 0.50f;

    // ──────────────────────────────────────────────
    //  Inspector – hit stop
    // ──────────────────────────────────────────────
    [Header("Hit Stop")]
    [Tooltip("Segundos que se congela el tiempo en un golpe ligero")]
    [SerializeField] private float hitStopDurationLight = 0.04f;

    [Tooltip("Segundos que se congela el tiempo en un golpe fuerte")]
    [SerializeField] private float hitStopDurationStrong = 0.12f;

    [Tooltip("Time.timeScale durante el hit stop (0 = congelado total, >0 = cámara lenta)")]
    [Range(0f, 0.5f)]
    [SerializeField] private float hitStopTimeScale = 0f;

    [Tooltip("Si está activo, el hit stop se lanza al inicio del ataque en lugar de\n" +
             "esperar un Animation Event. Útil si aún no tienes eventos de animación.")]
    [SerializeField] private bool triggerHitStopOnAttack = false;

    // ──────────────────────────────────────────────
    //  Inspector – combo
    // ──────────────────────────────────────────────
    [Header("Combo")]
    [Tooltip("Tiempo máximo entre golpes para mantener el combo")]
    [SerializeField] private float comboWindow = 0.85f;

    [Tooltip("Ventana de tiempo para detectar Jump simultáneo al 3er golpe")]
    [SerializeField] private float jumpComboWindow = 0.18f;
    public event Action OnComboExpired;

    // ──────────────────────────────────────────────
    //  Estado interno
    // ──────────────────────────────────────────────
    private int groundCombo;
    private int airCombo;
    private float comboTimer;          // usa unscaledDeltaTime → no se ve afectado por hit stop

    private bool isAttacking;
    private bool attackQueued;
    private bool isHitStopped;        // evita hit stops simultáneos

    // Clasificación del ataque en curso (para el hit stop automático)
    private bool currentAttackIsStrong;

    // Detección salto + 3er combo
    private InputAction jumpAction;
    private float jumpPressTimestamp = -999f;

    // ──────────────────────────────────────────────
    //  Referencias
    // ──────────────────────────────────────────────
    private PlayerController controller;
    private Animator anim;

    // ──────────────────────────────────────────────
    //  Unity
    // ──────────────────────────────────────────────
    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        anim = GetComponent<Animator>();

        // El Animator del jugador corre en tiempo real para que su animación
        // continúe visible mientras el mundo está congelado.
        anim.updateMode = AnimatorUpdateMode.UnscaledTime;

        var playerInput = GetComponent<PlayerInput>();
        jumpAction = playerInput.actions.FindAction("Jump");
    }

    private void OnEnable()
    {
        controller.OnAttackPressed += HandleAttackInput;

        jumpAction.Enable();
        jumpAction.performed += OnJumpPerformed;
    }

    private void OnDisable()
    {
        controller.OnAttackPressed -= HandleAttackInput;

        jumpAction.performed -= OnJumpPerformed;

        // Seguridad: restaurar timeScale si el objeto se desactiva durante un hit stop
        if (isHitStopped)
            RestoreTimeScale();
    }

    private void Update()
    {
        // Combo timer con unscaledDeltaTime para que avance incluso durante hit stop
        if (comboTimer > 0f)
        {
            comboTimer -= Time.unscaledDeltaTime;
            if (comboTimer <= 0f)
                ResetCombo();
        }
    }

    // ──────────────────────────────────────────────
    //  Callbacks de input
    // ──────────────────────────────────────────────
    private void HandleAttackInput()
    {
        if (isAttacking)
            attackQueued = true;
        else
            ExecuteAttack();
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        jumpPressTimestamp = Time.unscaledTime;
    }

    // ──────────────────────────────────────────────
    //  Lógica de combos
    // ──────────────────────────────────────────────
    private void ExecuteAttack()
    {
        if (controller.IsGrounded)
            PerformGroundAttack();
        else
            PerformAirAttack();
    }

    private void PerformGroundAttack()
    {
        airCombo = 0;
        groundCombo++;
        RefreshComboTimer();

        switch (groundCombo)
        {
            case 1:
                currentAttackIsStrong = false;
                TriggerAttack("Attack1", lightGroundDuration);
                break;

            case 2:
                currentAttackIsStrong = false;
                TriggerAttack("Attack2", lightGroundDuration);
                break;

            case 3:
            default:
                groundCombo = 0;
                currentAttackIsStrong = true;

                if (SpacePressedInWindow())
                    StartCoroutine(LaunchAndAirStrongAttack());
                else
                    TriggerAttack("Attack3Strong", strongGroundDuration);
                break;
        }
    }

    private void PerformAirAttack()
    {
        groundCombo = 0;
        airCombo++;
        RefreshComboTimer();

        switch (airCombo)
        {
            case 1:
                currentAttackIsStrong = false;
                TriggerAttack("AirAttack1", lightAirDuration);
                break;

            case 2:
            default:
                airCombo = 0;
                currentAttackIsStrong = true;
                TriggerAttack("AirAttack2Strong", strongAirDuration);
                break;
        }
    }

    private IEnumerator LaunchAndAirStrongAttack()
    {
        isAttacking = true;
        currentAttackIsStrong = true;
        controller.SetMovementLocked(true);

        yield return new WaitForSecondsRealtime(0.1f);

        controller.LaunchUpward();
        anim.SetTrigger("AirAttackStrong");

        if (triggerHitStopOnAttack)
            StartCoroutine(HitStopRoutine(hitStopDurationStrong));

        yield return new WaitForSecondsRealtime(strongAirDuration);

        FinishAttack();
    }

    // ──────────────────────────────────────────────
    //  Helpers de ataque
    // ──────────────────────────────────────────────
    private void TriggerAttack(string triggerName, float duration)
    {
        anim.SetTrigger(triggerName);

        // Opción B: hit stop automático al lanzar el ataque
        if (triggerHitStopOnAttack)
        {
            float stopDuration = currentAttackIsStrong
                ? hitStopDurationStrong
                : hitStopDurationLight;
            StartCoroutine(HitStopRoutine(stopDuration));
        }

        StartCoroutine(AttackCooldown(duration));
    }

    private IEnumerator AttackCooldown(float duration)
    {
        isAttacking = true;
        controller.SetMovementLocked(true);

        // WaitForSecondsRealtime: el cooldown avanza aunque timeScale sea 0
        yield return new WaitForSecondsRealtime(duration);

        FinishAttack();
    }

    private void FinishAttack()
    {
        isAttacking = false;
        controller.SetMovementLocked(false);

        if (attackQueued)
        {
            attackQueued = false;
            ExecuteAttack();
        }
    }

    // ──────────────────────────────────────────────
    //  Hit Stop
    // ──────────────────────────────────────────────

    /// <summary>
    /// Llamar desde un Animation Event en el frame exacto de impacto.
    /// hitType: 0 = ligero, 1 = fuerte.
    /// </summary>
    public void TriggerHitStop(int hitType)
    {
        float duration = hitType == 0 ? hitStopDurationLight : hitStopDurationStrong;
        StartCoroutine(HitStopRoutine(duration));
    }

    /// <summary>
    /// Versión con duración explícita, por si quieres llamarlo desde código
    /// (p. ej. desde un script de enemigo al recibir impacto).
    /// </summary>
    public void TriggerHitStop(float duration)
    {
        StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        // Evitar hit stops simultáneos (el más largo tiene prioridad)
        if (isHitStopped) yield break;

        isHitStopped = true;
        Time.timeScale = hitStopTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // mantener física coherente

        yield return new WaitForSecondsRealtime(duration);

        RestoreTimeScale();
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        isHitStopped = false;
    }

    // ──────────────────────────────────────────────
    //  Helpers de combo
    // ──────────────────────────────────────────────
    private void RefreshComboTimer() => comboTimer = comboWindow;

    private void ResetCombo()
    {
        groundCombo = 0;
        airCombo = 0;
        comboTimer = 0f;
        OnComboExpired?.Invoke();
    }

    private bool SpacePressedInWindow()
        => (Time.unscaledTime - jumpPressTimestamp) <= jumpComboWindow;
}

