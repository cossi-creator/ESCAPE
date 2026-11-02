using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TriggerSound : MonoBehaviour
{
    public AudioSource audioSource;

    [Tooltip("Si es true solo sonará una vez")]
    public bool playOnlyOnce = true;

    private bool hasPlayed = false;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (playOnlyOnce && hasPlayed)
            return;

        audioSource.Play();

        hasPlayed = true;
    }
}
