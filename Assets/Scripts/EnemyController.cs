using System.Collections;
using UnityEngine;

// ================================================================
//  ENEMY CONTROLLER — Beat'em Up Core
//  Basado en tu EnemyIA original. Conserva el mismo estilo
//  (enum de estado, FindTarget, FixedUpdate para movimiento).
//
//  Añadidos:
//    · Estados: Hurt, Dead  (+ Patrolling opcional)
//    · HealthComponent integrado
//    · Hitbox spawner (mismo prefab que el jugador)
//    · ReceiveHit() para que la Hitbox del jugador lo llame
//    · Rango de detección: el enemigo no persigue desde el inicio
// ================================================================

public class EnemyController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed   = 3f;
    public float detectRange = 6f;    // distancia para empezar a perseguir
    public float attackRange = 1.5f;

    [Header("Combate")]
    public float attackCooldown = 1.2f;
    public float attackDamage   = 10f;
    public float knockbackForce = 4f;
    

    [Header("Hitbox")]
    public GameObject hitboxPrefab;      // mismo prefab que usa el jugador
    public Transform  hitboxSpawnPoint;  // hijo del enemigo, delante del sprite
    public Vector2    hitboxSize     = new(1.2f, 1f);
    public float      hitboxDelay    = 0.15f;   // segundos hasta activar la hitbox
    public float      hitboxDuration = 0.12f;

    [Header("Patrol (opcional)")]
    public Transform[] patrolPoints;     // déjalo vacío para desactivar el patrullaje

    [Header("Daño recibido")]
    public float hurtDuration = 0.35f;

    [Header("Referencias")]
    public Transform   target;
    public Rigidbody2D rb;
    public Animator    animator;
    public HitBox hb;
    

    // ── Estado ────────────────────────────────────────────
    private enum State { Idle, Patrolling, Chasing, Attacking, Hurt, Dead }
    private State _state = State.Idle;

    // ── Combate ───────────────────────────────────────────
    private float     _attackTimer;
    private Coroutine _attackCoroutine;
    private Coroutine _hurtCoroutine;

    // ── Patrol ────────────────────────────────────────────
    private int _patrolIndex = 0;

    // ── Health ────────────────────────────────────────────
    private HealthComponent _health;

    // ==========================================================
    //  INIT
    // ==========================================================

    void Awake()
    {
        if (rb       == null) rb       = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (hb       == null) hb       = GetComponent<HitBox>();
        
        rb.gravityScale = 0;

        _health = GetComponent<HealthComponent>();
        if (_health == null) _health = gameObject.AddComponent<HealthComponent>();

        _health.OnDeath         += OnDeath;
        _health.OnHealthChanged += (cur, max) => Debug.Log($"[Enemy] HP: {cur}/{max}");

        // BUG FIX: conectar la HurtBox propia al sistema de daño.
        // Sin esto, los golpes del jugador nunca aplicaban daño real.
        HurtBox ownHurtBox = GetComponent<HurtBox>();
        if (ownHurtBox != null)
            ownHurtBox.OnHitReceived += OnHurtBoxHit;
    }

    void Start()
    {
        FindTarget();
        _state = patrolPoints != null && patrolPoints.Length > 0
            ? State.Patrolling
            : State.Idle;
    }

    // ==========================================================
    //  UPDATE — MÁQUINA DE ESTADOS
    // ==========================================================

    void Update()
    {
        if (_state == State.Dead || _state == State.Hurt) return;
        if (target == null) return;

        float distance = Vector2.Distance(transform.position, target.position);

        switch (_state)
        {
            case State.Idle:
                if (distance <= detectRange) _state = State.Chasing;
                break;

            case State.Patrolling:
                if (distance <= detectRange) _state = State.Chasing;
                break;

            case State.Chasing:
                if (distance <= attackRange)
                {
                    rb.linearVelocity = Vector2.zero;
                    animator.SetFloat("Speed", 0);
                    _state       = State.Attacking;
                    _attackTimer = attackCooldown;
                }
                else if (distance > detectRange * 1.5f)
                {
                    _state = patrolPoints != null && patrolPoints.Length > 0
                        ? State.Patrolling
                        : State.Idle;
                }
                break;

            case State.Attacking:
                _attackTimer -= Time.deltaTime;

                if (distance > attackRange)
                {
                    _state = State.Chasing;
                    break;
                }

                if (_attackTimer <= 0f)
                {
                    _attackTimer = attackCooldown;
                    if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
                    _attackCoroutine = StartCoroutine(AttackRoutine());
                }
                break;
        }
    }

    void FixedUpdate()
    {
        if (_state == State.Chasing)    MoveToTarget();
        if (_state == State.Patrolling) Patrol();
    }

    // ==========================================================
    //  MOVIMIENTO
    // ==========================================================

    void MoveToTarget()
    {
        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        animator.SetFloat("Speed", rb.linearVelocity.magnitude);
        FaceDirection(direction.x);
    }

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Transform wp  = patrolPoints[_patrolIndex];
        Vector2   dir = ((Vector2)wp.position - (Vector2)transform.position).normalized;

        rb.linearVelocity = dir * moveSpeed * 0.5f;
        animator.SetFloat("Speed", rb.linearVelocity.magnitude);
        FaceDirection(dir.x);

        if (Vector2.Distance(transform.position, wp.position) < 0.25f)
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
    }

    void FaceDirection(float xDir)
    {
        if (xDir == 0) return;
        transform.localScale = new Vector3(Mathf.Sign(xDir), 1f, 1f);
    }

    // ==========================================================
    //  COMBATE — ATAQUE
    // ==========================================================

    IEnumerator AttackRoutine()
    {
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(hitboxDelay);
        SpawnHitbox();
    }

    void SpawnHitbox()
    {
        if (hitboxPrefab == null || hitboxSpawnPoint == null) return;

        GameObject go = Instantiate(
            hitboxPrefab,
            hitboxSpawnPoint.position,
            Quaternion.identity);

        // BUG FIX: el HitBox se instanciaba pero nunca se activaba.
        // HitBox.Awake() llama a Deactivate(), así que el collider
        // quedaba deshabilitado y jamás detectaba colisiones con el jugador.
        HitBox spawnedHitBox = go.GetComponent<HitBox>();
        if (spawnedHitBox != null)
            spawnedHitBox.Activate();

        Destroy(go, hitboxDuration);
    }

    // ==========================================================
    //  RECIBIR DAÑO  (llamado desde la Hitbox del jugador)
    // ==========================================================

    // BUG FIX: callback del evento HurtBox.OnHitReceived.
    // Antes no existía esta suscripción, así que los golpes del jugador
    // llegaban a la HurtBox pero nunca disparaban la lógica del enemigo.
    private void OnHurtBoxHit(HitData data)
    {
        ReceiveHit(data.damage);
    }

    public void ReceiveHit(float damage)
    {
        if (_state == State.Dead) return;

        // BUG FIX: el daño nunca se aplicaba a _health.
        // ReceiveHit recibía el golpe pero descartaba el valor 'damage'.
        _health.TakeDamage(damage);

        if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
        if (_hurtCoroutine   != null) StopCoroutine(_hurtCoroutine);

        _hurtCoroutine = StartCoroutine(HurtRoutine());
    }

    IEnumerator HurtRoutine()
    {
        _state = State.Hurt;
        rb.linearVelocity = Vector2.zero;
        animator.SetFloat("Speed", 0);
        animator.SetTrigger("Hurt");

        yield return new WaitForSeconds(hurtDuration);

        if (_state != State.Dead)
            _state = State.Chasing;
    }

    // ==========================================================
    //  MUERTE
    // ==========================================================

    void OnDeath()
    {
        _state = State.Dead;
        rb.linearVelocity = Vector2.zero;
        animator.SetTrigger("Die");

        if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
        if (_hurtCoroutine   != null) StopCoroutine(_hurtCoroutine);

        GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject, 2.5f);
    }

    // ==========================================================
    //  UTILIDADES
    // ==========================================================

    void FindTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
