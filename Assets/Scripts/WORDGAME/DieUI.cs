using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Added IBeginDragHandler, IDragHandler, IEndDragHandler for tactile movement
public class DieUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
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

        if (dieBackgroundImage != null && data.dieSprite != null) dieBackgroundImage.sprite = data.dieSprite;

        if (faceText != null && data.currentFace != null)
        {
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
}