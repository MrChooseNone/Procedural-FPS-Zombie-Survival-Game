using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIHoverSpriteChange : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Sprite normalSprite;     // The default sprite
    public Sprite hoverSprite;      // The sprite shown on hover

    private Image image;

    void Awake()
    {
        image = GetComponent<Image>();
        if (image != null && normalSprite == null)
        {
            normalSprite = image.sprite; // Save the original if not set
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (image != null && hoverSprite != null)
        {
            image.sprite = hoverSprite;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (image != null && normalSprite != null)
        {
            image.sprite = normalSprite;
        }
    }
}
