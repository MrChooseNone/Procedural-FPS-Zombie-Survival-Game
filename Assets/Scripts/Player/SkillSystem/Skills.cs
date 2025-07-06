using UnityEngine;

[System.Serializable]
public class Skill
{
    public SkillType type;
    public int level = 0;
    public float xp = 0;
    public float xpToNextLevel = 100f;

    public void AddXP(float amount)
    {
        xp += amount;
        if (xp >= xpToNextLevel)
        {
            xp -= xpToNextLevel;
            level++;
            xpToNextLevel *= 1.25f; // Increase XP requirement
            Debug.Log($"[Skill] {type} leveled up to {level}!");
        }
    }
}