using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject botPrefab;
    public Transform botContainerUI;

    [Header("Game State")]
    public double estimatedHypePerSecond = 5.0;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        CalculateOfflineProgress();
        SpawnSavedBots();
    }

    public void AddHype(int amount)
    {
        SaveManager.Instance.currentData.totalHype += amount;
        if (UIManager.Instance != null) UIManager.Instance.UpdateHypeDisplay(SaveManager.Instance.currentData.totalHype);
    }

    public void TryBuyNewBot(string keywordToTrack)
    {
        if (EconomyManager.Instance == null || SaveManager.Instance == null) return;
        if (SaveManager.Instance.currentData.activeBots == null) SaveManager.Instance.currentData.activeBots = new System.Collections.Generic.List<BotSaveData>();

        int currentBotCount = SaveManager.Instance.currentData.activeBots.Count;

        // FIXED: Capitalized EconomyManager.Instance
        if (EconomyManager.Instance.BuyInToKeyword(keywordToTrack, currentBotCount))
        {
            SpawnSingleBot(keywordToTrack, 1);
            SaveManager.Instance.currentData.activeBots.Add(new BotSaveData { keyword = keywordToTrack, level = 1 });
            SaveManager.Instance.SaveGame();

            if (UIManager.Instance != null) UIManager.Instance.UpdateHypeDisplay(SaveManager.Instance.currentData.totalHype);
        }
    }

    private void SpawnSavedBots()
    {
        foreach (BotSaveData botData in SaveManager.Instance.currentData.activeBots)
        {
            SpawnSingleBot(botData.keyword, botData.level);
        }
    }

    private void SpawnSingleBot(string botKeyword, int botLevel)
    {
        GameObject newBotObj = Instantiate(botPrefab, botContainerUI);
        KeywordCrawler crawler = newBotObj.GetComponent<KeywordCrawler>();
        if (crawler != null)
        {
            crawler.keyword = botKeyword;
            crawler.botLevel = botLevel;
        }
    }

    private void CalculateOfflineProgress()
    {
        string savedTimeStr = SaveManager.Instance.currentData.lastExitTime;
        if (string.IsNullOrEmpty(savedTimeStr)) return;

        long temp = Convert.ToInt64(savedTimeStr);
        DateTime oldTime = DateTime.FromBinary(temp);
        TimeSpan difference = DateTime.Now.Subtract(oldTime);

        double offlineSeconds = difference.TotalSeconds;
        if (offlineSeconds > 86400) { offlineSeconds = 86400; }

        double offlineHypeEarned = offlineSeconds * estimatedHypePerSecond;
        if (offlineHypeEarned > 1)
        {
            AddHype(Mathf.CeilToInt((float)offlineHypeEarned));
            if (UIManager.Instance != null) UIManager.Instance.ShowNotification($"Welcome back! Earned {offlineHypeEarned:F0} MB offline.");
        }
    }
}