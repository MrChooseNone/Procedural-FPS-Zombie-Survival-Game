using UnityEngine;
public class PlayerSkills : MonoBehaviour
{
    public Skill[] skills;

    private void Awake() 
    {
        // Initialize all skills
        skills = new Skill[4];
        for (int i = 0; i < 4; i++)
        {
            skills[i] = new Skill { type = (SkillType)i };
        }
    }

    public void GainXP(SkillType type, float amount)
    {
        skills[(int)type].AddXP(amount);
    }

    public int GetSkillLevel(SkillType type)
    {
        return skills[(int)type].level;
    }
    public float GetSkillXp(SkillType type)
    {
        return skills[(int)type].xp;
    }
    public float GetSkillMaxXp(SkillType type)
    {
        return skills[(int)type].xpToNextLevel;
    }

    public bool HasUnlocked(SkillType type, int requiredLevel)
    {
        return skills[(int)type].level >= requiredLevel;
    }
}