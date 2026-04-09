using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DieUI : MonoBehaviour
{
    [Header("Visual References")]
    public Image dieBackgroundImage;
    public TextMeshProUGUI faceText;
    public TextMeshProUGUI scoreText;
    public GameObject splitFaceIcon; // Optional: A little icon to show it can be flipped

    [HideInInspector] public DieData myData;
    [HideInInspector] public bool isInHand = true;

    public void SetupVisuals(DieData data)
    {
        myData = data;
        gameObject.SetActive(true);

        if (dieBackgroundImage != null && data.dieSprite != null) dieBackgroundImage.sprite = data.dieSprite;

        if (faceText != null && data.currentFace != null)
        {
            // If it's a Boss Mutation, show both letters! Otherwise, just the one.
            if (data.currentFace.HasSplitFace) faceText.text = $"{data.currentFace.faceText}<size=60%><color=#AAAAAA>/{data.currentFace.altFaceText}</color></size>";
            else faceText.text = data.currentFace.faceText;
        }

        if (scoreText != null && data.currentFace != null && WordValidator.Instance != null)
        {
            int totalScore = data.currentFace.GetTotalScore(data.currentFace.faceText);
            if (data.currentFace.bonusScore > 0) scoreText.text = $"<color=#00FF00>{totalScore}</color>";
            else scoreText.text = totalScore.ToString();
        }

        if (splitFaceIcon != null) splitFaceIcon.SetActive(data.currentFace != null && data.currentFace.HasSplitFace);
    }

    public void OnClick_TogglePosition()
    {
        if (WordUIManager.Instance == null || myData == null) return;
        if (isInHand) WordUIManager.Instance.MoveDieToWord(this);
        else WordUIManager.Instance.MoveDieToHand(this);
    }

    // Link this to a tiny "Swap" button on your Die Prefab!
    public void OnClick_FlipSplitFace()
    {
        if (myData != null && myData.currentFace != null && myData.currentFace.HasSplitFace)
        {
            myData.currentFace.ToggleSplitFace();
            SetupVisuals(myData); // Refresh to show the new active letter
        }
    }
}