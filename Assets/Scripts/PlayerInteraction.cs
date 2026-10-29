using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerHealth), typeof(PlayerMovement))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interacción")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 2.5f;
    [SerializeField] private LayerMask interactMask = ~0;

    public event Action<string> OnPromptChanged;
    public event Action<string> OnMessageShow;

    private PlayerHealth health;
    private PlayerMovement movement;
    private InputAction interactAction;

    private IInteractable currentInteractable;
    private string lastPrompt = "";
    private readonly HashSet<string> keys = new HashSet<string>();

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        movement = GetComponent<PlayerMovement>();
        interactMask &= ~(1 << gameObject.layer);

        interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
    }

    private void OnEnable()
    {
        interactAction.Enable();
        interactAction.performed += OnInteractPressed;
    }

    private void OnDisable()
    {
        interactAction.Disable();
        interactAction.performed -= OnInteractPressed;
    }

    private void Update()
    {
        if (health.IsDead || movement.ControlLocked)
        {
            UpdatePrompt("");
            return;
        }
        HandleRaycast();
    }

    private void HandleRaycast()
    {
        currentInteractable = null;
        Transform cam = playerCamera.transform;

        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit,
                            interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            currentInteractable = hit.collider.GetComponentInParent<IInteractable>();
        }

        string prompt = currentInteractable != null ? currentInteractable.InteractionPrompt : "";
        UpdatePrompt(prompt);
    }

    private void UpdatePrompt(string newPrompt)
    {
        if (newPrompt != lastPrompt)
        {
            lastPrompt = newPrompt;
            OnPromptChanged?.Invoke(newPrompt == "" ? "" : $"[E] {newPrompt}");
        }
    }

    private void OnInteractPressed(InputAction.CallbackContext ctx)
    {
        if (!health.IsDead && !movement.ControlLocked && currentInteractable != null)
            currentInteractable.Interact(GetComponent<Player>());
    }

    public bool HasKey(string keyId) => keys.Contains(keyId);
    public void AddKey(string keyId) => keys.Add(keyId);
    public void RemoveKey(string keyId) => keys.Remove(keyId);

    public void ShowMessage(string message) => OnMessageShow?.Invoke(message);
}