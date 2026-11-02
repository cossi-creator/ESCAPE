using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el objeto que tiene el script PlayerMovement")]
    public PlayerMovement playerMovement;
    public Rigidbody playerRb;

    [Header("Audios")]
    public AudioClip[] footstepClips;

    [Header("Configuración de Cadencia (Segundos entre pasos)")]
    public float walkStepInterval = 0.5f;
    public float sprintStepInterval = 0.3f;
    public float crouchStepInterval = 0.8f;

    [Header("Volumen y Tono")]
    public float baseVolume = 1f;
    public float crouchVolumeMultiplier = 0.4f; 
    [Range(0.8f, 1.2f)] public float minPitch = 0.9f;
    [Range(0.8f, 1.2f)] public float maxPitch = 1.1f;

    private AudioSource audioSource;
    private float stepTimer = 0f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerRb == null) playerRb = GetComponentInParent<Rigidbody>();
    }

    void Update()
    {
        if (playerMovement == null || playerRb == null) return;
        if (playerMovement.ControlLocked) return;

        Vector3 horizontalVelocity = new Vector3(playerRb.linearVelocity.x, 0, playerRb.linearVelocity.z);
        bool isMoving = horizontalVelocity.sqrMagnitude > 0.1f;

        if (isMoving)
        {
            stepTimer += Time.deltaTime;

            float currentInterval = walkStepInterval;
            if (playerMovement.IsSprinting)
                currentInterval = sprintStepInterval;
            else if (playerMovement.IsCrouching)
                currentInterval = crouchStepInterval;

            if (stepTimer >= currentInterval)
            {
                PlayFootstep();
                stepTimer = 0f; 
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    private void PlayFootstep()
    {
        if (footstepClips.Length == 0) return;

        int randomIndex = Random.Range(0, footstepClips.Length);
        AudioClip clipToPlay = footstepClips[randomIndex];

        float currentVolume = baseVolume;
        if (playerMovement.IsCrouching)
        {
            currentVolume *= crouchVolumeMultiplier;
        }

        audioSource.pitch = Random.Range(minPitch, maxPitch);

        audioSource.PlayOneShot(clipToPlay, currentVolume);
    }
}