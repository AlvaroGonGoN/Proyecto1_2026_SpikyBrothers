// HitBoxController.cs
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla qué hitbox está activa en cada momento.
/// Recibe órdenes de PlayerCombat (para activación por código) o
/// directamente de Animation Events (para activación por frame exacto).
///
/// También es el punto donde se dispara el Hit Stop al detectar impacto.
/// </summary>
public class HitBoxController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Inspector — asignar cada hitbox manualmente
    // ──────────────────────────────────────────────
    [Header("Hitboxes de tierra")]
    [SerializeField] private HitBox hitBox_Attack1;
    [SerializeField] private HitBox hitBox_Attack2;
    [SerializeField] private HitBox hitBox_Attack3Strong;

    [Header("Hitboxes de aire")]
    [SerializeField] private HitBox hitBox_AirAttack1;
    [SerializeField] private HitBox hitBox_AirAttack2Strong;
    [SerializeField] private HitBox hitBox_AirAttackStrong;

    // ──────────────────────────────────────────────
    //  Referencias
    // ──────────────────────────────────────────────
    private PlayerCombat combat;
    private HitBox activeHitBox;

    // ──────────────────────────────────────────────
    //  Unity
    // ──────────────────────────────────────────────
    private void Awake()
    {
        combat = GetComponent<PlayerCombat>();
    }

    private void OnEnable()
    {
        SubscribeAll();
    }

    private void OnDisable()
    {
        UnsubscribeAll();
        DeactivateAll();
    }

    // ──────────────────────────────────────────────
    //  Suscripción a eventos de cada hitbox
    // ──────────────────────────────────────────────
    private void SubscribeAll()
    {
        Subscribe(hitBox_Attack1);
        Subscribe(hitBox_Attack2);
        Subscribe(hitBox_Attack3Strong);
        Subscribe(hitBox_AirAttack1);
        Subscribe(hitBox_AirAttack2Strong);
        Subscribe(hitBox_AirAttackStrong);
    }

    private void UnsubscribeAll()
    {
        Unsubscribe(hitBox_Attack1);
        Unsubscribe(hitBox_Attack2);
        Unsubscribe(hitBox_Attack3Strong);
        Unsubscribe(hitBox_AirAttack1);
        Unsubscribe(hitBox_AirAttack2Strong);
        Unsubscribe(hitBox_AirAttackStrong);
    }

    private void Subscribe(HitBox hb)
    {
        if (hb != null) hb.OnHit += HandleHit;
    }

    private void Unsubscribe(HitBox hb)
    {
        if (hb != null) hb.OnHit -= HandleHit;
    }

    // ──────────────────────────────────────────────
    //  Callback de impacto → dispara Hit Stop
    // ──────────────────────────────────────────────
    private void HandleHit(HurtBox hurtBox, HitData data)
    {
        // Determinar si el golpe activo es fuerte para elegir la duración del hit stop
        bool isStrong = activeHitBox == hitBox_Attack3Strong
                     || activeHitBox == hitBox_AirAttack2Strong
                     || activeHitBox == hitBox_AirAttackStrong;

        // Disparar hit stop en PlayerCombat (el hit stop real ocurre al impactar, no al lanzar)
        combat.TriggerHitStop(isStrong ? 1 : 0);
        ScoreManager.Instance?.RegisterHit(data.damage, isStrong);

        // Aquí puedes añadir VFX/SFX usando data.hitPoint
        // Ejemplo: Instantiate(hitParticle, data.hitPoint, Quaternion.identity);
    }

    // ──────────────────────────────────────────────
    //  API pública — para Animation Events y PlayerCombat
    // ──────────────────────────────────────────────

    /// <summary>
    /// Activa una hitbox por nombre. Llamado desde Animation Events.
    /// Los nombres válidos son: Attack1, Attack2, Attack3Strong,
    /// AirAttack1, AirAttack2Strong, AirAttackStrong.
    /// </summary>
    public void ActivateHitBox(string attackName)
    {
        DeactivateAll();

        HitBox target = GetHitBoxByName(attackName);
        if (target == null)
        {
            Debug.LogWarning($"[HitBoxController] No se encontró la hitbox: '{attackName}'");
            return;
        }

        activeHitBox = target;
        target.Activate();
    }

    /// <summary>Desactiva todas las hitboxes. Llamado desde Animation Events al terminar el frame de impacto.</summary>
    public void DeactivateAll()
    {
        activeHitBox = null;

        hitBox_Attack1?.Deactivate();
        hitBox_Attack2?.Deactivate();
        hitBox_Attack3Strong?.Deactivate();
        hitBox_AirAttack1?.Deactivate();
        hitBox_AirAttack2Strong?.Deactivate();
        hitBox_AirAttackStrong?.Deactivate();
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────
    private HitBox GetHitBoxByName(string name) => name switch
    {
        "Attack1" => hitBox_Attack1,
        "Attack2" => hitBox_Attack2,
        "Attack3Strong" => hitBox_Attack3Strong,
        "AirAttack1" => hitBox_AirAttack1,
        "AirAttack2Strong" => hitBox_AirAttack2Strong,
        "AirAttackStrong" => hitBox_AirAttackStrong,
        _ => null
    };
}