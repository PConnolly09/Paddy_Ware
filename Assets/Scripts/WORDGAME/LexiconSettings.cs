using UnityEngine;

[CreateAssetMenu(fileName = "LexiconSettings", menuName = "Lexicon/Game Settings")]
public class LexiconSettingsSO : ScriptableObject
{
    [Header("Encounter Scaling")]
    public int maxLevel = 50;
    public double baseFirewallHP = 500000000; // 500M
    public float hpScaleMultiplier = 1.5f;

    [Header("Resource Economy")]
    public int startingQueries = 5;
    public int startingDiscards = 2;
    public int startingRerolls = 2;

    [Header("Scoring Engine: Word Length")]
    [Tooltip("Multiplier applied based on the length of the word (Index 0 = 0 letters, Index 3 = 3 letters, etc.)")]
    public float[] lengthMultipliers = new float[] { 0f, 0f, 0.5f, 1.0f, 2.0f, 3.5f, 5.0f, 8.0f };

    [Header("Scoring Engine: Rarity Multiplier")]
    public float rareWordBonus = 5.0f;     // For words under 1M hits
    public float uncommonWordBonus = 2.5f; // For words under 10M hits
    public float commonWordBonus = 1.0f;   // Normal words
    public float mainstreamPenalty = 0.5f; // For words over 100M hits (shallow playstyle)
}