using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler
{
    [Tooltip("Check this if this zone is the Hand. Uncheck if it's the Word submission area.")]
    public bool isHandZone = true;

    public void OnDrop(PointerEventData eventData)
    {
        DieUI droppedDie = eventData.pointerDrag.GetComponent<DieUI>();

        if (droppedDie != null)
        {
            droppedDie.transform.SetParent(transform);

            // --- SIBLING INDEX CALCULATION ---
            int newSiblingIndex = transform.childCount; // Default to the far right

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);

                // Compare the mouse's X screen position to the UI element's X screen position
                if (eventData.position.x < child.position.x)
                {
                    newSiblingIndex = i;

                    // If we are dragging leftwards within the same container, we must adjust 
                    // the index by -1 because the dragged object itself is taking up an early slot!
                    if (droppedDie.transform.GetSiblingIndex() < newSiblingIndex)
                    {
                        newSiblingIndex--;
                    }
                    break;
                }
            }

            droppedDie.transform.SetSiblingIndex(newSiblingIndex);
            // ---------------------------------

            // Tell the UIManager to officially update its state
            if (WordUIManager.Instance != null)
            {
                if (isHandZone) WordUIManager.Instance.MoveDieToHand(droppedDie);
                else WordUIManager.Instance.MoveDieToWord(droppedDie);
            }
        }
    }
}