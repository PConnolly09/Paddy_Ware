using UnityEngine;

[CreateAssetMenu(fileName = "LexiconSettings", menuName = "Lexicon/Game Settings")]
public class LexiconSettingsSO : ScriptableObject
{
    [Header("Encounter Scaling")]
    public int maxLevel = 50;
    public double baseFirewallHP = 3500;
    public float hpScaleMultiplier = 1.35f;

    [Header("Boss Multipliers")]
    public float standardBossHpMultiplier = 2.5f;
    public float titanBossHpMultiplier = 4.0f;

    [Header("Resource Economy")]
    public int startingQueries = 5;
    public int startingDiscards = 2;
    public int startingRerolls = 2;

    [Header("Scoring: Wiki Power Scaling")]
    [Tooltip("The fractional power applied to Wiki Hits. 0.25 (Fourth Root) scales 10k hits to 10x, and 1 Billion hits to 177x.")]
    public float hitPowerScaling = 0.25f;

    [Header("Scoring: Datamuse Rarity Multipliers")]
    public float anomalyMultiplier = 2.5f;   // Freq == 0
    public float ultraRareMultiplier = 1.8f; // Freq <= 1.0
    public float rareMultiplier = 1.4f;      // Freq <= 10.0
    public float commonMultiplier = 1.0f;    // Freq > 10.0

    [Header("Scoring: Obscurity Combo")]
    [Tooltip("If Datamuse Frequency is below this, the Obscurity Combo triggers.")]
    public float obscurityFrequencyThreshold = 2.0f;
    public float baseObscurityStackValue = 0.25f;
    public float obscurityStreakBonus = 0.1f;

    [Header("Scoring: Word Length")]
    public float[] lengthMultipliers = new float[] { 0f, 0f, 0.5f, 1.0f, 2.0f, 3.5f, 5.0f, 8.0f };
}