using UnityEngine;
using TMPro;

public class TooltipSystem : MonoBehaviour
{
    public static TooltipSystem Instance;

    public GameObject tooltipObject;
    public TMP_Text tooltipText;

    void Awake()
    {
        Instance = this;
        Hide();
    }

    public static void Show(string content)
    {
        Instance.tooltipObject.SetActive(true);
        Instance.tooltipText.text = content;
    }

    public static void Hide()
    {
        Instance.tooltipObject.SetActive(false);
    }
}
