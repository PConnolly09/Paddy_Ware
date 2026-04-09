using UnityEngine;

public abstract class RelicSO : ScriptableObject
{
    [Header("Relic Identity")]
    public string relicID;
    public string relicName;
    [TextArea] public string description;
    public int baseCost = 250;

    [Header("Unlock Requirements")]
    public bool isUnlockedByDefault = true;
    public string unlockID = ""; // e.g., "UNLOCK_NOVELIST"
    [TextArea] public string unlockConditionText = ""; // e.g., "Spell a 7-letter word."

    // --- THE EVENT HOOKS ---
    public virtual void OnEquip() { }
    public virtual void OnPreMath(RunManager.ScoreBreakdown bd) { }
    public virtual void OnPostMath(RunManager.ScoreBreakdown bd) { }
    public virtual void OnDiscard(DieData discardedDie) { }
    public virtual float ModifyShopPrice(float currentPrice) { return currentPrice; }
}