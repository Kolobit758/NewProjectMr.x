using UnityEngine;

public enum SkillType { Buff, PuzzleInteract, ObstacleBypass }

public abstract class UnitSkillSO : ScriptableObject
{
    [Header("Skill Identity")]
    public string skillId;
    public string skillName;
    [TextArea] public string description;
    public Sprite skillIcon;
    public SkillType skillType;

    [Header("Settings")]
    public float cooldown = 5f;

    // ฟังก์ชันนี้จะถูกเรียกใช้งานเมื่อกดการ์ดบน UI
    public abstract void ExecuteSkill(GameObject user);
}