using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;
    public bool IsDead { get; private set; }

    // Eventos del Patrón Observer
    public event Action<int, int> OnHealthChanged;
    public event Action<int> OnDamaged;
    public event Action OnDied;

    public int CurrentHealth
    {
        get => currentHealth;
        set
        {
            currentHealth = Mathf.Clamp(value, 0, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    private void Start() => CurrentHealth = maxHealth;

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        CurrentHealth -= amount;
        OnDamaged?.Invoke(amount);

        if (CurrentHealth <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        CurrentHealth += amount;
    }

    private void Die()
    {
        IsDead = true;
        OnDied?.Invoke();
    }
}