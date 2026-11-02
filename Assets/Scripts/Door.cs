using UnityEngine;

public enum DoorLockType
{
    None,     
    Key,      
    AllKeys,  
    Puzzle    
}
public class Door : MonoBehaviour, IInteractable
{
    [Header("Bloqueo")]
    [SerializeField] private DoorLockType lockType = DoorLockType.None;
    [SerializeField] private string requiredKeyId = "llave_1";          
    [SerializeField] private bool consumeKey = false;                   
    [SerializeField] private string requiredPuzzleId = "codigo_sotano"; 
    [SerializeField] private string lockedMessage = "Está cerrada.";

    [Header("Puerta de escape")]
    [SerializeField] private bool isExitDoor = false;   
    [SerializeField] private float winDelay = 1.2f;

    [Header("Animación")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float speed = 120f;        

    private bool isLocked;
    private bool isOpen;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    public string InteractionPrompt
    {
        get
        {
            if (isLocked)
            {
                if (lockType == DoorLockType.AllKeys && GameManager.Instance != null)
                    return $"Puerta principal ({GameManager.Instance.KeysCollected}/{GameManager.Instance.TotalKeys} llaves)";
                return "Puerta bloqueada";
            }
            return isOpen ? "Cerrar puerta" : "Abrir puerta";
        }
    }

    private void Awake()
    {
        isLocked = lockType != DoorLockType.None;
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
            if (!ConditionMet(player))
            {
                player.ShowMessage(lockedMessage);
                return;
            }

            isLocked = false;
            if (lockType == DoorLockType.Key && consumeKey) player.RemoveKey(requiredKeyId);
            player.ShowMessage("Destrabaste la puerta.");
        }

        isOpen = !isOpen;

        if (isExitDoor && isOpen) Invoke(nameof(Escape), winDelay);
    }

    private bool ConditionMet(Player player)
    {
        GameManager gm = GameManager.Instance;
        switch (lockType)
        {
            case DoorLockType.Key: return player.HasKey(requiredKeyId);
            case DoorLockType.AllKeys: return gm != null && gm.HasAllKeys;
            case DoorLockType.Puzzle: return gm != null && gm.IsPuzzleSolved(requiredPuzzleId);
            default: return true;
        }
    }

    private void Escape()
    {
        if (GameManager.Instance != null) GameManager.Instance.Win();
    }
}