using System;
using UnityEngine;

/// <summary>
/// Singleton que gestiona toda la puntuación de la partida.
///
/// FÓRMULA:
///   Golpe      → (daño × 10 + 50 si fuerte) × multiplicadorCombo
///   Enemigo    → 200 × multiplicadorCombo
///   Tiempo     → max(0, 3000 − segundosTranscurridos × 10)  [al acabar nivel]
///   Muerte     → −300 puntos
///   Daño recibido → −daño × 2 puntos
///
/// MULTIPLICADOR DE COMBO:
///   Sube +0.5× cada 3 golpes consecutivos. Máx 4×.
///   Se reinicia cuando PlayerCombat cancela el combo.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Puntos base")]
    [SerializeField] private int pointsPerDamageUnit = 10;
    [SerializeField] private int strongHitBonus = 50;
    [SerializeField] private int pointsPerEnemyKill = 200;

    [Header("Penalizaciones")]
    [SerializeField] private int deathPenalty = 300;
    [SerializeField] private int damageReceivedPenalty = 2;

    [Header("Bonus de tiempo")]
    [SerializeField] private int timeBonusBase = 3000;
    [SerializeField] private int timeBonusDecayPerSec = 10;

    [Header("Multiplicador de combo")]
    [SerializeField] private int hitsPerMultiplierStep = 3;
    [SerializeField] private float multiplierStep = 0.5f;
    [SerializeField] private float maxMultiplier = 4f;

    public int Score { get; private set; }
    public float ComboMultiplier { get; private set; } = 1f;
    public int ComboHits { get; private set; }
    public int EnemiesDefeated { get; private set; }
    public int Deaths { get; private set; }
    public float LevelTime => Time.time - _levelStartTime;

    public event Action<int> OnScoreChanged;
    public event Action<float> OnMultiplierChanged;
    public event Action<int> OnComboHitsChanged;
    public event Action OnComboReset;

    private float _levelStartTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _levelStartTime = Time.time;
    }

    private void Start()
    {
        SubscribeToPlayer();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void SubscribeToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        HealthComponent hp = player.GetComponent<HealthComponent>();
        if (hp != null)
        {
            hp.OnDamage += RegisterDamageReceived;
            hp.OnDeath += RegisterPlayerDeath;
        }

        PlayerCombat combat = player.GetComponent<PlayerCombat>();
        if (combat != null)
            combat.OnComboExpired += ResetCombo;
    }

    public void RegisterHit(float damage, bool isStrong)
    {
        ComboHits++;
        UpdateMultiplier();

        int points = Mathf.RoundToInt(
            (damage * pointsPerDamageUnit + (isStrong ? strongHitBonus : 0))
            * ComboMultiplier);

        AddScore(points);
        OnComboHitsChanged?.Invoke(ComboHits);
    }

    public void RegisterEnemyKilled()
    {
        EnemiesDefeated++;
        int points = Mathf.RoundToInt(pointsPerEnemyKill * ComboMultiplier);
        AddScore(points);
    }

    public int ApplyTimeBonus()
    {
        int bonus = Mathf.Max(0, timeBonusBase - Mathf.RoundToInt(LevelTime) * timeBonusDecayPerSec);
        AddScore(bonus);
        return bonus;
    }

    private void RegisterDamageReceived(float damage)
    {
        int penalty = Mathf.RoundToInt(damage * damageReceivedPenalty);
        SubtractScore(penalty);
    }

    private void RegisterPlayerDeath()
    {
        Deaths++;
        SubtractScore(deathPenalty);
        ResetCombo();
    }

    private void ResetCombo()
    {
        ComboHits = 0;
        ComboMultiplier = 1f;
        OnMultiplierChanged?.Invoke(ComboMultiplier);
        OnComboHitsChanged?.Invoke(ComboHits);
        OnComboReset?.Invoke();
    }

    private void UpdateMultiplier()
    {
        float newMult = 1f + Mathf.Floor((float)ComboHits / hitsPerMultiplierStep) * multiplierStep;
        newMult = Mathf.Min(newMult, maxMultiplier);

        if (!Mathf.Approximately(newMult, ComboMultiplier))
        {
            ComboMultiplier = newMult;
            OnMultiplierChanged?.Invoke(ComboMultiplier);
        }
    }

    private void AddScore(int points)
    {
        Score += points;
        OnScoreChanged?.Invoke(Score);
    }

    private void SubtractScore(int points)
    {
        Score = Mathf.Max(0, Score - points);
        OnScoreChanged?.Invoke(Score);
    }
}