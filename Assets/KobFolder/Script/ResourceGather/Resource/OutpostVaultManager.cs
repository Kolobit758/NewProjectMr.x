using System.Collections.Generic;
using UnityEngine;

public class OutpostVaultManager : MonoBehaviour
{
    public static OutpostVaultManager Instance { get; private set; }

    // 🏛️ คลังเก็บของกลางของ Outpost: [ไอดีไอเทม] -> [จำนวนแร่ที่สะสมไว้ในคลังฐาน]
    private Dictionary<string, int> vaultInventory = new Dictionary<string, int>();

    [Header("ตั้งค่าเวลาส่งทรัพยากร")]
    public float tickInterval = 60f; // ส่งทรัพยากรทุกๆ 60 วินาที (1 นาที)
    private float timer;

    [System.Serializable]
    public class OutpostProduction
    {
        public SO_ItemData item;
        public int amountPerTick; // ความเร็วการขุด เช่น ไม้ +5 ชิ้นต่อนาที
    }

    [Header("รายการทรัพยากรที่ Outpost ทั้งหมดกำลังช่วยกันขุด ณ ตอนนี้")]
    public List<OutpostProduction> activeOutpostRates = new List<OutpostProduction>();


    [Header("TEST ITEM TAKE from outpos")]
    public SO_ItemData itemTake;
    public int amountTake;
    // 🟢 แอดเพิ่มตรงโซน [Header] หรือตรงไหนก็ได้ในคลาส เพื่อแก้บั๊กเรื่องเปิดหาประวัติยึดฐานไม่เจอ
    [Header("Save System Bridge")]
    public List<string> capturedOutpostIDs = new List<string>();

    // 🟢 แอดฟังก์ชันนี้เพิ่มเข้าไป เพื่อให้ระบบรวมศูนย์ดึงแร่สดๆ จาก Dictionary ไปเซฟลงเครื่องได้
    public Dictionary<string, int> GetVaultInventoryData()
    {
        return vaultInventory;
    }

    // 🟢 แอดฟังก์ชันนี้เพิ่มเข้าไป เพื่อให้ระบบรวมศูนย์ยัดค่าแร่ที่โหลดมาคืนตู้ Dictionary
    public void SetVaultInventoryData(Dictionary<string, int> loadedData)
    {
        vaultInventory = loadedData;
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= tickInterval)
        {
            timer = 0f;
            MineResourcesFromOutposts();
        }
    }

    // 🟢 ปรับลอจิกตรงปั๊มทรัพยากรอัตโนมัติใน OutpostVaultManager.cs
    private void MineResourcesFromOutposts()
    {
        if (activeOutpostRates.Count == 0) return;

        bool sharedDataChanged = false; // เอาไว้เช็คว่ามีของเพิ่มจริงไหม
        foreach (var prod in activeOutpostRates)
        {
            if (prod.item == null) continue;

            string id = prod.item.itemId;
            if (vaultInventory.ContainsKey(id)) vaultInventory[id] += prod.amountPerTick;
            else vaultInventory.Add(id, prod.amountPerTick);

            sharedDataChanged = true;
            Debug.Log($"[Outpost Vault] 🏭 คลังฐานได้รับ {prod.item.itemName} +{prod.amountPerTick} | สะสมรวมในคลัง: {vaultInventory[id]}");
        }

        // 🔥 💥 บรรทัดไม้ตาย: ถ้าแร่เด้งปั๊บ สั่งเซฟข้อมูลทุกอย่างลงไฟล์ทันที ป้องกันการปิดเกมหนีแล้วแร่หาย!
        if (sharedDataChanged && SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGame(true);
        }
    }

    // 🟢 ปรับลอจิกตอนกดเบิกของใส่กระเป๋าใน OutpostVaultManager.cs
    public void ClaimToPlayerInventory(SO_ItemData item, int amountToWithdraw)
    {
        if (item == null) return;
        string id = item.itemId;

        if (vaultInventory.ContainsKey(id) && vaultInventory[id] >= amountToWithdraw)
        {
            vaultInventory[id] -= amountToWithdraw;
            ResourceInventory.Instance.AddResource(item, amountToWithdraw);

            Debug.Log($"[Withdraw] 🎒 เบิกของสำเร็จ! ย้าย {item.itemName} x{amountToWithdraw} เข้ากระเป๋าผู้เล่นแล้ว");

            // 🔥 💥 บรรทัดไม้ตาย: เมื่อกดถอนของสำเร็จ ยอดคลังลด ยอดกระเป๋าเพิ่ม สั่งบันทึกไฟล์ทันที!
            if (SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.SaveGame(true);
            }
        }
        else
        {
            Debug.LogWarning("[Withdraw] ทรัพยากรในคลัง Outpost มีไม่พอให้เบิก!");
        }
    }

    [ContextMenu("Get amount")]
    public void GetAmount()
    {
        foreach (var prod in vaultInventory)
        {
            Debug.Log("Resource name : " + prod.Key + "Value : " + prod.Value);

        }
    }

    // เอาไว้ระบุจำนวนทรัพยากรที่ค้างอยู่ใน Outpost ไปโชว์บนหน้าจอ UI
    public int GetVaultResourceCount(SO_ItemData item)
    {
        if (item == null) return 0;
        return vaultInventory.ContainsKey(item.itemId) ? vaultInventory[item.itemId] : 0;
    }

    [ContextMenu("Test take item")]
    public void TestTakeItem()
    {
        ClaimToPlayerInventory(itemTake, amountTake);
    }
}