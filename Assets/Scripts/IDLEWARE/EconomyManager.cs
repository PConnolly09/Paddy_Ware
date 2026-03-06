using UnityEngine;
using System.Collections.Generic;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    // List of expensive, high-value words
    private List<string> powerWords = new List<string> { "apple", "tesla", "gaming", "crypto", "ai" };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // SCOUTING: Calculates how much a word costs to buy based on text analysis
    public int CalculateKeywordCost(string keyword, int estimatedUpvotes)
    {
        int baseCost = 25; // Minimum cost for a slow/niche word

        // Traffic tax: Highly upvoted current trends cost more
        if (estimatedUpvotes > 5000) baseCost += 50;
        else if (estimatedUpvotes > 1000) baseCost += 20;

        // Power Word Tax: Brands and massive nouns cost a premium
        if (powerWords.Contains(keyword.ToLower()))
        {
            baseCost *= 3;
        }

        return baseCost;
    }

    public bool SpendCreds(int amount)
    {
        if (SaveManager.Instance.currentData.creds >= amount)
        {
            SaveManager.Instance.currentData.creds -= amount;
            if (UIManager.Instance != null) UIManager.Instance.UpdateCredsDisplay(SaveManager.Instance.currentData.creds);
            return true;
        }
        if (UIManager.Instance != null) UIManager.Instance.ShowNotification("[ERROR] Insufficient Creds.");
        return false;
    }

    // LIQUIDATION MATH: Risk vs Reward
    public void LiquidateBot(string keyword, int initialCost, double totalHypeMined, int cyclesInFallingState)
    {
        // Base return: You get a fraction of your initial Creds back, plus bonuses for Hype mined
        float basePayout = (initialCost * 0.5f) + (float)(totalHypeMined / 1000f);

        // DECAY PENALTY: Every cycle spent in "FALLING" removes 10% of your payout value!
        float penaltyMultiplier = 1.0f - (cyclesInFallingState * 0.1f);
        if (penaltyMultiplier < 0.1f) penaltyMultiplier = 0.1f; // Floor at 10% value

        int finalCredsEarned = Mathf.FloorToInt(basePayout * penaltyMultiplier);
        if (finalCredsEarned < 1) finalCredsEarned = 1;

        SaveManager.Instance.currentData.creds += finalCredsEarned;
        SaveManager.Instance.currentData.lifetimeLiquidations++;
        SaveManager.Instance.SaveGame();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCredsDisplay(SaveManager.Instance.currentData.creds);

            string penaltyNotice = cyclesInFallingState > 0 ? $" [PENALTY: -{cyclesInFallingState * 10}%]" : " [PERFECT TIMING]";
            UIManager.Instance.ShowNotification($"[LIQUIDATED] {keyword.ToUpper()} for {finalCredsEarned} Creds." + penaltyNotice);
        }
    }
}