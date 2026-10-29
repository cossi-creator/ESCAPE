using System;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth), typeof(PlayerMovement), typeof(PlayerInteraction))]
[RequireComponent(typeof(PlayerCamera))]
public class Player : MonoBehaviour, IDamageable
{
    private PlayerHealth health;
    private PlayerMovement movement;
    private PlayerInteraction interaction;

    public event Action Died; // GameManager se suscribe acá

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        movement = GetComponent<PlayerMovement>();
        interaction = GetComponent<PlayerInteraction>();

        // Enlaza el evento interno de muerte con el evento público que espera el GameManager
        health.OnDied += () => Died?.Invoke();
    }

    // --- IDamageable ---
    public int Health
    {
        get => health.CurrentHealth;
        set => health.CurrentHealth = value;
    }
    public void TakeDamage(int amount) => health.TakeDamage(amount);
    public void Heal(int amount) => health.Heal(amount);
    // --- Exposición de estado para Enemy.cs ---
    public bool IsDead => health.IsDead;
    public bool IsCrouching => movement.IsCrouching;
    public bool IsSprinting => movement.IsSprinting;

    // --- Control (usado por GameManager y Keypad.cs) ---
    public void LockControl() => movement.LockControl();
    public void UnlockControl() => movement.UnlockControl();

    // --- Inventario (usado por Door.cs y Keypickup.cs) ---
    public bool HasKey(string keyId) => interaction.HasKey(keyId);
    public void AddKey(string keyId) => interaction.AddKey(keyId);
    public void RemoveKey(string keyId) => interaction.RemoveKey(keyId);
    public void ShowMessage(string message) => interaction.ShowMessage(message);
}