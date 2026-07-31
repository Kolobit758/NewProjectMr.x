using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSeed", menuName = "Plants/Seed Data")]
public class SO_PlantData : SO_ItemData
{
    [Header("Ideal Soil Requirements (ธาตุอาหารที่พืชชนิดนี้ต้องการ)")]
    public NutrientData idealNutrients; // ค่า N, P, K ในอุดมคติที่พืชชอบ (เช่น ตั้งเป้า N:50, P:30, K:40)
    public float toleranceRange = 20f;  // ค่าความคลาดเคลื่อนที่ยอมรับได้ (เช่น +/- 20 ยังโตปกติ ถ้าเกินกว่านี้เริ่มมีปัญหา)

    [Header("Harvest Results (ผลผลิตที่จะได้ตามคุณภาพ)")]
    public SO_ItemData normalProduct;    // ผลผลิตเกรดปกติ (โตตามมีตามเกิด)
    public SO_ItemData premiumProduct;   // ผลผลิตเกรดพรีเมียม (ดูแลธาตุอาหารได้ตรงตามที่พืชชอบเป๊ะๆ)
    public SO_ItemData mutatedProduct;   // ผลผลิตกลายพันธุ์ (เกิดจากการใช้ปุ๋ยผิดสูตรสุดขั้ว หรือสูตรลับเฉพาะ)
    [Header("Farming Settings")]
    public float timeToGrow = 15f; // เวลาที่ใช้ในการโตทั้งหมด (วินาที)

    [Header("Prefabs for Visual Stages")]
    public GameObject seedPrefab;       // โมเดลตอนเพิ่งหยอดเมล็ด
    public GameObject growingPrefab;    // โมเดลตอนกำลังโต
    public GameObject fullyGrownPrefab; // โมเดลตอนโตเต็มที่พร้อมเก็บเกี่ยว

    [Header("Harvest Rewards")]
    public SO_ItemData cropProduct;     // ผลผลิตผักที่จะได้รับ (เช่น ผักกาด/แครอท)
    public SO_ItemData cropSeed;     // ผลผลิตผักที่จะได้รับ (เช่น ผักกาด/แครอท)
    public int minProductAmount = 1;
    public int maxProductAmount = 3;

    public SO_ItemData poopFertilizerProduct; // ไอเทมปุ๋ยมูลสัตว์ที่จะแจกคู่กัน
    public int fertilizerAmount = 1;
    [Header("Enemy Attraction Settings")]
    public List<SO_EnemyData> specificAttractedEnemies = new List<SO_EnemyData>(); // 🎯 เปลี่ยนเป็น List เพื่อรองรับศัตรูหลายตัวต่อพืชหนึ่งชนิด

    // 🟢 เมื่อคนเล่นกดใช้งานเมล็ดพันธุ์นี้จากในกระเป๋า/Hotbar
    public override bool UseItem(GameObject user)
    {
        if (FarmingManager.Instance != null)
        {
            // ยัดข้อมูลเมล็ดต้นนี้เข้ามือ FarmingManager เพื่อเตรียมเล็งจิ้มแปลงผักทันที!
            FarmingManager.Instance.currentSelectedSeed = this;

            Debug.Log($"🌱 [Farming System] ถือเมล็ด {itemName} ในมือ เล็งแปลงผักเตรียมหยอดเมล็ดแล้วมึงกอบ!");
            return true;
        }
        return false;
    }
}