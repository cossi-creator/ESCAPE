using System;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth), typeof(PlayerMovement), typeof(PlayerInteraction))]
[RequireComponent(typeof(PlayerCamera))]
public class Player : MonoBehaviour, IDamageable
{
    private PlayerHealth health;
    private PlayerMovement movement;
    private PlayerInteraction interaction;

    public event Action Died; 

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        movement = GetComponent<PlayerMovement>();
        interaction = GetComponent<PlayerInteraction>();

        health.OnDied += () => Died?.Invoke();
    }


    public int Health
    {
        get => health.CurrentHealth;
        set => health.CurrentHealth = value;
    }
    public void TakeDamage(int amount) => health.TakeDamage(amount);
    public void Heal(int amount) => health.Heal(amount);
 
    public bool IsDead => health.IsDead;
    public bool IsCrouching => movement.IsCrouching;
    public bool IsSprinting => movement.IsSprinting;

  
    public void LockControl() => movement.LockControl();
    public void UnlockControl() => movement.UnlockControl();


    public bool HasKey(string keyId) => interaction.HasKey(keyId);
    public void AddKey(string keyId) => interaction.AddKey(keyId);
    public void RemoveKey(string keyId) => interaction.RemoveKey(keyId);
    public void ShowMessage(string message) => interaction.ShowMessage(message);
}