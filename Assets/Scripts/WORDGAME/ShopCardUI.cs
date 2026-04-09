using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopCardUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI priceText;
    public Button buyButton;

    private ShopItem myItemData;

    // Called by ShopManager when the shop is generated
    public void SetupShopItem(ShopItem item)
    {
        myItemData = item;

        if (titleText != null) titleText.text = item.ItemName;
        if (descriptionText != null) descriptionText.text = item.Description;
        if (priceText != null) priceText.text = $"{item.Cost} CR";

        if (buyButton != null) buyButton.interactable = true;
    }

    // LINK THIS TO THE BUTTON COMPONENT ON YOUR SHOP ITEM PREFAB
    public void OnBuyClicked()
    {
        if (myItemData == null) return;

        // 1. Check if the player has enough credits
        if (RunManager.Instance.currentCredits >= myItemData.Cost)
        {
            // 2. Take the money
            RunManager.Instance.currentCredits -= myItemData.Cost;

            // 3. Give them the item/relic/heal!
            myItemData.OnPurchase?.Invoke();

            // 4. Update the card to show it's sold
            if (priceText != null) priceText.text = "SOLD OUT";
            if (buyButton != null) buyButton.interactable = false;

            // 5. Update the main HUD so the wallet flashes the new value
            if (WordUIManager.Instance != null) WordUIManager.Instance.ForceUpdateHUD();
        }
        else
        {
            // Not enough money!
            if (WordUIManager.Instance != null)
                WordUIManager.Instance.ShowTransientMessage("<color=#FF0000>INSUFFICIENT CREDITS</color>");
        }
    }
}