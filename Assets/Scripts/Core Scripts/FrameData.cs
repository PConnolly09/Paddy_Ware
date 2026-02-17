using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct FrameData
{
    public Vector2 position;
    public bool isInteracting;
    public int actionID;

    public FrameData(Vector2 pos, bool interact, int action)
    {
        position = pos;
        isInteracting = interact;
        actionID = action;
    }
}

[System.Serializable]
public class StatSet
{
    public int strength = 100;
    public int agility = 100;
    public int focus = 100;

    public StatSet GetDecayedCopy()
    {
        StatSet newStats = new StatSet();
        newStats.strength = Mathf.Max(1, this.strength / 2);
        newStats.agility = Mathf.Max(1, this.agility / 2);
        newStats.focus = Mathf.Max(1, this.focus / 2);
        return newStats;
    }

    public StatSet Clone()
    {
        return new StatSet { strength = strength, agility = agility, focus = focus };
    }
}

[System.Serializable]
public class CloneData
{
    public int originalDayNumber;
    public ArchetypeData archetype; // Stores what the clone WAS on that day
    public StatSet stats;
    public List<FrameData> recording = new List<FrameData>();
}