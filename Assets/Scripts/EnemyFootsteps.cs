using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AudioSource))]
public class EnemyFootsteps : MonoBehaviour
{
    public NavMeshAgent agent;
    public AudioClip[] footstepClips;
    public float stepInterval = 0.5f;

    private AudioSource audioSource;
    private float stepTimer;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (agent == null)
            agent = GetComponentInParent<NavMeshAgent>();

        stepTimer = stepInterval;
    }

    void Update()
    {
        if (agent == null) return;

        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            stepTimer += Time.deltaTime;

            if (stepTimer >= stepInterval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = stepInterval;
        }
    }

    private void PlayFootstep()
    {
        if (footstepClips.Length == 0) return;

        audioSource.pitch = Random.Range(0.95f, 1.05f);

        audioSource.PlayOneShot(
            footstepClips[Random.Range(0, footstepClips.Length)],
            Random.Range(0.7f, 1f)
        );
    }
}