using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum ShopType { PreRun, MidRun }

public class ShopItem
{
    public string ItemName;
    public string Description;
    public int Cost;
    public Action OnPurchase;
    public bool IsSoldOut;
}

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    public ShopType currentShopType;
    public readonly List<ShopItem> currentShopItems = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void GeneratePreRunShop()
    {
        currentShopType = ShopType.PreRun;
        currentShopItems.Clear();

        currentShopItems.Add(new ShopItem
        {
            ItemName = "Query Overclock",
            Description = "Permanently gain +1 Query to your Max Queries every run.",
            Cost = 2500,
            OnPurchase = () => { LexiconSaveManager.Instance.currentData.bonusStartingQueries++; }
        });

        currentShopItems.Add(new ShopItem
        {
            ItemName = "Loaded Dice",
            Description = "Permanently start every run with an extra D20 in your pool.",
            Cost = 5000,
            OnPurchase = () => { LexiconSaveManager.Instance.currentData.bonusStartingD20s++; }
        });

        WordUIManager.Instance.OpenShopUI("BLACK MARKET (Permanent Upgrades)", LexiconSaveManager.Instance.currentData.dataCores, "Data Cores", currentShopItems);
    }

    public void GenerateMidRunShop()
    {
        currentShopType = ShopType.MidRun;
        currentShopItems.Clear();

        // 1. Always offer a heal
        currentShopItems.Add(new ShopItem
        {
            ItemName = "Heal Node",
            Description = "Instantly restore +2 Queries for the upcoming Mainframe.",
            Cost = 300,
            OnPurchase = () => { RunManager.Instance.queriesRemaining += 2; WordUIManager.Instance.ForceUpdateHUD(); }
        });

        // 2. Offer 2 Random Relics the player doesn't have yet
        List<Relic> availableRelics = RelicLibrary.Instance.AllRelics.Keys
            .Where(r => !RunManager.Instance.activeRelics.Contains(r)).ToList();

        // Shuffle
        for (int i = 0; i < availableRelics.Count; i++)
        {
            Relic temp = availableRelics[i];
            int r = UnityEngine.Random.Range(i, availableRelics.Count);
            availableRelics[i] = availableRelics[r];
            availableRelics[r] = temp;
        }

        for (int i = 0; i < 2 && i < availableRelics.Count; i++)
        {
            Relic relicToSell = availableRelics[i];
            RelicData data = RelicLibrary.Instance.AllRelics[relicToSell];

            currentShopItems.Add(new ShopItem
            {
                ItemName = $"[RELIC] {data.Name}",
                Description = data.Description,
                Cost = data.Cost,
                OnPurchase = () => { RunManager.Instance.activeRelics.Add(relicToSell); }
            });
        }

        WordUIManager.Instance.OpenShopUI("TERMINAL NODE (Mid-Run Shop)", RunManager.Instance.currentCredits, "Credits", currentShopItems);
    }

    public void TryBuyItem(ShopItem item, GameObject buttonUI)
    {
        if (item.IsSoldOut) return;

        bool success = false;
        if (currentShopType == ShopType.MidRun)
        {
            if (RunManager.Instance.currentCredits >= item.Cost)
            {
                RunManager.Instance.currentCredits -= item.Cost;
                success = true;
            }
        }
        else
        {
            if (LexiconSaveManager.Instance.currentData.dataCores >= item.Cost)
            {
                LexiconSaveManager.Instance.currentData.dataCores -= item.Cost;
                LexiconSaveManager.Instance.SaveGame();
                success = true;
            }
        }

        if (success)
        {
            item.OnPurchase?.Invoke();
            item.IsSoldOut = true;
            WordUIManager.Instance.MarkShopItemSold(buttonUI);
            int newBalance = currentShopType == ShopType.MidRun ? RunManager.Instance.currentCredits : LexiconSaveManager.Instance.currentData.dataCores;
            WordUIManager.Instance.UpdateShopBalance(newBalance);
        }
        else
        {
            WordUIManager.Instance.LogError("Insufficient funds for this transaction.");
        }
    }
}