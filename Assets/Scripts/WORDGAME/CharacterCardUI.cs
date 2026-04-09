using UnityEngine;
using TMPro;

public class CharacterCardUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI classNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI statsText;

    private LexiconCharacterSO myCharacter;

    public void SetupCard(LexiconCharacterSO character)
    {
        myCharacter = character;

        if (classNameText != null) classNameText.text = character.className.ToUpper();
        if (descriptionText != null) descriptionText.text = character.classDescription;

        if (statsText != null)
        {
            statsText.text =
                $"<color=#FFD700>STARTING CREDITS: {character.startingCredits}</color>\n" +
                $"<color=#00FF00>BONUS QUERIES: +{character.bonusQueries}</color>\n" +
                $"<color=#FF0000>FIREWALL DEBUFF: -{character.bonusStartingHP} HP</color>";
        }
    }

    // Link this to the Button component on your Card Prefab!
    public void OnClick_SelectCharacter()
    {
        if (myCharacter == null) return;

        // 1. Give the chosen data to the RunManager
        RunManager.Instance.InitializeNewRun(myCharacter);

        // 2. Tell the GameDirector to move us to the Pre-Run Shop
        if (GameDirector.Instance != null)
        {
            GameDirector.Instance.OnClick_ConfirmCharacter();
        }
    }
}