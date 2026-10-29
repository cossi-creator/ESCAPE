using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Referencias a Scripts")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInteraction playerInteraction;

    [Header("Elementos de UI")]
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageDuration = 2f;
    [SerializeField] private Slider staminaBar;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Image damageOverlay;

    private Coroutine messageRoutine;

    private void OnEnable()
    {
        playerHealth.OnHealthChanged += UpdateHealthBar;
        playerHealth.OnDamaged += ShowDamageOverlay;
        playerHealth.OnDied += HandleDeath;

        playerMovement.OnStaminaChanged += UpdateStaminaBar;

        playerInteraction.OnPromptChanged += UpdatePrompt;
        playerInteraction.OnMessageShow += ShowMessage;
    }

    private void OnDisable()
    {
        playerHealth.OnHealthChanged -= UpdateHealthBar;
        playerHealth.OnDamaged -= ShowDamageOverlay;
        playerHealth.OnDied -= HandleDeath;

        playerMovement.OnStaminaChanged -= UpdateStaminaBar;

        playerInteraction.OnPromptChanged -= UpdatePrompt;
        playerInteraction.OnMessageShow -= ShowMessage;
    }

    private void Start()
    {
        if (messageText != null) messageText.text = "";
        if (promptText != null) promptText.text = "";
    }

    private void Update()
    {
        FadeDamageOverlay();
    }

    private void UpdateHealthBar(int current, int max)
    {
        if (healthBar != null) healthBar.value = (float)current / max;
    }

    private void ShowDamageOverlay(int damageAmount)
    {
        if (damageOverlay != null)
        {
            Color c = damageOverlay.color;
            c.a = 0.6f;
            damageOverlay.color = c;
        }
    }

    private void FadeDamageOverlay()
    {
        if (damageOverlay == null || playerHealth.IsDead) return;
        Color c = damageOverlay.color;
        c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * 1.5f);
        damageOverlay.color = c;
    }

    private void HandleDeath() => ShowMessage("Has muerto");

    private void UpdateStaminaBar(float current, float max)
    {
        if (staminaBar == null) return;
        staminaBar.value = current / max;
        bool show = current < max;
        if (staminaBar.gameObject.activeSelf != show) staminaBar.gameObject.SetActive(show);
    }

    private void UpdatePrompt(string prompt)
    {
        if (promptText != null) promptText.text = prompt;
    }

    private void ShowMessage(string message)
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
}
