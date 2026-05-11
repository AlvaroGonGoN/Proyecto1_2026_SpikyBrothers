using System;
using UnityEngine;

public class HurtBox : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Eventos
    // ──────────────────────────────────────────────
    /// <summary>Se dispara cuando esta hurtbox recibe un golpe válido.</summary>
    public event Action<HitData> OnHitReceived;

    // ──────────────────────────────────────────────
    //  Estado
    // ──────────────────────────────────────────────
    private bool isInvulnerable;
    private Collider2D col;

    // ──────────────────────────────────────────────
    //  Unity
    // ──────────────────────────────────────────────
    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    // ──────────────────────────────────────────────
    //  API pública
    // ──────────────────────────────────────────────

    /// <summary>Llamado por HitBox cuando impacta. Aplica el golpe si no hay invulnerabilidad.</summary>
    public void ReceiveHit(HitData data)
    {
        if (isInvulnerable) return;

        OnHitReceived?.Invoke(data);
    }

    /// <summary>Activa invulnerabilidad temporal (dodge, inicio de combo, i-frames).</summary>
    public void SetInvulnerable(bool value) => isInvulnerable = value;

    /// <summary>Activa/desactiva el collider de la hurtbox (útil para pooling o muerte).</summary>
    public void SetActive(bool value) => col.enabled = value;
}

