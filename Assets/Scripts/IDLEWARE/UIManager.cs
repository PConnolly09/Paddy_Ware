using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Top Bar")]
    public TextMeshProUGUI hypeText;
    public TextMeshProUGUI credsText;

    [Header("Global Clock")]
    public TextMeshProUGUI globalTimerText;

    [Header("News Ticker")]
    public TextMeshProUGUI headlineText;

    [Header("Buy Bot UI")]
    public TMP_InputField newBotInputField;

    [Header("Notifications & Tracker Log")]
    public TextMeshProUGUI notificationText;

    // Simplified new expression!
    private readonly List<string> _logMessages = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.currentData != null)
        {
            UpdateHypeDisplay(SaveManager.Instance.currentData.totalHype);
            UpdateCredsDisplay(SaveManager.Instance.currentData.creds);
        }
    }

    public void OnBuyBotButtonClicked()
    {
        if (newBotInputField != null && !string.IsNullOrEmpty(newBotInputField.text))
        {
            // Fallback for manual typing: Send it directly to GameManager
            string newKeyword = newBotInputField.text.Trim();
            GameManager.Instance.TryBuyNewBot(newKeyword);
            newBotInputField.text = "";
        }
    }

    public void ShowNotification(string message)
    {
        if (notificationText != null)
        {
            _logMessages.Add(message);
            if (_logMessages.Count > 4) _logMessages.RemoveAt(0);

            // Replaced unicode with standard characters
            notificationText.text = string.Join("\n\n", _logMessages);
        }
    }

    public void UpdateGlobalTimer(float timeRemaining)
    {
        if (globalTimerText != null)
        {
            globalTimerText.text = $"Cycle in: {Mathf.Max(0, timeRemaining):F1}s";
        }
    }

    public void UpdateHypeDisplay(double currentHype)
    {
        if (hypeText != null) hypeText.text = $"Data: {currentHype:F0} MB";
    }

    public void UpdateCredsDisplay(int currentCreds)
    {
        if (credsText != null) credsText.text = $"Creds: {currentCreds}";
    }

    public void ShowHeadline(string keyword, string headline)
    {
        if (headlineText != null) headlineText.text = $"[<color=#FFD700>{keyword.ToUpper()}</color>] {headline}";
    }

    public void ClearHeadline()
    {
        if (headlineText != null) headlineText.text = "<color=#888888>Awaiting Net Intel...</color>";
    }
}