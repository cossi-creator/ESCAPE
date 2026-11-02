using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Objetivo")]
    [SerializeField] private int totalKeys = 3;
    [SerializeField] private Player player;

    [Header("UI")]
    [SerializeField] private TMP_Text keysText;         
    [SerializeField] private TMP_Text objectiveText;     
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private float gameOverDelay = 1.5f; 

    [Header("Alarma (todo opcional)")]
    [SerializeField] private AudioSource alarmSound;    
    [SerializeField] private Image alarmFlash;           
    [SerializeField] private Light[] alarmLights;        

  
    public int KeysCollected { get; private set; }
    public int TotalKeys => totalKeys;
    public bool HasAllKeys => KeysCollected >= totalKeys;
    public bool AlarmActive { get; private set; }

    
    public event Action<int, int> KeysChanged;          
    public event Action AlarmTriggered;
    public event Action<string, int> PuzzleSolved;      
    public event Action GameOver;
    public event Action Victory;

    private readonly HashSet<string> solvedPuzzles = new HashSet<string>();
    private bool ended;
    private float[] lightBaseIntensity;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Time.timeScale = 1f; 

        if (player == null) player = FindFirstObjectByType<Player>(); 

        KeysChanged += UpdateKeysText; 

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (alarmFlash != null) alarmFlash.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (player != null) player.Died += HandlePlayerDied;

        if (alarmLights != null)
        {
            lightBaseIntensity = new float[alarmLights.Length];
            for (int i = 0; i < alarmLights.Length; i++) lightBaseIntensity[i] = alarmLights[i].intensity;
        }

        UpdateKeysText(KeysCollected, totalKeys);
        if (objectiveText != null) objectiveText.text = $"Encontrá las {totalKeys} llaves";
    }

    private void OnDestroy()
    {
        if (player != null) player.Died -= HandlePlayerDied;
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!AlarmActive || ended) return;

        float pulse = Mathf.PingPong(Time.time * 2f, 1f);
        if (alarmFlash != null)
        {
            Color c = alarmFlash.color;
            c.a = pulse * 0.25f;
            alarmFlash.color = c;
        }
        for (int i = 0; i < alarmLights.Length; i++)
            alarmLights[i].intensity = Mathf.Lerp(lightBaseIntensity[i] * 0.2f, lightBaseIntensity[i] * 1.2f, pulse);
    }

   
    public void CollectKey()
    {
        if (ended) return;

        KeysCollected = Mathf.Min(KeysCollected + 1, totalKeys);
        KeysChanged?.Invoke(KeysCollected, totalKeys);

        if (HasAllKeys && !AlarmActive) TriggerAlarm(); 
    }

   
    public void SolvePuzzle(string puzzleId)
    {
        if (solvedPuzzles.Add(puzzleId)) PuzzleSolved?.Invoke(puzzleId, solvedPuzzles.Count);
    }

    public bool IsPuzzleSolved(string puzzleId) => solvedPuzzles.Contains(puzzleId);

  
    private void TriggerAlarm()
    {
        AlarmActive = true;

        if (objectiveText != null) objectiveText.text = "¡ALARMA! Escapá por la puerta principal";
        if (alarmSound != null) alarmSound.Play();
        if (alarmFlash != null) alarmFlash.gameObject.SetActive(true);
        foreach (Light l in alarmLights) l.color = Color.red;

        AlarmTriggered?.Invoke(); 
    }


    private void HandlePlayerDied() => EndGame(false, gameOverDelay);

    public void Win() => EndGame(true, 0f);
    public void Lose() => EndGame(false, 0f);

    private void EndGame(bool won, float delay)
    {
        if (ended) return;
        ended = true;
        AlarmActive = false;

        if (player != null) player.LockControl();
        if (alarmSound != null) alarmSound.Stop();
        if (alarmFlash != null) alarmFlash.gameObject.SetActive(false);

        if (won) Victory?.Invoke(); else GameOver?.Invoke();
        StartCoroutine(ShowEndScreen(won, delay));
    }

    private IEnumerator ShowEndScreen(bool won, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GameObject panel = won ? victoryPanel : gameOverPanel;
        if (panel != null) panel.SetActive(true);
    }

  
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }

    public void MainMenu()
    {
        RestartLevel();
    }
    private void UpdateKeysText(int collected, int total)
    {
        if (keysText != null) keysText.text = $"Llaves: {collected}/{total}";
    }
    
}