using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Added IBeginDragHandler, IDragHandler, IEndDragHandler for tactile movement
public class DieUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual References")]
    public Image dieBackgroundImage;
    public TextMeshProUGUI faceText;
    public TextMeshProUGUI scoreText;
    public GameObject splitFaceIcon;

    [HideInInspector] public DieData myData;
    [HideInInspector] public bool isInHand = true;
    [HideInInspector] public Transform originalParent;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        // CanvasGroup is required to block raycasts while dragging, 
        // allowing the mouse to detect the drop zones behind the die.
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetupVisuals(DieData data)
    {
        myData = data;
        gameObject.SetActive(true);

        if (dieBackgroundImage != null && data.dieSprite != null)
        {
            dieBackgroundImage.sprite = data.dieSprite;

            // FIX: Use soft pastel tints so the text remains highly readable!
            if (data.consecutivePlays == 0 || data.consecutivePlays == 1)
                dieBackgroundImage.color = Color.white;
            else if (data.consecutivePlays == 2)
                dieBackgroundImage.color = new Color(1f, 0.9f, 0.7f); // Pale Orange/Warm
            else if (data.consecutivePlays >= 3)
                dieBackgroundImage.color = new Color(1f, 0.7f, 0.7f); // Pale Red/Critical
        }

        if (faceText != null && data.currentFace != null)
        {
            if (data.currentFace.HasSplitFace)
                faceText.text = $"{data.currentFace.faceText}<size=60%><color=#AAAAAA>/{data.currentFace.altFaceText}</color></size>";
            else
                faceText.text = data.currentFace.faceText;
        }

        if (scoreText != null && data.currentFace != null && WordValidator.Instance != null)
        {
            int totalScore = data.currentFace.GetTotalScore(data.currentFace.faceText);

            if (data.consecutivePlays == 2) totalScore = Mathf.RoundToInt(totalScore * 1.5f);
            else if (data.consecutivePlays >= 3) totalScore = Mathf.RoundToInt(totalScore * 2.0f);

            string scoreStr = totalScore.ToString();
            if (data.consecutivePlays >= 2) scoreStr = $"<color=#FF0000>{scoreStr}</color>";

            scoreText.text = scoreStr;
        }

        if (splitFaceIcon != null) splitFaceIcon.SetActive(data.currentFace != null && data.currentFace.HasSplitFace);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (myData == null || WordUIManager.Instance == null) return;

        string tooltipString = $"<size=110%><b><color=#00FFFF>{myData.dieName.ToUpper()}</color></b></size>\n";
        tooltipString += "<color=#AAAAAA>Faces:</color> ";

        foreach (DieFace face in myData.faces)
        {
            string bonusStr = face.bonusScore > 0 ? $"<color=#00FF00>+{face.bonusScore}</color>" : "";
            if (face.HasSplitFace) tooltipString += $"[{face.faceText}/{face.altFaceText}]{bonusStr} ";
            else tooltipString += $"[{face.faceText}]{bonusStr} ";
        }

        // NEW: EXPLICIT OVERLOAD CLARITY
        tooltipString += "\n\n<color=#AAAAAA>--- Core Temp ---</color>\n";
        if (myData.consecutivePlays == 0) tooltipString += "<color=#00FF00>Stable (0/3)</color>";
        else if (myData.consecutivePlays == 1) tooltipString += "<color=#FFFF00>Warming (1/3)</color>";
        else if (myData.consecutivePlays == 2) tooltipString += "<color=#FFAA00>Critical (2/3) - 1.5x Score!</color>";
        else tooltipString += "<color=#FF0000>OVERLOADED! Will shatter next cast!</color>";

        tooltipString += "\n<size=80%><i>(Cools down if unused for 1 turn)</i></size>";

        WordUIManager.Instance.ShowHoverTooltip(tooltipString);
    }

    // ==========================================
    // DRAG AND DROP LOGIC
    // ==========================================
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;

        // Pop the die out of the Layout Group and put it on the root Canvas 
        // so it renders on top of everything else while dragging
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();

        // Turn off raycasts so the mouse can "see" through the die to the drop zones below
        _canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Follow the mouse
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;

        // If the die didn't get caught by a valid DropZone, snap it back to where it started
        if (transform.parent == transform.root)
        {
            transform.SetParent(originalParent);
        }
    }

    // ==========================================
    // CLICK LOGIC (Fallback)
    // ==========================================
    public void OnClick_TogglePosition()
    {
        if (WordUIManager.Instance == null || myData == null) return;
        if (isInHand) WordUIManager.Instance.MoveDieToWord(this);
        else WordUIManager.Instance.MoveDieToHand(this);
    }

    public void OnClick_FlipSplitFace()
    {
        if (myData != null && myData.currentFace != null && myData.currentFace.HasSplitFace)
        {
            myData.currentFace.ToggleSplitFace();
            SetupVisuals(myData);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (myData == null || WordUIManager.Instance == null) return;

        // Build a string of all the faces on this die
        string tooltipString = $"<color=#00FFFF>{myData.dieName.ToUpper()}</color>\n";

        foreach (DieFace face in myData.faces)
        {
            string bonusStr = face.bonusScore > 0 ? $"<color=#00FF00>+{face.bonusScore}</color>" : "";

            if (face.HasSplitFace)
                tooltipString += $"[{face.faceText}/{face.altFaceText}]{bonusStr} ";
            else
                tooltipString += $"[{face.faceText}]{bonusStr} ";
        }

        // Send it to the UI Manager to display
        WordUIManager.Instance.ShowHoverTooltip(tooltipString);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (WordUIManager.Instance != null)
        {
            WordUIManager.Instance.HideHoverTooltip();
        }
    }
}
}