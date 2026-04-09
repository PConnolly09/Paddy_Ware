using UnityEngine;
using System.Collections.Generic;

// ==========================================
// 1. THE DIE BLUEPRINT
// ==========================================
[CreateAssetMenu(fileName = "New Die", menuName = "Lexicon/Die Blueprint")]
public class LexiconDieSO : ScriptableObject
{
    [Header("Die Identity")]
    public string dieName;
    public DiceType visualShape;
    public string[] defaultFaces;

    [Header("2.5D / 3D Visuals")]
    [Tooltip("The flat front-facing 2D sprite.")]
    public Sprite dieSprite;
    [Tooltip("Drag the specific 3D/UI Prefab for this die here!")]
    public GameObject diePrefab;
    [Tooltip("Optional: Material for 3D dice or shiny 2.5D effects.")]
    public Material dieMaterial;
    [Tooltip("The base color of the die to give it depth and identity.")]
    public Color dieBaseColor = Color.white;
}