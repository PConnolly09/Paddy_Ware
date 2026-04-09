using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DraftCardUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI scoreText;

    [Header("Visual Layout Containers")]
    public Transform dieVisualContainer;
    public Transform effectsContainer;

    private RunManager.DraftUpgradeOption upgradeData;
    private RunManager.DraftMutateOption mutateData;
    private bool isMutation;
    private GameObject spawnedDieVisual;

    public void SetupUpgrade(RunManager.DraftUpgradeOption data)
    {
        upgradeData = data;
        isMutation = false;

        if (titleText != null) titleText.text = "UPGRADE";
        if (descriptionText != null) descriptionText.text = $"Add +1 Bonus Score to face [{data.face.faceText}] on {data.die.dieName}.";

        int baseScore = GetBaseScore(data.face.faceText);
        int totalBonus = data.face.bonusScore + 1;

        if (scoreText != null)
            scoreText.text = $"Score: {baseScore} <color=#00FF00>+{totalBonus}</color>";

        SpawnDiePrefab(data.die, data.face.faceText, totalBonus);
    }

    public void SetupMutation(RunManager.DraftMutateOption data)
    {
        mutateData = data;
        isMutation = true;

        if (titleText != null) titleText.text = "MUTATE";
        if (descriptionText != null) descriptionText.text = $"Change face [{data.face.faceText}] on {data.die.dieName} to [{data.newFaceText}].";

        int oldBaseScore = GetBaseScore(data.face.faceText);
        int newBaseScore = GetBaseScore(data.newFaceText);
        int diff = newBaseScore - oldBaseScore;

        if (scoreText != null)
        {
            string diffString = "";
            if (diff > 0) diffString = $" <color=#00FF00>(+{diff})</color>";
            else if (diff < 0) diffString = $" <color=#FF0000>({diff})</color>";
            else diffString = $" <color=#AAAAAA>(+0)</color>";

            string bonusString = data.face.bonusScore > 0 ? $" <color=#00FF00>+{data.face.bonusScore}</color>" : "";
            scoreText.text = $"Score: {newBaseScore}{bonusString}{diffString}";
        }

        SpawnDiePrefab(data.die, data.newFaceText, data.face.bonusScore);
    }

    private int GetBaseScore(string faceText)
    {
        if (WordValidator.Instance != null && !string.IsNullOrEmpty(faceText))
            return WordValidator.Instance.GetLetterScore(faceText[0]);
        return 0;
    }

    private void SpawnDiePrefab(DieData dieData, string faceToDisplay, int appliedBonus)
    {
        if (spawnedDieVisual != null) Destroy(spawnedDieVisual);
        if (dieVisualContainer == null) return;

        if (dieData.diePrefab != null)
        {
            spawnedDieVisual = Instantiate(dieData.diePrefab, dieVisualContainer);

            // FORCE SCALE AND POSITION TO BE PERFECT
            spawnedDieVisual.transform.localScale = Vector3.one;
            spawnedDieVisual.transform.localPosition = Vector3.zero;

            DieUI uiComponent = spawnedDieVisual.GetComponent<DieUI>();
            if (uiComponent != null)
            {
                DieFace tempFace = new DieFace { faceText = faceToDisplay, bonusScore = appliedBonus };
                DieData tempData = new DieData { dieName = dieData.dieName, currentFace = tempFace, dieSprite = dieData.dieSprite };
                uiComponent.SetupVisuals(tempData);

                // Disable the button so the player can't accidentally "click" the die on the draft card
                Button btn = uiComponent.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }
        }
    }

    public void OnCardClicked()
    {
        if (isMutation) RunManager.Instance.ApplyDraftMutation(mutateData);
        else RunManager.Instance.ApplyDraftUpgrade(upgradeData);
    }
}