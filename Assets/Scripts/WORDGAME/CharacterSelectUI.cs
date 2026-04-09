using UnityEngine;
using System.Collections.Generic;

public class CharacterSelectUI : MonoBehaviour
{
    [Header("Available Classes")]
    public List<LexiconCharacterSO> availableCharacters;

    [Header("Grid UI References")]
    [Tooltip("The parent object with a Grid Layout Group or Horizontal Layout Group.")]
    public Transform characterCardContainer;
    [Tooltip("The actual Card Prefab representing a Class.")]
    public GameObject characterCardPrefab;

    private void OnEnable()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (characterCardContainer == null || characterCardPrefab == null) return;

        // NEW: 1. Hide all existing cards instead of destroying them
        for (int i = 0; i < characterCardContainer.childCount; i++)
        {
            characterCardContainer.GetChild(i).gameObject.SetActive(false);
        }

        // NEW: 2. Reuse hidden cards, or spawn new ones only if we run out
        for (int i = 0; i < availableCharacters.Count; i++)
        {
            GameObject cardObj;
            if (i < characterCardContainer.childCount)
            {
                // Reuse an existing pooled card
                cardObj = characterCardContainer.GetChild(i).gameObject;
                cardObj.SetActive(true);
            }
            else
            {
                // Spawn a new one (only happens the first time!)
                cardObj = Instantiate(characterCardPrefab, characterCardContainer);
            }

            CharacterCardUI cardUI = cardObj.GetComponent<CharacterCardUI>();
            if (cardUI != null)
            {
                cardUI.SetupCard(availableCharacters[i]);
            }
        }
    }
}