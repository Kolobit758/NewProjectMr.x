using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ระบบเลือกซื้อพืชแบบ data-driven (แพทเทิร์นเดียวกับ CategoryInventoryUI)
/// ในนี้เก็บแค่ "data" ของพืชที่ขาย (SO_PlantData + ราคา) ไม่ผูก UI ตรงๆ
/// พอรันจะ Instantiate slotPrefab ใต้ container ตามจำนวน data ที่มี แล้วยัดข้อมูลลงไปให้แต่ละช่อง
///
/// การกดปุ่มแค่เลือกเมล็ดใส่มือ (currentSelectedSeed ใน FarmingManager) ไม่หักเงินตรงนี้
/// เงินจะถูกหักตอนคลิกวางบน CropPlots จริงๆ (ดูใน FarmingManager.TryInteractWithPlot)
/// </summary>
public class PlantShopUI : MonoBehaviour
{
    [System.Serializable]
    public class ShopItemConfig
    {
        [Header("ข้อมูลเมล็ด")]
        public SO_PlantData plantData;

        [Header("ราคา (หน่วยเป็นทอง) - จะถูกหักตอนวางจริง")]
        public int price = 10;
    }

    [Header("Data: รายการพืชที่เลือกซื้อได้")]
    public List<ShopItemConfig> shopItems = new List<ShopItemConfig>();

    [Header("UI Spawn Settings")]
    [Tooltip("Parent ที่จะเอา slot ไปวาง (ควรมี Layout Group ติดอยู่ เช่น Grid Layout Group)")]
    public Transform container;

    [Tooltip("Prefab ของช่องขายพืช 1 ช่อง ต้องมีคอมโพเนนต์ PlantShopSlotUI ติดอยู่")]
    public GameObject slotPrefab;

    private readonly List<PlantShopSlotUI> spawnedSlots = new List<PlantShopSlotUI>();

    void OnEnable()
    {
        BuildShopUI();
    }

    private void BuildShopUI()
    {
        if (container == null || slotPrefab == null)
        {
            Debug.LogWarning("[PlantShopUI] ยังไม่ได้ตั้งค่า container หรือ slotPrefab");
            return;
        }

        // เคลียร์ของเก่าก่อน กันกรณี OnEnable ถูกเรียกซ้ำ (เช่น เปิด-ปิด panel ร้านค้า)
        foreach (var slot in spawnedSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        spawnedSlots.Clear();

        foreach (var config in shopItems)
        {
            if (config == null || config.plantData == null) continue;

            GameObject slotGo = Instantiate(slotPrefab, container);
            PlantShopSlotUI slotScript = slotGo.GetComponent<PlantShopSlotUI>();

            if (slotScript == null)
            {
                Debug.LogWarning("[PlantShopUI] slotPrefab ไม่มีคอมโพเนนต์ PlantShopSlotUI ติดอยู่");
                Destroy(slotGo);
                continue;
            }

            slotScript.Setup(config.plantData, config.price, HandleSeedSelected);
            spawnedSlots.Add(slotScript);
        }
    }

    private void HandleSeedSelected(SO_PlantData plantData, int price)
    {
        if (plantData == null) return;

        if (FarmingManager.Instance == null)
        {
            Debug.LogWarning("[PlantShopUI] ไม่พบ FarmingManager.Instance ในซีน");
            return;
        }

        // ✅ แค่ยัดเมล็ดใส่มือ ยังไม่หักเงิน - จะหักตอนคลิกวางบน CropPlots จริง
        FarmingManager.Instance.SelectSeedFromShop(plantData, price);

        Debug.Log($"🌱 [Shop] เลือกเมล็ด {plantData.itemName} (ราคา {price} G) พร้อมนำไปวาง - จะหักเงินตอนวางจริงเท่านั้น");
    }
}