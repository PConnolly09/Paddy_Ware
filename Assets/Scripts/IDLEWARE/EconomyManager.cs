using System.Collections.Generic;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    // CAPITALIZED!
    public static EconomyManager Instance { get; private set; }
    public int currentMonitors = 2;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool BuyInToKeyword(string newKeyword, int currentActiveBots)
    {
        if (currentActiveBots >= currentMonitors)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowNotification("<color=red>Not enough monitors!</color>");
            return false;
        }

        double cost = 100 * Mathf.Pow(1.5f, currentActiveBots);

        if (SaveManager.Instance.currentData.totalHype >= cost)
        {
            SaveManager.Instance.currentData.totalHype -= cost;
            return true;
        }

        if (UIManager.Instance != null) UIManager.Instance.ShowNotification("<color=red>Not enough Data to deploy bot!</color>");
        return false;
    }

    public void LiquidateKeyword(string keyword, double totalDataMinedByBot, int peakUpvotes)
    {
        // MATH FIX: 1 Cred per 50 Data Mined (was 500)
        int credsEarned = Mathf.FloorToInt((float)totalDataMinedByBot / 50f);

        if (peakUpvotes > 10000) credsEarned += 10;
        else if (peakUpvotes > 2000) credsEarned += 5;
        else if (peakUpvotes > 500) credsEarned += 2;

        if (credsEarned < 1) credsEarned = 1;

        SaveManager.Instance.currentData.creds += credsEarned;
        SaveManager.Instance.SaveGame();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCredsDisplay(SaveManager.Instance.currentData.creds);
            UIManager.Instance.ShowNotification($"<color=cyan>LIQUIDATED {keyword.ToUpper()}</color>. Earned {credsEarned} Creds.");
        }

        if (GhostTracker.Instance != null)
        {
            GhostTracker.Instance.StartGhostTracking(keyword, peakUpvotes);
        }
    }
}