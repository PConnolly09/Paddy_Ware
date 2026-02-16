using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct FrameData
{
    public Vector2 position;
    public bool isInteracting; // True if 'E' was pressed
    public int actionID;       // 0=None, 1=Chop, 2=Water

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
    public int strength = 100; // Affects Chop Speed / Damage
    public int agility = 100;  // Affects Movement Speed
    public int focus = 100;    // Affects Crafting Speed / Night Defense

    // Logic to halve stats for the next generation
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
    public StatSet stats;
    public List<FrameData> recording = new List<FrameData>();
}