using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class Player : MonoBehaviour, IDamageable
{
    #region Inspector
    [Header("Referencias")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Camera playerCamera;

    [Header("Movimiento (m/s)")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float crouchSpeed = 1.8f;
    [SerializeField] private float acceleration = 40f;

    [Header("Estamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrain = 25f;
    [SerializeField] private float staminaRegen = 18f;
    [SerializeField] private float regenDelay = 1.2f;
    [SerializeField] private float recoverThreshold = 20f;  // Mínimo para volver a correr tras agotarse

    [Header("Agacharse")]
    [SerializeField] private float standHeight = 1.8f;
    [SerializeField] private float crouchHeight = 1.0f;
    [SerializeField] private float standEyeHeight = 1.6f;
    [SerializeField] private float crouchEyeHeight = 0.8f;
    [SerializeField] private float crouchTransitionSpeed = 8f;
    [SerializeField] private bool holdToCrouch = false;     // false = toggle
    [SerializeField] private LayerMask ceilingMask = ~0;

    [Header("Cámara")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float pitchLimit = 85f;

    [Header("Interacción")]
    [SerializeField] private float interactDistance = 2.5f;
    [SerializeField] private LayerMask interactMask = ~0;

    [Header("Vida")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private UnityEvent onDeath;            // Conectar aca game over, sonido, etc.
    [SerializeField] private UnityEvent<int> onDamaged;     // Recibe el daño (sonido de dolor, shake...)

    [Header("UI (todo opcional)")]
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageDuration = 2f;
    [SerializeField] private Slider staminaBar;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Image damageOverlay;           // Imagen roja a pantalla completa
    #endregion

    #region Estado público
    public bool IsDead { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool IsSprinting { get; private set; }

    public int Health
    {
        get => health;
        set
        {
            health = Mathf.Clamp(value, 0, maxHealth);
            if (healthBar != null) healthBar.value = (float)health / maxHealth;
        }
    }
    #endregion
    private Rigidbody rb;
    private CapsuleCollider capsule;

    private InputAction moveAction, lookAction, sprintAction, crouchAction, interactAction;

    private Vector3 targetVelocity;
    private float pitch;
    private float stamina;
    private bool exhausted;
    private float lastSprintTime = -999f;
    private bool wantsCrouch;
    private int health;
    private IInteractable currentInteractable;
    private string lastPrompt = "";
    private Coroutine messageRoutine;
    private readonly HashSet<string> keys = new HashSet<string>();

    #region Unity
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Los raycasts ignoran al jugador
        int ownLayer = ~(1 << gameObject.layer);
        ceilingMask &= ownLayer;
        interactMask &= ownLayer;

        stamina = maxStamina;
        Health = maxHealth;
        ApplyCapsuleHeight(standHeight);
        CreateInputActions();
    }

    private void OnEnable()
    {
        moveAction.Enable(); lookAction.Enable(); sprintAction.Enable();
        crouchAction.Enable(); interactAction.Enable();
        crouchAction.performed += OnCrouchPressed;
        interactAction.performed += OnInteractPressed;
    }

    private void OnDisable()
    {
        crouchAction.performed -= OnCrouchPressed;
        interactAction.performed -= OnInteractPressed;
        moveAction.Disable(); lookAction.Disable(); sprintAction.Disable();
        crouchAction.Disable(); interactAction.Disable();
    }

    private void Start()
    {
        SetCursorLocked(true);
        if (messageText != null) messageText.text = "";
        if (promptText != null) promptText.text = "";
    }

    private void Update()
    {
        HandleCursor();
        FadeDamageOverlay();
        if (IsDead) return;

        HandleLook();
        HandleCrouch();
        HandleMovementInput();
        HandleStamina();
        HandleInteraction();
        UpdateStaminaBar();
    }

    private void FixedUpdate()
    {
        Vector3 v = rb.linearVelocity;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);
        horizontal = Vector3.MoveTowards(horizontal, targetVelocity, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
    }
    #endregion

    #region Input
    private void CreateInputActions()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
        sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");

        crouchAction = new InputAction("Crouch", InputActionType.Button, "<Keyboard>/leftCtrl");
        crouchAction.AddBinding("<Keyboard>/c");

        interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
    }

    private void OnCrouchPressed(InputAction.CallbackContext ctx)
    {
        if (!holdToCrouch) wantsCrouch = !wantsCrouch;
    }

    private void OnInteractPressed(InputAction.CallbackContext ctx)
    {
        if (!IsDead && currentInteractable != null) currentInteractable.Interact(this);
    }
    #endregion

    #region Cámara
    private void HandleCursor()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
        if (!IsDead && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) SetCursorLocked(true);
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 look = lookAction.ReadValue<Vector2>() * mouseSensitivity;
        transform.Rotate(Vector3.up * look.x);                              
        pitch = Mathf.Clamp(pitch - look.y, -pitchLimit, pitchLimit);       
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
    #endregion

    #region Movimiento, estamina y agacharse
    private void HandleMovementInput()
    {
        Vector2 move = moveAction.ReadValue<Vector2>();
        bool hasInput = move.sqrMagnitude > 0.01f;

        IsSprinting = sprintAction.IsPressed() && hasInput && move.y > 0.1f
                      && !IsCrouching && !exhausted && stamina > 0f;

        float speed = IsCrouching ? crouchSpeed : (IsSprinting ? runSpeed : walkSpeed);
        Vector3 dir = transform.right * move.x + transform.forward * move.y;
        targetVelocity = Vector3.ClampMagnitude(dir, 1f) * speed;
    }

    private void HandleStamina()
    {
        if (IsSprinting)
        {
            lastSprintTime = Time.time;
            stamina -= staminaDrain * Time.deltaTime;
            if (stamina <= 0f)
            {
                stamina = 0f;
                exhausted = true; // disparar el sonido de jadeo
            }
        }
        else if (Time.time - lastSprintTime >= regenDelay && stamina < maxStamina)
        {
            stamina = Mathf.Min(stamina + staminaRegen * Time.deltaTime, maxStamina);
            if (exhausted && stamina >= recoverThreshold) exhausted = false;
        }
    }

    private void UpdateStaminaBar()
    {
        if (staminaBar == null) return;
        staminaBar.value = stamina / maxStamina;
        bool show = stamina < maxStamina;
        if (staminaBar.gameObject.activeSelf != show) staminaBar.gameObject.SetActive(show);
    }

    private void HandleCrouch()
    {
        if (holdToCrouch) wantsCrouch = crouchAction.IsPressed();

        if (wantsCrouch) IsCrouching = true;
        else if (IsCrouching && CanStandUp()) IsCrouching = false;

        float t = crouchTransitionSpeed * Time.deltaTime;
        ApplyCapsuleHeight(Mathf.Lerp(capsule.height, IsCrouching ? crouchHeight : standHeight, t));

        Vector3 pos = cameraRoot.localPosition;
        pos.y = Mathf.Lerp(pos.y, IsCrouching ? crouchEyeHeight : standEyeHeight, t);
        cameraRoot.localPosition = pos;
    }

    private void ApplyCapsuleHeight(float height)
    {
        capsule.height = height;
        capsule.center = new Vector3(0f, height * 0.5f, 0f); // Los pies quedan en el piso
    }

    private bool CanStandUp()
    {
        float r = capsule.radius * 0.95f;
        Vector3 bottom = transform.position + Vector3.up * (capsule.radius + 0.05f);
        Vector3 top = transform.position + Vector3.up * (standHeight - capsule.radius);
        return !Physics.CheckCapsule(bottom, top, r, ceilingMask, QueryTriggerInteraction.Ignore);
    }
    #endregion

    #region Interacción (Raycast) e inventario
    private void HandleInteraction()
    {
        currentInteractable = null;
        Transform cam = playerCamera.transform;

        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit,
                            interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            currentInteractable = hit.collider.GetComponentInParent<IInteractable>();
        }

        string prompt = currentInteractable != null ? currentInteractable.InteractionPrompt : "";
        if (prompt != lastPrompt)
        {
            lastPrompt = prompt;
            if (promptText != null) promptText.text = prompt == "" ? "" : $"[E] {prompt}";
        }
    }

    public bool HasKey(string keyId) => keys.Contains(keyId);
    public void AddKey(string keyId) => keys.Add(keyId);
    public void RemoveKey(string keyId) => keys.Remove(keyId);

    public void ShowMessage(string message)
    {
        if (messageText == null) return;
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(message));
    }

    private IEnumerator MessageRoutine(string message)
    {
        messageText.text = message;
        yield return new WaitForSeconds(messageDuration);
        messageText.text = "";
    }
    #endregion

    #region Vida
    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        Health -= amount;
        onDamaged?.Invoke(amount);

        if (damageOverlay != null)
        {
            Color c = damageOverlay.color;
            c.a = 0.6f;
            damageOverlay.color = c;
        }

        if (Health <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        Health += amount;
    }

    private void Die()
    {
        IsDead = true;
        IsSprinting = false;
        targetVelocity = Vector3.zero;
        if (promptText != null) promptText.text = "";
        ShowMessage("Has muerto");
        onDeath?.Invoke();
    }

    private void FadeDamageOverlay()
    {
        if (damageOverlay == null || IsDead) return;
        Color c = damageOverlay.color;
        c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * 1.5f);
        damageOverlay.color = c;
    }
    #endregion
}
