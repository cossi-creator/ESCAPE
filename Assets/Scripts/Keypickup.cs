using UnityEngine;
public class KeyPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private string keyId = "llave"; 
    [SerializeField] private string displayName = "Llave oxidada";

    public string InteractionPrompt => $"Agarrar {displayName}";

    public void Interact(Player player)
    {
        player.AddKey(keyId);
        player.ShowMessage($"Agarraste: {displayName}");
        Destroy(gameObject);
    }
}