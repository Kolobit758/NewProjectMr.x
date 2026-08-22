using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitTemplate", menuName = "Combat/Unit Template")]
public class UnitDataSO : ScriptableObject
{
    [Header("Species Configuration")]
    public string speciesName = "Wild Animal";
    public GameObject unitPrefab;
    public Sprite unitIcon;
    public int coinCost = 50; // 💰 ราคาเงินที่ใช้ซื้อยูนิตตัวนี้ (ตั้งค่าจบที่ SO นี้ที่เดียว)

    [Header("Base Stats")]
    public int baseMaxHP = 50;
    public float baseMoveSpeed = 5.5f;
    public float baseAttackRange = 1.5f;
    public float baseGuardRadius = 5.0f;
    public float baseAttackDamage = 10f;

    [Header("Randomization Bounds (Offsets)")]
    public int hpRandomRange = 10;            // baseMaxHP +/- random(0, hpRandomRange)
    public float attackRandomRange = 2f;    // baseAttackDamage +/- random(0, attackRandomRange)

    [Header("Skill Configuration")]
    public UnitSkillSO uniqueSkill; // 🔥 แปะสกิลประจำตัวสายพันธุ์ตรงนี้
}