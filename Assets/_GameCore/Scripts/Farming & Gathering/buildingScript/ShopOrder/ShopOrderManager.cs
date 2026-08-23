using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct OrderRequirement
{
    public SO_ItemData requiredItem;
    public int amount;
}

[System.Serializable]
public class ProceduralOrder
{
    public string orderTitle;
    public List<OrderRequirement> requirements = new List<OrderRequirement>();
    public int rewardGold;
}

public class ShopOrderManager : MonoBehaviour
{
    public static ShopOrderManager Instance { get; private set; }

    [Header("Seasonal Item Pools")]
    public List<SO_ItemData> springItems = new List<SO_ItemData>();
    public List<SO_ItemData> summerItems = new List<SO_ItemData>();
    public List<SO_ItemData> autumnItems = new List<SO_ItemData>();
    public List<SO_ItemData> winterItems = new List<SO_ItemData>();

    [Header("Active Daily Orders")]
    public List<ProceduralOrder> activeDailyOrders = new List<ProceduralOrder>();

    [Header("Generation Settings")]
    public int minItemsPerOrder = 1;
    public int maxItemsPerOrder = 3;
    public int minAmountPerItem = 2;
    public int maxAmountPerItem = 8;

    [Header("Order Type Settings")]
    [Tooltip("จำนวนเควส (จากทั้งหมด 5 ใบ) ที่บังคับให้เป็นประเภท Resource ล้วนๆ ที่เหลือจะสุ่มเป็น Resource/Plant/Mixed")]
    public int guaranteedResourceOrders = 3;

    [Tooltip("ItemType ที่ถือว่าเป็น 'Resource' เช่น ไม้ น้ำ วัตถุดิบทั่วไป")]
    public ItemType resourceItemType = ItemType.Material;

    [Tooltip("ItemType ที่ถือว่าเป็น 'Plant' เช่น ผลผลิตที่เก็บเกี่ยวได้ - เลือกให้ตรงกับ enum จริงในโปรเจกต์ของคุณ")]
    public ItemType plantItemType;

