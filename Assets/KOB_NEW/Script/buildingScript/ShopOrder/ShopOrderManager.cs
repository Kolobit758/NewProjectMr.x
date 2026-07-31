using UnityEngine;
using System.Collections.Generic;

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
    [Tooltip("รายชื่อไอเทมที่จะถูกสุ่มมาทำออเดอร์ในแต่ละฤดู (ลาก SO_ItemData มาใส่)")]
    public List<SO_ItemData> springItems = new List<SO_ItemData>();
    public List<SO_ItemData> summerItems = new List<SO_ItemData>();
    public List<SO_ItemData> autumnItems = new List<SO_ItemData>();
    public List<SO_ItemData> winterItems = new List<SO_ItemData>();

    [Header("Active Daily Orders (5 Orders)")]
    public List<ProceduralOrder> activeDailyOrders = new List<ProceduralOrder>();

    [Header("Generation Settings")]
    public int minItemsPerOrder = 1; // จำนวนประเภทไอเทมขั้นต่ำต่อ 1 ออเดอร์
    public int maxItemsPerOrder = 3; // จำนวนประเภทไอเทมสูงสุดต่อ 1 ออเดอร์
    public int minAmountPerItem = 2; // จำนวนชิ้นขั้นต่ำของไอเทมนั้นๆ
    public int maxAmountPerItem = 8; // จำนวนชิ้นสูงสุดของไอเทมนั้นๆ
    [Header("Currency Settings")]
    public SO_ItemData goldItemData; // 💰 ลาก SO_ItemData ของทองคำมาใส่ตรงนี้ใน Inspector

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // สุ่มสร้างออเดอร์ 5 แบบแรกตอนเริ่มเกม
        GenerateDailyProceduralOrders();

        // 🟢 ดักฟัง Event: ทุกครั้งที่ขึ้นวันใหม่ (Day เปลี่ยน) ให้สั่งสุ่มออเดอร์ใหม่ทันที!
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnDayChanged += OnNewDayStarted;
        }
    }

    void OnDestroy()
    {
        // ป้องกัน Memory Leak ถอดการดักฟัง Event ตอน Object ถูกทำลาย
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnDayChanged -= OnNewDayStarted;
        }
    }

    // ฟังก์ชันที่จะถูกเรียกอัตโนมัติทุกๆ เช้าวันใหม่
    private void OnNewDayStarted(int newDay)
    {
        Debug.Log($"🌅 [Shop]: เช้าวันใหม่ Day {newDay} มาถึงแล้ว! ทำการสุ่มออเดอร์ใหม่...");
        GenerateDailyProceduralOrders();

        // ถ้าหน้าต่าง Shop เปิดอยู่ ให้สั่งรีเฟรชหน้าจอ UI ร้านค้าด้วย
        if (ShopUIController.Instance != null)
        {
            ShopUIController.Instance.RefreshShopUI();
        }
    }

    // 🟢 สุ่มสร้างออเดอร์ Procedural 5 แบบใหม่ตามฤดูกาลปัจจุบัน
    public void GenerateDailyProceduralOrders()
    {
        activeDailyOrders.Clear();

        Season currentSeason = (DayNightManager.Instance != null) ? DayNightManager.Instance.currentSeason : Season.Spring;
        List<SO_ItemData> activePool = GetItemPoolBySeason(currentSeason);

        if (activePool == null || activePool.Count == 0)
        {
            Debug.LogWarning($"⚠️ [Shop]: Item pool is empty for season {currentSeason}!");
            return;
        }

        // สุ่มสร้าง 5 ออเดอร์
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
                totalRewardCalc += amount * 15; // คำนวณรางวัลทองตามจำนวนไอเทม
            }

            newOrder.rewardGold = totalRewardCalc;
            activeDailyOrders.Add(newOrder);
        }

        Debug.Log($"🛒 [Shop]: Generated 5 procedural orders for season: <b>{currentSeason}</b>");
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

    // 📦 ฟังก์ชันส่งมอบออเดอร์ร้านค้า
    public bool CompleteOrder(ProceduralOrder targetOrder)
    {
        if (targetOrder == null || !activeDailyOrders.Contains(targetOrder)) return false;

        // 1. เช็คว่าไอเทมในคลังพอส่งไหม
        foreach (var req in targetOrder.requirements)
        {
            if (!ResourceInventory.Instance.HasResource(req.requiredItem.itemName, req.amount))
            {
                Debug.Log($"❌ [Shop]: Not enough items to complete {targetOrder.orderTitle} (Missing {req.requiredItem.itemName})");
                return false;
            }
        }

        // 2. หักไอเทมออกจากคลัง
        foreach (var req in targetOrder.requirements)
        {
            ResourceInventory.Instance.ConsumeResource(req.requiredItem, req.amount);
        }

        // 3. 💰 มอบรางวัลเข้า ResourceInventory จริงๆ!
        if (ResourceInventory.Instance != null && goldItemData != null)
        {
            ResourceInventory.Instance.AddResource(goldItemData, targetOrder.rewardGold);
        }

        Debug.Log($"✨ [Shop]: Order completed successfully! Rewarded {targetOrder.rewardGold} Gold added to inventory.");
        activeDailyOrders.Remove(targetOrder);

        return true;
    }
}