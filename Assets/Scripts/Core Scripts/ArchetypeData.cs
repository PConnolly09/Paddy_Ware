using UnityEngine;

[CreateAssetMenu(fileName = "New Archetype", menuName = "TimeLoop/Archetype")]
public class ArchetypeData : ScriptableObject
{
    public string className = "Neutral";
    public Color tint = Color.white;

    [Header("Stat Multipliers")]
    public float strengthMult = 1.0f;
    public float agilityMult = 1.0f;
    public float focusMult = 1.0f;

    [Header("Drift Criteria")]
    // Which action triggers this class?
    // 1=Chop, 2=Mine, 3=Water
    public int requiredActionID;
}