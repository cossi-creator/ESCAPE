using UnityEngine;

public class LighterPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private string displayName = "Encendedor";

    public string InteractionPrompt => $"Agarrar {displayName}";

    public void Interact(Player player)
    {
        LighterController lighter = player.GetComponent<LighterController>();

        if (lighter != null)
        {
            lighter.UnlockLighter();
        }

        player.ShowMessage($"Agarraste: {displayName}");

        Destroy(gameObject);
    }
}