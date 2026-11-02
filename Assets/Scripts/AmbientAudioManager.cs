using UnityEngine;

public class AmbientAudioManager : MonoBehaviour
{
    public AudioSource audioSource;

    public void ChangeAmbient(AudioClip clip)
    {
        if (audioSource.clip == clip)
            return;

        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }
}
