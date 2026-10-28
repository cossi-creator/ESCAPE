using UnityEngine;
public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] private bool isLocked = false;
    [SerializeField] private string requiredKeyId = "llave";
    [SerializeField] private bool consumeKey = false;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float speed = 120f; // Grados por segundo

    private bool isOpen;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    public string InteractionPrompt
    {
        get
        {
            if (isLocked) return "Puerta cerrada con llave";
            return isOpen ? "Cerrar puerta" : "Abrir puerta";
        }
    }

    private void Awake()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    private void Update()
    {
        Quaternion target = isOpen ? openRotation : closedRotation;
        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, target, speed * Time.deltaTime);
    }

    public void Interact(Player player)
    {
        if (isLocked)
        {
            if (!player.HasKey(requiredKeyId))
            {
                player.ShowMessage("Está cerrada con llave.");
                return;
            }

            isLocked = false;
            if (consumeKey) player.RemoveKey(requiredKeyId);
            player.ShowMessage("Destrabaste la puerta.");
        }

        isOpen = !isOpen;
    }
}
