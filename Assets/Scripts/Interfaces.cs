using UnityEngine;
public interface IInteractable
{
    string InteractionPrompt { get; }
    void Interact(Player player);
}

public interface IDamageable
{
    int Health { get; set; }
    void TakeDamage(int amount);
}