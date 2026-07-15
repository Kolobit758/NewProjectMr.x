using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewOutpostData", menuName = "Tactical/Outpost Data")]
public class OutpostDataSO : ScriptableObject
{
    [Header("Identity")]
    public string outpostID = "outpost_default";
    public string outpostName = "ค่ายย่อยใหม่";
    public bool isCaptured = false;

    [Header("Resource Production")]
    // 🟢 รองรับคลังส่วยแร่ผูกกับ SO_ItemData ของทีมมึงโดยตรง
    public SO_ItemData resourceToProduce;
    public int amountPerTick = 5;

    [Header("Garrison Status (กองทัพสัตว์เลี้ยงประจำการ)")]
    // ⚔️ ลิสต์ทหารเฝ้าค่ายที่ดึงคลาส UnitInstance ของมึงมาใช้ ข้อมูลเลือด/Stat จริงจะถูกจำไว้ในนี้ข้ามซีน!
    public List<UnitInstance> garrisonUnits = new List<UnitInstance>();

    [Header("Supply & Hunger System")]
    public float hungerLevel = 100f;         // 100 = อิ่มเต็มที่, 0 = หิวโซไม่มีแรง
    public float hungerDecreaseRate = 1.5f;   // อัตราความหิวลดลงต่อวินาทีใน RAM

    [Header("Food Buff Modifiers")]
    public float attackBuffMultiplier = 1f;
    public float defenseBuffMultiplier = 1f;

    /// <summary>
    /// 🥗 ฟังก์ชันป้อนอาหารผักแปรรูป (UI ซีนฟาร์มจะมาเรียกใช้ฟังก์ชันนี้ผ่าน ID ค่าย)
    /// </summary>
    public void FeedOutpost(float foodAmount, float atkBuff, float defBuff)
    {
        hungerLevel = Mathf.Min(hungerLevel + foodAmount, 100f);
        attackBuffMultiplier = atkBuff;
        defenseBuffMultiplier = defBuff;
        
        Debug.Log($"📦 SO [{outpostName}]: ได้รับเสบียงผักเพิ่ม {foodAmount}! ความหิวรีเซ็ตเป็น: {hungerLevel} | พลังโจมตีได้บัฟ x{attackBuffMultiplier}");
    }

    /// <summary>
    /// ⚔️ คำนวณพลังโจมตีสุทธิของค่ายย่อย (รวมดาเมจยูนิตทุกตัว + บัฟอาหาร + ดักสเตตัสความหิว)
    /// </summary>
    public float GetTotalGarrisonAttack()
    {
        float totalAttack = 0f;

        // วนลูปบวกพลังโจมตีจาก UnitInstance ทุกตัวที่เฝ้าอยู่จริง
        foreach (var unit in garrisonUnits)
        {
            if (unit != null)
            {
                totalAttack += unit.attackDamage;
            }
        }

        // 🍖 ลอจิกบริหารความหิว: ถ้ายูนิตอดอยากจนหิวเหลือ 0 พลังรบสุทธิจะดิ่งฮวบเหลือแค่ 50% ทันที!
        float hungerFactor = hungerLevel / 100f;
        if (hungerLevel <= 0)
        {
            hungerFactor = 0.5f; 
        }

        return totalAttack * attackBuffMultiplier * hungerFactor;
    }
}