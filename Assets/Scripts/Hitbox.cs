// HitBox.cs
using System;
using UnityEngine;

/// <summary>
/// Volumen de detección de golpe. Debe estar en un GameObject hijo
/// con un Collider2D marcado como isTrigger, en el layer PlayerHitBox.
///
/// Se activa y desactiva desde HitBoxController (que recibe órdenes
/// de PlayerCombat o de Animation Events).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HitBox : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Inspector
    // ──────────────────────────────────────────────
    [Header("Datos del golpe")]
    [Tooltip("Daño que aplica este ataque")]
    [SerializeField] private int damage = 10;

    [Tooltip("Fuerza de knockback aplicada al enemigo")]
    [SerializeField] private float knockbackForce = 5f;

    [Tooltip("Dirección del knockback en espacio local (se voltea con el sprite)")]
    [SerializeField] private Vector2 knockbackDirection = new Vector2(1f, 0.5f);

    [Tooltip("¿Este ataque puede provocar un estado de aturdimiento (stun)?")]
    [SerializeField] private bool causesStun = false;

    [Tooltip("Duración del stun en segundos (si causesStun está activo)")]
    [SerializeField] private float stunDuration = 0.3f;

    // ──────────────────────────────────────────────
    //  Eventos
    // ──────────────────────────────────────────────
    /// <summary>Se dispara cuando la hitbox impacta con una hurtbox válida.</summary>
    public event Action<HurtBox, HitData> OnHit;

    // ──────────────────────────────────────────────
    //  Estado interno
    // ──────────────────────────────────────────────
    private Collider2D col;
    private bool isActive;

    // Evita golpear al mismo objetivo más de una vez por activación
    private readonly System.Collections.Generic.HashSet<HurtBox> hitThisActivation
        = new System.Collections.Generic.HashSet<HurtBox>();

    // ──────────────────────────────────────────────
    //  Propiedades
    // ──────────────────────────────────────────────
    public int Damage => damage;
    public float KnockbackForce => knockbackForce;

    // ──────────────────────────────────────────────
    //  Unity
    // ──────────────────────────────────────────────
    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // Empieza desactivada
        Deactivate();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;

        HurtBox hurtBox = other.GetComponent<HurtBox>();
        if (hurtBox == null) return;

        // Evitar golpear al mismo objetivo dos veces en la misma activación
        if (hitThisActivation.Contains(hurtBox)) return;
        hitThisActivation.Add(hurtBox);

        // Construir los datos del golpe
        Vector2 worldKnockback = new Vector2(
            knockbackDirection.x * transform.lossyScale.x, // respeta el flip del sprite
            knockbackDirection.y
        ).normalized * knockbackForce;

        HitData data = new HitData
        {
            damage = damage,
            knockback = worldKnockback,
            causesStun = causesStun,
            stunDuration = stunDuration,
            hitPoint = transform.position
        };

        // Notificar al controlador de la hitbox (que disparará el hit stop)
        OnHit?.Invoke(hurtBox, data);

        // Notificar a la hurtbox (que aplicará el daño al enemigo)
        hurtBox.ReceiveHit(data);
    }

    // ──────────────────────────────────────────────
    //  API pública (usada por HitBoxController)
    // ──────────────────────────────────────────────
    public void Activate()
    {
        hitThisActivation.Clear();
        isActive = true;
        col.enabled = true;
    }

    public void Deactivate()
    {
        isActive = false;
        col.enabled = false;
    }
}

/// <summary>Datos que viajan desde la HitBox hasta la HurtBox del enemigo.</summary>
[System.Serializable]
public struct HitData
{
    public int damage;
    public Vector2 knockback;
    public bool causesStun;
    public float stunDuration;
    public Vector2 hitPoint;      // posición del impacto (útil para VFX/sonido)
}