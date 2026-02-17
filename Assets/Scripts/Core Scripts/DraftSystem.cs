using UnityEngine;

[System.Serializable]
public class DraftConfig
{
    public StatSet startingStats;
    public ArchetypeData startingArchetype;
    public bool isFreshStart;

    public DraftConfig(StatSet stats, ArchetypeData arche, bool fresh)
    {
        startingStats = stats;
        startingArchetype = arche;
        isFreshStart = fresh;
    }
}

public class DraftSystem : MonoBehaviour
{
    public static DraftSystem Instance;

    // The configuration for the upcoming day
    public DraftConfig nextDayDraft;

    [Header("Settings")]
    public float baseSacrificeReward = 25f;

    void Awake()
    {
        Instance = this;
    }

    // Called automatically at the end of a normal day
    public void PrepareNormalDraft(StatSet currentStats, ArchetypeData currentArch)
    {
        // Normal behavior: Stats decay, Archetype persists/evolves
        nextDayDraft = new DraftConfig(currentStats.GetDecayedCopy(), currentArch, false);
    }

    // Called when you choose to sacrifice a specific clone
    public void PrepareSacrificeDraft(CloneData target)
    {
        // Rebirth behavior: 
        // 1. You get the clone's EXACT stats (no decay)
        StatSet preservedStats = target.stats.Clone();

        // 2. Your Archetype is wiped to null (Blank Slate)
        // 3. Flag as fresh start
        nextDayDraft = new DraftConfig(preservedStats, null, true);

        // 4. Grant Entropy Reward
        float reward = baseSacrificeReward;
        TimelineManager.Instance.AddEntropy(reward);
        Debug.Log($"SACRIFICE: Gained {reward} Entropy. Next day will be a Fresh Start.");
    }

    public void ClearDraft()
    {
        nextDayDraft = null;
    }
}