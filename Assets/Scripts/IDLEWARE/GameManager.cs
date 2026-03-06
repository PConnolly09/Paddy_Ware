using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Global Clock")]
    public float globalCycleTime = 15f;
    private float _currentTimer;

    public delegate void CycleTickAction();
    public event CycleTickAction OnGlobalCycleTick;

    [Header("Prefabs")]
    public GameObject botPrefab;
    public Transform botContainerUI;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        _currentTimer = globalCycleTime;
        SpawnSavedBots();
    }

    private void Update()
    {
        _currentTimer -= Time.deltaTime;

        if (UIManager.Instance != null) UIManager.Instance.UpdateGlobalTimer(_currentTimer);

        if (_currentTimer <= 0)
        {
            _currentTimer = globalCycleTime;
            if (OnGlobalCycleTick != null) OnGlobalCycleTick.Invoke();
        }
    }

    public void AddHype(double amount)
    {
        SaveManager.Instance.currentData.totalHype += amount;
        SaveManager.Instance.currentData.lifetimeHypeMined += amount;
        if (UIManager.Instance != null) UIManager.Instance.UpdateHypeDisplay(SaveManager.Instance.currentData.totalHype);
    }

    public void TryBuyNewBot(string keywordToTrack)
    {
        int credCost = 25;
        if (EconomyManager.Instance != null)
        {
            credCost = EconomyManager.Instance.CalculateKeywordCost(keywordToTrack, 0);
        }

        TryDeployBot(keywordToTrack, credCost, "popular");
    }

    public void TryDeployBot(string keyword, int credCost, string targetSubreddit = "popular")
    {
        if (EconomyManager.Instance.SpendCreds(credCost))
        {
            // Simplified new expression!
            BotSaveData newBotData = new()
            {
                keyword = keyword,
                buyInCostCreds = credCost,
                totalHypeMined = 0,
                targetSubreddit = targetSubreddit,
                hypeHistory = new() // NEW: Creates the empty graph history
            };
            SaveManager.Instance.currentData.activeBots.Add(newBotData);

            // Pass the whole data object directly to the bot
            SpawnSingleBot(newBotData);
            SaveManager.Instance.SaveGame();
        }
    }

    private void SpawnSavedBots()
    {
        foreach (BotSaveData botData in SaveManager.Instance.currentData.activeBots)
        {
            SpawnSingleBot(botData);
        }
    }

    private void SpawnSingleBot(BotSaveData botData)
    {
        GameObject newBotObj = Instantiate(botPrefab, botContainerUI);
        KeywordCrawler crawler = newBotObj.GetComponent<KeywordCrawler>();
        if (crawler != null)
        {
            crawler.InitializeBot(botData);
        }
    }
}