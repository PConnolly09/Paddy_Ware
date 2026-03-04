using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Top Bar")]
    public TextMeshProUGUI hypeText;
    public TextMeshProUGUI credsText;

    [Header("News Ticker")]
    public TextMeshProUGUI headlineText;

    [Header("Trending Feed")]
    public TextMeshProUGUI trendingText;

    [Header("Buy Bot UI")]
    public TMP_InputField newBotInputField;

    [Header("Notifications & Tracker Log")]
    public TextMeshProUGUI notificationText; // NEW: Drag a UI Text here!
    private List<string> _logMessages = new List<string>();

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
            string newKeyword = newBotInputField.text.Trim();
            GameManager.Instance.TryBuyNewBot(newKeyword);
            newBotInputField.text = "";
        }
    }

    // --- NEW: GHOST TRACKER LOG ---
    public void ShowNotification(string message)
    {
        if (notificationText != null)
        {
            _logMessages.Add(message);
            // Keep only the last 4 messages so it doesn't run off the screen
            if (_logMessages.Count > 4) _logMessages.RemoveAt(0);

            notificationText.text = string.Join("\n\n", _logMessages);
        }
    }

    public void UpdateHypeDisplay(double currentHype) { if (hypeText != null) hypeText.text = $"Data: {currentHype:F0} MB"; }
    public void UpdateCredsDisplay(int currentCreds) { if (credsText != null) credsText.text = $"Creds: {currentCreds}"; }
    public void ShowHeadline(string keyword, string headline) { if (headlineText != null) headlineText.text = $"[<color=#FFD700>{keyword.ToUpper()}</color>] {headline}"; }
    public void ClearHeadline() { if (headlineText != null) headlineText.text = "<color=#888888>Awaiting Net Intel...</color>"; }

    public void UpdateTrendingDisplay(List<string> keywords)
    {
        if (trendingText != null)
        {
            trendingText.text = "<b>TRENDING NOW:</b>\n";
            // Removed unicode emoji!
            foreach (string word in keywords) trendingText.text += $"[HOT] {word}\n";
        }
    }
}