    [Header("Currency Settings")]
    public SO_ItemData goldItemData; 

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        GenerateDailyProceduralOrders();
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnDayChanged += OnNewDayStarted;
        }
    }

    void OnDestroy()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnDayChanged -= OnNewDayStarted;
        }
    }

    private void OnNewDayStarted(int newDay)
    {
        GenerateDailyProceduralOrders();
        if (ShopUIController.Instance != null)
        {
            ShopUIController.Instance.RefreshShopUI();
        }
    }

    public void GenerateDailyProceduralOrders()
    {
        activeDailyOrders.Clear();
        Season currentSeason = (DayNightManager.Instance != null) ? DayNightManager.Instance.currentSeason : Season.Spring;
        List<SO_ItemData> activePool = GetItemPoolBySeason(currentSeason);

        if (activePool == null || activePool.Count == 0) return;

        // 🟢 แยกพูลไอเทมของฤดูนี้ตามประเภท เพื่อคุมได้ว่าเควสไหนจะสุ่มของจากประเภทไหน
        List<SO_ItemData> resourcePool = activePool.Where(i => i != null && i.itemType == resourceItemType).ToList();
        List<SO_ItemData> plantPool = activePool.Where(i => i != null && i.itemType == plantItemType).ToList();

        int totalOrders = 5;
        int resourceOrderCount = Mathf.Clamp(guaranteedResourceOrders, 0, totalOrders);
        int randomOrderCount = totalOrders - resourceOrderCount;

        // 1. สร้างเควสบังคับประเภท Resource ล้วนๆ ตามจำนวนที่ตั้งไว้ (ค่าเริ่มต้น 3 ใบ)
        for (int i = 0; i < resourceOrderCount; i++)
        {
            ProceduralOrder order = BuildOrder(resourcePool, currentSeason, i + 1, "Resource");
            if (order != null) activeDailyOrders.Add(order);
        }

        // 2. เควสที่เหลือ (ค่าเริ่มต้น 2 ใบ) สุ่มว่าจะเป็น Resource / Plant / Mixed
        for (int i = 0; i < randomOrderCount; i++)
        {
            int roll = Random.Range(0, 3); // 0 = Resource, 1 = Plant, 2 = Mixed
            List<SO_ItemData> chosenPool;
            string typeLabel;

            switch (roll)
            {
                case 0:
                    chosenPool = resourcePool;
                    typeLabel = "Resource";
                    break;
                case 1:
                    chosenPool = plantPool;
                    typeLabel = "Plant";
                    break;
                default:
                    chosenPool = resourcePool.Concat(plantPool).ToList();
                    typeLabel = "Mixed";
                    break;
            }

            // ถ้าพูลที่สุ่มได้ว่างเปล่า (เช่น plantItemType ยังตั้งไม่ตรง หรือฤดูนี้ไม่มีของประเภทนั้น) fallback ไปใช้ทุกไอเทมในซีซั่นแทนชั่วคราว
            if (chosenPool == null || chosenPool.Count == 0)
            {
                Debug.LogWarning($"[ShopOrderManager] พูลไอเทมประเภท {typeLabel} ว่างเปล่าในฤดู {currentSeason} ใช้ทั้งหมดในซีซั่นแทนชั่วคราว");
                chosenPool = activePool;
            }

            ProceduralOrder order = BuildOrder(chosenPool, currentSeason, resourceOrderCount + i + 1, typeLabel);
            if (order != null) activeDailyOrders.Add(order);
        }
    }

    // 🟢 สร้างออเดอร์ 1 ใบจากพูลไอเทมที่กำหนด (แยกออกมาจาก loop เดิม เพื่อให้ทั้ง Resource/Plant/Mixed เรียกใช้ logic เดียวกันได้)
    private ProceduralOrder BuildOrder(List<SO_ItemData> sourcePool, Season season, int orderNumber, string typeLabel)
    {
        if (sourcePool == null || sourcePool.Count == 0) return null;

        ProceduralOrder newOrder = new ProceduralOrder();
        newOrder.orderTitle = $"Order #{orderNumber} ({season} - {typeLabel})";

        int itemTypesCount = Random.Range(minItemsPerOrder, Mathf.Min(maxItemsPerOrder + 1, sourcePool.Count + 1));
        List<SO_ItemData> tempPool = new List<SO_ItemData>(sourcePool);
        int totalRewardCalc = 0;

        for (int j = 0; j < itemTypesCount; j++)
        {
            int randIndex = Random.Range(0, tempPool.Count);
            SO_ItemData chosenItem = tempPool[randIndex];
            tempPool.RemoveAt(randIndex);

            int amount = Random.Range(minAmountPerItem, maxAmountPerItem + 1);

            OrderRequirement req = new OrderRequirement();
            req.requiredItem = chosenItem;
            req.amount = amount;

            newOrder.requirements.Add(req);
            totalRewardCalc += amount * 15; // ราคาฐาน (เทียบเท่าเกรด B)
        }

        newOrder.rewardGold = totalRewardCalc;
        return newOrder;
    }

    private List<SO_ItemData> GetItemPoolBySeason(Season season)
    {
        switch (season)
        {
            case Season.Spring: return springItems;
            case Season.Summer: return summerItems;
            case Season.Autumn: return autumnItems;
            case Season.Winter: return winterItems;
            default: return springItems;
        }
    }

    // 🌟 [อัปเกรดใหม่]: ส่งมอบออเดอร์พร้อมคิดราคาบวก/ลบตาม "เกรดพืช" (S, A, B, C, D, E, F)
    public bool CompleteOrder(ProceduralOrder targetOrder, ItemGrade itemGrade = ItemGrade.B)
    {
        if (targetOrder == null || !activeDailyOrders.Contains(targetOrder)) return false;

        // 1. เช็คว่าไอเทมในคลังพอส่งไหม
        foreach (var req in targetOrder.requirements)
        {
            if (!ResourceInventory.Instance.HasResource(req.requiredItem.itemName, req.amount))
            {
                Debug.Log($"❌ [Shop]: ของในคลังไม่พอสำหรับส่งออเดอร์นี้");
                return false;
            }
        }

        // 2. หักไอเทมออกจากคลัง
        foreach (var req in targetOrder.requirements)
        {
            ResourceInventory.Instance.ConsumeResource(req.requiredItem, req.amount);
        }

        // 3. 💰 คำนวณโบนัส/หักเงินตามเกรด
        int baseGold = targetOrder.rewardGold; // ราคามาตรฐานป้าย (เกรด B)
        int gradeModifier = 0;

        switch (itemGrade)
        {
            case ItemGrade.S: gradeModifier = 20; break; // +20 ทอง
            case ItemGrade.A: gradeModifier = 10; break; // +10 ทอง
            case ItemGrade.B: gradeModifier = 0;  break; // ราคาตามป้าย
            case ItemGrade.C: gradeModifier = -10; break; // -10 ทอง
            case ItemGrade.D: gradeModifier = -20; break; // -20 ทอง
            case ItemGrade.E: gradeModifier = -30; break; // -30 ทอง
            case ItemGrade.F: gradeModifier = -40; break; // -40 ทอง
        }

        int finalReward = Mathf.Max(5, baseGold + gradeModifier); // การันตีได้เงินอย่างน้อย 5 ทอง

        // 4. มอบทองให้ผู้เล่นจริง
        if (ResourceInventory.Instance != null && goldItemData != null)
        {
            ResourceInventory.Instance.AddResource(goldItemData, finalReward);
        }

        Debug.Log($"✨ [Shop]: ส่งออเดอร์สำเร็จ! เกรดผลผลิต [{itemGrade}] | ได้รับทองรวม: {finalReward} Gold (ฐาน: {baseGold}, ปรับตามเกรด: {gradeModifier})");
        activeDailyOrders.Remove(targetOrder);

        return true;
    }
}