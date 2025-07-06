using UnityEngine.UI;
using UnityEngine;

public class DisplaySkills : MonoBehaviour
{
    public PlayerSkills playerSkills; 
    [System.Serializable]
    public class UISkill 
    {
        public int requiredLevel;
        public SkillType type;
        public Image image;

    }

    public UISkill[] uiSkills;

    public void UpdateUISkills()
    {
        foreach (UISkill skill in uiSkills) {
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
    }

}