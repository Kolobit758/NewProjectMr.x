using UnityEngine;

[CreateAssetMenu(fileName = "NewSeed", menuName = "Plants/Seed Data")]
public class SO_PlantData : SO_ItemData
{
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