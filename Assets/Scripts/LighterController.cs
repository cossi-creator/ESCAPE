using UnityEngine;
using UnityEngine.InputSystem;

public class LighterController : MonoBehaviour
{
    [Header("Referencia")]
    [SerializeField] private Camera playerCamera;

    [Header("Prefab visual")]
    [SerializeField] private GameObject lighterPrefab;

    [Header("Posición")]
    [SerializeField]
    private Vector3 localPosition =
        new Vector3(0.35f, -0.35f, 0.7f);

    [SerializeField]
    private Vector3 localRotation =
        new Vector3(15f, -20f, 0f);

    [Header("Luz")]
    [SerializeField] private float lightIntensity = 3f;
    [SerializeField] private float lightRange = 8f;

    private bool hasLighter;
    private GameObject lighterInstance;
    private Light lighterLight;

    private void Start()
    {
        CreateLighter();
    }

    private void Update()
    {
        if (!hasLighter)
            return;

        if (Keyboard.current.fKey.isPressed)
        {
            ShowLighter();
        }
        else
        {
            HideLighter();
        }
    }

    private void CreateLighter()
    {
        if (lighterPrefab == null || playerCamera == null)
            return;

        lighterInstance = Instantiate(
            lighterPrefab,
            playerCamera.transform);

        lighterInstance.transform.localPosition = localPosition;

        lighterInstance.transform.localEulerAngles = localRotation;

        lighterInstance.SetActive(false);

        lighterLight = lighterInstance.GetComponentInChildren<Light>();

        if (lighterLight != null)
        {
            lighterLight.intensity = lightIntensity;
            lighterLight.range = lightRange;
            lighterLight.enabled = false;
        }
    }

    private void ShowLighter()
    {
        if (!lighterInstance.activeSelf)
        {
            lighterInstance.SetActive(true);

            if (lighterLight != null)
                lighterLight.enabled = true;
        }
    }

    private void HideLighter()
    {
        if (lighterInstance.activeSelf)
        {
            lighterInstance.SetActive(false);

            if (lighterLight != null)
                lighterLight.enabled = false;
        }
    }

    public void UnlockLighter()
    {
        hasLighter = true;
    }
}