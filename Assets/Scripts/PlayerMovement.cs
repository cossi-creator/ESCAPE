using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerHealth))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform cameraRoot;

    [Header("Movimiento (m/s)")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float crouchSpeed = 1.8f;
    [SerializeField] private float acceleration = 10f;

    [Header("Estamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrain = 10f;
    [SerializeField] private float staminaRegen = 20f;
    [SerializeField] private float regenDelay = 1.2f;
    [SerializeField] private float recoverThreshold = 20f;

    [Header("Agacharse")]
    [SerializeField] private float standHeight = 4f;
    [SerializeField] private float crouchHeight = 3f;
    [SerializeField] private float standEyeHeight = 4f;
    [SerializeField] private float crouchEyeHeight = 2.7f;
    [SerializeField] private float crouchTransitionSpeed = 8f;
    [SerializeField] private bool holdToCrouch = false;
    [SerializeField] private LayerMask ceilingMask = ~0;

    public bool IsCrouching { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool ControlLocked { get; private set; }

    public event Action<float, float> OnStaminaChanged;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private PlayerHealth health;
    private InputAction moveAction, sprintAction, crouchAction;

    private Vector3 targetVelocity;
    private float stamina;
    private bool exhausted;
    private float lastSprintTime = -999f;
    private bool wantsCrouch;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        health = GetComponent<PlayerHealth>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        ceilingMask &= ~(1 << gameObject.layer);
        stamina = maxStamina;
        ApplyCapsuleHeight(standHeight);
        CreateInputs();
    }

    private void OnEnable()
    {
        moveAction.Enable(); sprintAction.Enable(); crouchAction.Enable();
        crouchAction.performed += OnCrouchPressed;
    }

    private void OnDisable()
    {
        moveAction.Disable(); sprintAction.Disable(); crouchAction.Disable();
        crouchAction.performed -= OnCrouchPressed;
    }
    private void Start()
    {
        OnStaminaChanged?.Invoke(stamina, maxStamina);
    }
    private void CreateInputs()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
        crouchAction = new InputAction("Crouch", InputActionType.Button, "<Keyboard>/leftCtrl");
        crouchAction.AddBinding("<Keyboard>/c");
    }

    private void OnCrouchPressed(InputAction.CallbackContext ctx)
    {
        if (!holdToCrouch && !ControlLocked) wantsCrouch = !wantsCrouch;
    }

    private void Update()
    {
        if (health.IsDead || ControlLocked) return;

        HandleCrouch();
        HandleMovementInput();
        HandleStamina();
    }

    private void FixedUpdate()
    {
        Vector3 v = rb.linearVelocity;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);
        horizontal = Vector3.MoveTowards(horizontal, targetVelocity, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
    }

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
        float previousStamina = stamina;

        if (IsSprinting)
        {
            lastSprintTime = Time.time;
            stamina -= staminaDrain * Time.deltaTime;
            if (stamina <= 0f)
            {
                stamina = 0f;
                exhausted = true;
            }
        }
        else if (Time.time - lastSprintTime >= regenDelay && stamina < maxStamina)
        {
            stamina = Mathf.Min(stamina + staminaRegen * Time.deltaTime, maxStamina);
            if (exhausted && stamina >= recoverThreshold) exhausted = false;
        }

        if (Mathf.Abs(stamina - previousStamina) > 0.01f)
            OnStaminaChanged?.Invoke(stamina, maxStamina);
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
        capsule.center = new Vector3(0f, height * 0.5f, 0f);
    }

    private bool CanStandUp()
    {
        float r = capsule.radius * 0.95f;
        Vector3 bottom = transform.position + Vector3.up * (capsule.radius + 0.05f);
        Vector3 top = transform.position + Vector3.up * (standHeight - capsule.radius);
        return !Physics.CheckCapsule(bottom, top, r, ceilingMask, QueryTriggerInteraction.Ignore);
    }

    public void LockControl()
    {
        ControlLocked = true;
        IsSprinting = false;
        targetVelocity = Vector3.zero;
    }

    public void UnlockControl()
    {
        if (health.IsDead || Time.timeScale == 0f) return;
        ControlLocked = false;
    }
}