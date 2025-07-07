using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class DisplaySkills : MonoBehaviour
{
    [System.Serializable]
    public class DisplayLevel
    {
        public Image progressionBar;
        public TextMeshProUGUI text;
        public SkillType type;
    }
    public PlayerSkills playerSkills; 
    [System.Serializable]
    public class UISkill 
    {
        public int requiredLevel;
        public SkillType type;
        public Image image;

    }

    public UISkill[] uiSkills;
    public DisplayLevel[] displayLevels;

    public void UpdateUISkills()
    {
        foreach (UISkill skill in uiSkills)
        {
            if (playerSkills.HasUnlocked(skill.type, skill.requiredLevel)) 
            {
                Color color = skill.image.color;
                color.a = 1f;
                skill.image.color = color;
            }
            else
            {
                Color color = skill.image.color;
                color.a = 0.5f;
                skill.image.color = color;
            }
        }
        foreach (DisplayLevel level in displayLevels)
        {
            if (playerSkills != null)
            {
                int skillLevel = playerSkills.GetSkillLevel(level.type);
                level.text.text = skillLevel.ToString();
                float currentXp = playerSkills.GetSkillXp(level.type);
                float maxXp = playerSkills.GetSkillMaxXp(level.type);

                level.progressionBar.fillAmount = maxXp > 0f ? Mathf.Clamp01(currentXp / maxXp) : 0f;


            }
        }
    }

}