using System.Collections.Generic;
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

        for (int i = 0; i < 5; i++)
        {
            ProceduralOrder newOrder = new ProceduralOrder();
            newOrder.orderTitle = $"Order #{i + 1} ({currentSeason})";

            int itemTypesCount = Random.Range(minItemsPerOrder, Mathf.Min(maxItemsPerOrder + 1, activePool.Count + 1));
            List<SO_ItemData> tempPool = new List<SO_ItemData>(activePool);
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
            activeDailyOrders.Add(newOrder);
        }
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