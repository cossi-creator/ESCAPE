using UnityEngine;

public class EscapeZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>();

        if (player != null)
        {
            GameManager.Instance.Win();
        }
    }
}