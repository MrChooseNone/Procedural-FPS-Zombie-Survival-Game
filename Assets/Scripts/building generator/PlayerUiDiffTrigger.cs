using UnityEngine;
using Mirror;
using TMPro; // or UnityEngine.UI if using standard UI Text

public class PlayerUIDiffTrigger : NetworkBehaviour
{
    public GameObject difficultyUIPanel;
    public TextMeshProUGUI difficultyText;

    public void ShowDifficultyUI(int level)
    {
        if (!isLocalPlayer) return;

        if (difficultyUIPanel != null && difficultyText != null)
        {
            difficultyText.text = $"{level}";
            difficultyUIPanel.SetActive(true);
        }
    }

    public void HideDifficultyUI()
    {
        if (!isLocalPlayer) return;

        if (difficultyUIPanel != null)
        {
            difficultyUIPanel.SetActive(false);
        }
    }
}
