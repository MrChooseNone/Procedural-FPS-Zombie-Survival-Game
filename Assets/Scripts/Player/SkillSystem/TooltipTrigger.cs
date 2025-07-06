using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea]
    public string tooltipText;

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipSystem.Show(tooltipText);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipSystem.Hide();
    }
}
