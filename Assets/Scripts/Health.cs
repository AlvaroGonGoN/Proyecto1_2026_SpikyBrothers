// Health.cs  (mínimo funcional, amplía según tu juego)
using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    public event Action OnDeath;
    public event Action<int, int> OnHealthChanged; // (current, max)

    private void Start()
    {
        currentHealth = maxHealth;
        GetComponentInChildren<HurtBox>().OnHitReceived += TakeDamage;
    }

    private void TakeDamage(HitData data)
    {
        currentHealth = Mathf.Max(0, currentHealth - data.damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Knockback
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.AddForce(data.knockback, ForceMode2D.Impulse);

        if (currentHealth == 0)
            OnDeath?.Invoke();
    }
}
