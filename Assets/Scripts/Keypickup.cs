using UnityEngine;

/// <summary>Llave que se puede agarrar. Necesita un Collider.</summary>
public class KeyPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private string keyId = "llave_1";           // Solo importa si una puerta pide esta llave en particular
    [SerializeField] private string displayName = "Llave oxidada";

    public string InteractionPrompt => $"Agarrar {displayName}";

    public void Interact(Player player)
    {
        player.AddKey(keyId);

        string progress = "";
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollectKey(); // Actualiza la UI y dispara la alarma con la última
            progress = $" ({GameManager.Instance.KeysCollected}/{GameManager.Instance.TotalKeys})";
        }

        player.ShowMessage($"Agarraste: {displayName}{progress}");
        Destroy(gameObject);
    }
}