using UnityEngine;

[CreateAssetMenu(fileName = "NewFertilizer", menuName = "Inventory/Fertilizer Data")]
public class SO_FertilizerData : SO_ItemData
{
    [Header("Fertilizer N-P-K Values (ปริมาณธาตุอาหารที่ปุ๋ยนี้เติมให้ดิน)")]
    public NutrientData providedNutrients; // เช่น ปุ๋ยสูตรเร่งใบ (N สูงมาก P, K น้อย)

    [Header("Effects")]
    public float growthSpeedMultiplier = 1.5f;
    [Range(0f, 100f)]
    public float mutationChance = 10f;

    public override bool UseItem(GameObject user)
    {
        // 🟢 3. ถ้าเป็นปุ๋ย (Fertilizer) ➡️ เปิดโหมดโปรยปุ๋ย (เดี๋ยวเราสร้าง FertilizerManager มารองรับเพิ่มด้านล่างครับ)
        if (itemType == ItemType.Fertilizer)
        {
            if (FertilizerManager2.Instance != null)
            {
                FertilizerManager2.Instance.StartFertilizeMode(this);
                Debug.Log($"🧪 [Item]: เปิดโหมดโปรยปุ๋ย {itemName} แล้ว!");
                return true;
            }
        }
        return false;
    }
}