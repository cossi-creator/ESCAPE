using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class AmbientZoneTrigger : MonoBehaviour
{
    public AudioClip zoneAmbientClip;
    public AmbientAudioManager audioManager;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        audioManager.ChangeAmbient(zoneAmbientClip);
    }
}
