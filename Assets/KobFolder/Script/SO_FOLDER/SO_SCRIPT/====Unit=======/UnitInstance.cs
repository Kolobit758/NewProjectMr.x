using UnityEngine;

[System.Serializable]
public class UnitInstance
{
    public string uniqueId;
    public string customName;
    public UnitDataSO template;

    [Header("Runtime Stats")]
    public int maxHP;
    public int currentHP;
    public float moveSpeed;
    public float attackDamage;
    public float attackRange;
    public float guardRadius;

    [Header("Formation Status")]
    public bool isInFormation;       // ✅ แก้ไขเออร์เรอร์: เพิ่มตัวแปรนี้ตามที่ TamedUnitsManager เรียกใช้
    public string formationSlotKey;   // ✅ แก้ไขเออร์เรอร์: เพิ่มตัวแปรนี้เพื่อระบุพิกัดสล็อต
    [Header("Skill Runtime")]
    public float currentCooldownTimer; // ⏳ ตัวนับเวลาคูลดาวน์ปัจจุบันรายตัว

    public UnitInstance(UnitDataSO templateData)
    {
        template = templateData;
        uniqueId = System.Guid.NewGuid().ToString();
        customName = templateData.speciesName;

        maxHP = templateData.baseMaxHP + UnityEngine.Random.Range(-templateData.hpRandomRange, templateData.hpRandomRange + 1);
        currentHP = maxHP;

        moveSpeed = templateData.baseMoveSpeed;
        attackRange = templateData.baseAttackRange;
        guardRadius = templateData.baseGuardRadius;

        attackDamage = templateData.baseAttackDamage + UnityEngine.Random.Range(-templateData.attackRandomRange, templateData.attackRandomRange);
        attackDamage = Mathf.Max(1f, attackDamage); 

        isInFormation = false;
        formationSlotKey = string.Empty;
        currentCooldownTimer = 0f;
    }

    public bool IsSkillReady()
    {
        return template.uniqueSkill != null && currentCooldownTimer <= 0f;
    }
}