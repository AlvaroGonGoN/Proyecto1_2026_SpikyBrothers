using UnityEngine;
using System;
using System.Collections;

public class HealthComponent : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public event System.Action<float, float> OnHealthChanged;  // (current, max)
    public float currentHealth;

    [Header("Invulnerability")]
    public float invulnerabilityTime = 0.2f;

    public bool IsDead { get; private set; }
    public bool IsAlive => currentHealth > 0f;

    public event Action OnDeath;
    public event Action<float> OnDamage; // devuelve daño recibido

    bool _isInvulnerable;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || _isInvulnerable) return;

        currentHealth -= damage;
        OnDamage?.Invoke(damage);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvulnerabilityRoutine());
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    void Die()
    {
        if (IsDead) return;
        IsDead = true;
        OnDeath?.Invoke();
    }

    IEnumerator InvulnerabilityRoutine()
    {
        _isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityTime);
        _isInvulnerable = false;
    }
}

