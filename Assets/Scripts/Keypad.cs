using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Teclado numérico. Al ingresar el código correcto resuelve un puzzle en el GameManager,
/// y cualquier Door con lockType = Puzzle y el mismo ID se puede abrir.
/// Se interactúa con E, se escribe el código y Enter confirma. Esc cierra.
/// </summary>
public class Keypad : MonoBehaviour, IInteractable
{
    [SerializeField] private string correctCode = "1234";
    [SerializeField] private string puzzleId = "codigo_sotano";   // Mismo ID que la puerta

    [Header("UI (puede compartirse entre varios teclados)")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_InputField inputField;           // Content Type: Integer Number
    [SerializeField] private TMP_Text feedbackText;

    private Player currentPlayer;
    private bool solved;

    public string InteractionPrompt => solved ? "Teclado (código aceptado)" : "Usar teclado";

    public void Interact(Player player)
    {
        if (solved)
        {
            player.ShowMessage("Ya ingresaste el código.");
            return;
        }

        currentPlayer = player;
        player.LockControl();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        panel.SetActive(true);
        feedbackText.text = "";
        inputField.text = "";
        inputField.characterLimit = correctCode.Length;
        inputField.onSubmit.AddListener(Submit);
        inputField.ActivateInputField();
    }

    private void Update()
    {
        if (currentPlayer != null && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    private void Submit(string code)
    {
        if (code == correctCode)
        {
            solved = true;
            if (GameManager.Instance != null) GameManager.Instance.SolvePuzzle(puzzleId);
            currentPlayer.ShowMessage("Código correcto.");
            Close();
        }
        else
        {
            feedbackText.text = "Código incorrecto";
            inputField.text = "";
            inputField.ActivateInputField();
        }
    }

    private void Close()
    {
        inputField.onSubmit.RemoveListener(Submit);
        panel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentPlayer.UnlockControl();
        currentPlayer = null;
    }
}