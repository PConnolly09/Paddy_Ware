using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DieUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("UI Components")]
    public Image dieBackgroundImage;
    public TextMeshProUGUI faceText;
    public TextMeshProUGUI scoreText;
    public GameObject splitFaceIcon;

    [HideInInspector] public DieData myData;
    public bool isInHand = true; // <--- ADD THIS BACK!

    private Transform _originalParent;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
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
            dieBackgroundImage.color = Color.white; // Pure white, no heat tints
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
            scoreText.text = data.currentFace.GetTotalScore(data.currentFace.faceText).ToString();
        }

        if (splitFaceIcon != null)
            splitFaceIcon.SetActive(data.currentFace != null && data.currentFace.HasSplitFace);
    }

    // ==========================================
    // DRAG AND DROP LOGIC
    // ==========================================
    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalParent = transform.parent;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();

        if (_canvasGroup != null) _canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_canvasGroup != null) _canvasGroup.blocksRaycasts = true;

        // If it wasn't dropped in a valid slot, snap it back to where it came from
        if (transform.parent == transform.root)
        {
            transform.SetParent(_originalParent);
        }
    }
    // ==========================================
    // CLICK SELECTION LOGIC
    // ==========================================

    // If you use the IPointerClickHandler interface:
    public void OnPointerClick(PointerEventData eventData)
    {
        // Prevent accidental clicks when you are just trying to drag the die
        if (eventData.dragging) return;

        TriggerSelection();
    }

    // If you use a Unity 'Button' component on your Prefab:
    public void OnDieClicked()
    {
        TriggerSelection();
    }

    private void TriggerSelection()
    {
        if (WordUIManager.Instance != null)
        {
            // Toggle the die between the hand and the board based on its current state
            if (isInHand)
            {
                WordUIManager.Instance.MoveDieToWord(this);
            }
            else
            {
                WordUIManager.Instance.MoveDieToHand(this);
            }
        }
        else
        {
            Debug.LogError("WordUIManager Instance is missing! Cannot move die.");
        }
    }
}