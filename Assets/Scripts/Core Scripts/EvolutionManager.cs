using UnityEngine;
using System.Collections.Generic;

public class EvolutionManager : MonoBehaviour
{
    public static EvolutionManager Instance;

    [Header("Available Classes")]
    public ArchetypeData neutralArchetype;
    public List<ArchetypeData> possibleArchetypes;

    void Awake()
    {
        Instance = this;
    }

    public ArchetypeData DetermineArchetype(List<FrameData> recording)
    {
        if (recording.Count == 0) return neutralArchetype;

        // 1. Tally Actions
        Dictionary<int, int> actionCounts = new Dictionary<int, int>();

        foreach (var frame in recording)
        {
            if (frame.actionID > 0)
            {
                if (!actionCounts.ContainsKey(frame.actionID)) actionCounts[frame.actionID] = 0;
                actionCounts[frame.actionID]++;
            }
        }

        // 2. Find Most Frequent Action
        int highestCount = 0;
        int dominantActionID = 0;

        foreach (var pair in actionCounts)
        {
            if (pair.Value > highestCount)
            {
                highestCount = pair.Value;
                dominantActionID = pair.Key;
            }
        }

        // 3. Match to Archetype
        // Requirement: You must have done the action at least 5 times to qualify
        if (highestCount < 5) return neutralArchetype;

        foreach (var arch in possibleArchetypes)
        {
            if (arch.requiredActionID == dominantActionID)
            {
                Debug.Log($"EVOLUTION: Player performed Action {dominantActionID} {highestCount} times. Evolving into {arch.className}.");
                return arch;
            }
        }

        return neutralArchetype;
    }
}