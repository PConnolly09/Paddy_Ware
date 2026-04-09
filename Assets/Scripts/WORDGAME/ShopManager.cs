using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ShopItem
{
    public string ItemName;
    public string Description;
    public int Cost;
    public Sprite itemIcon;
    public System.Action OnPurchase;
}

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop Settings")]
    public List<RelicSO> relicPool;

    private void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    // ==========================================
    // PRE-RUN SHOP (Called by GameDirector)
    // ==========================================
    public void GeneratePreRunShop()
    {
        List<ShopItem> preRunItems = new List<ShopItem>();

        preRunItems.Add(new ShopItem
        {
            ItemName = "Firewall Patch",
            Description = "Heal 25% of your Max HP.",
            Cost = 150,
            itemIcon = null,
            OnPurchase = () => {
                RunManager.Instance.currentDamageDealt = Mathf.Max(0, (float)(RunManager.Instance.currentDamageDealt - (RunManager.Instance.targetFirewallHP * 0.25f)));
                WordUIManager.Instance.ForceUpdateHUD();
            }
        });

        PopulateDice(preRunItems, 1, false);
        PopulateRelics(preRunItems, 2, false);

        WordUIManager.Instance.OpenShopUI("PRE-RUN BLACK MARKET", RunManager.Instance.currentCredits, "CR", preRunItems);
    }

    // ==========================================
    // COMBINED MARKET EXTRACTOR (Called by RunManager)
    // ==========================================
    public List<ShopItem> GetMarketItems(bool everythingIsFree)
    {
        List<ShopItem> marketItems = new List<ShopItem>();

        marketItems.Add(new ShopItem
        {
            ItemName = "Reboot Cycle",
            Description = "Restore +2 Queries for the upcoming Mainframe.",
            Cost = everythingIsFree ? 0 : 300,
            itemIcon = null,
            OnPurchase = () => { RunManager.Instance.queriesRemaining += 2; WordUIManager.Instance.ForceUpdateHUD(); }
        });

        PopulateDice(marketItems, 1, everythingIsFree);
        PopulateRelics(marketItems, 1, everythingIsFree);

        return marketItems;
    }

    // ==========================================
    // POOL POPULATION LOGIC
    // ==========================================
    private void PopulateDice(List<ShopItem> targetList, int count, bool isFree)
    {
        if (RunManager.Instance.activeCharacter != null && RunManager.Instance.activeCharacter.baseDiceSet != null)
        {
            var dicePool = RunManager.Instance.activeCharacter.baseDiceSet.diceInSet;
            if (dicePool.Count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    LexiconDieSO randomDieBlueprint = dicePool[UnityEngine.Random.Range(0, dicePool.Count)];
                    targetList.Add(new ShopItem
                    {
                        ItemName = randomDieBlueprint.dieName,
                        Description = "Add a completely new die to your draw bag.",
                        Cost = isFree ? 0 : 450,
                        itemIcon = randomDieBlueprint.dieSprite,
                        OnPurchase = () => {
                            DiceDeck.Instance.AddNewDie(randomDieBlueprint);
                            WordUIManager.Instance.ForceUpdateHUD();
                        }
                    });
                }
            }
        }
    }

    private void PopulateRelics(List<ShopItem> targetList, int count, bool isFree)
    {
        // FIX: Removed the LexiconSaveManager.IsUnlocked check so you can actually test them!
        var availableRelics = relicPool
            .Where(r => !RunManager.Instance.activeRelics.Contains(r))
            .OrderBy(x => UnityEngine.Random.value)
            .Take(count)
            .ToList();

        foreach (var relic in availableRelics)
        {
            targetList.Add(new ShopItem
            {
                ItemName = relic.relicName,
                Description = relic.description,
                Cost = isFree ? 0 : relic.baseCost,
                itemIcon = null,
                OnPurchase = () => {
                    RunManager.Instance.activeRelics.Add(relic);
                    relic.OnEquip();
                    WordUIManager.Instance.ForceUpdateHUD();
                }
            });
        }
    }
}