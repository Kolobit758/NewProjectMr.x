using System.Collections.Generic;
using UnityEngine;

public class OutpostVaultManager : MonoBehaviour
{
    public static OutpostVaultManager Instance { get; private set; }

    private Dictionary<string, int> vaultInventory = new Dictionary<string, int>();

    [Header("ตั้งค่าเวลาส่งทรัพยากร")]
    public float tickInterval = 60f; // ส่งทรัพยากรทุกๆ 60 วินาที
    private float timer;

    // 🟢 [FIX SUCCESS]: คืนชีพคลาสย่อยเดิมของมึง เพื่อสยบบั๊ก CS0426 ในสคริปต์ SaveLoad!
    [System.Serializable]
    public class OutpostProduction
    {
        public SO_ItemData item;
        public int amountPerTick;
    }

    [Header("รายการทรัพยากรที่ Outpost ทั้งหมดกำลังช่วยกันขุด ณ ตอนนี้")]
    public List<OutpostProduction> activeOutpostRates = new List<OutpostProduction>();

    // 🟢 NEW ARCHITECTURE: ถังเก็บรวมไฟล์ SO ค่ายย่อยทั้งหมดในเกม
    [Header("Global Outposts Data Base")]
    public List<OutpostDataSO> allOutpostsInWorld = new List<OutpostDataSO>();

    [Header("Save System Bridge")]
    public List<string> capturedOutpostIDs = new List<string>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // 🍖 1. บริหารความหิวยูนิตเฝ้าค่ายของไฟล์ SO ข้ามซีนตลอดเวลา
        foreach (var outpost in allOutpostsInWorld)
        {
            if (outpost != null && outpost.isCaptured)
            {
                if (outpost.hungerLevel > 0)
                {
                    outpost.hungerLevel -= outpost.hungerDecreaseRate * Time.deltaTime;
                    if (outpost.hungerLevel < 0) outpost.hungerLevel = 0;
                }
            }
        }

        // 🏭 2. ลูปจับเวลาผลิตแร่ส่งส่วย
        timer += Time.deltaTime;
        if (timer >= tickInterval)
        {
            timer = 0f;
            MineResourcesFromOutposts();
        }
    }

    private void MineResourcesFromOutposts()
    {
        bool sharedDataChanged = false;

        foreach (var outpost in allOutpostsInWorld)
        {
            if (outpost == null || !outpost.isCaptured || outpost.resourceToProduce == null) continue;
            if (outpost.hungerLevel <= 0 && outpost.garrisonUnits.Count == 0) continue;

            string id = outpost.resourceToProduce.itemId;
            int productionAmount = outpost.amountPerTick;

            // ถ้ายูนิตอดอยากจนหิวโซ พลังขุดลดเหลือครึ่งเดียว
            if (outpost.hungerLevel <= 0) productionAmount /= 2;

            if (vaultInventory.ContainsKey(id)) vaultInventory[id] += productionAmount;
            else vaultInventory.Add(id, productionAmount);

            sharedDataChanged = true;
            Debug.Log($"[Vault] 🏭 คลังฐานได้รับ {outpost.resourceToProduce.itemName} +{productionAmount} จาก {outpost.outpostName}");
        }

        if (sharedDataChanged && SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGame(true);
        }
    }

    // 🥗 ฟังก์ชันส่งอาหารป้อนยูนิตข้ามซีน (เรียกใช้จาก UI ซีนฟาร์ม)
    public void FeedOutpostByID(string id, float foodAmount, float atkBuff, float defBuff)
    {
        OutpostDataSO outpost = allOutpostsInWorld.Find(o => o.outpostID == id);
        if (outpost != null)
        {
            outpost.FeedOutpost(foodAmount, atkBuff, defBuff);
            if (SaveLoadManager.Instance != null) SaveLoadManager.Instance.SaveGame(true);
        }
    }

    public void ClaimToPlayerInventory(SO_ItemData item, int amountToWithdraw)
    {
        if (item == null) return;
        string id = item.itemId;

        if (vaultInventory.ContainsKey(id) && vaultInventory[id] >= amountToWithdraw)
        {
            vaultInventory[id] -= amountToWithdraw;
            ResourceInventory.Instance.AddResource(item, amountToWithdraw);

            if (SaveLoadManager.Instance != null) SaveLoadManager.Instance.SaveGame(true);
        }
    }

    public Dictionary<string, int> GetVaultInventoryData() => vaultInventory;
    public void SetVaultInventoryData(Dictionary<string, int> loadedData) => vaultInventory = loadedData;
    public int GetVaultResourceCount(SO_ItemData item) => (item != null && vaultInventory.ContainsKey(item.itemId)) ? vaultInventory[item.itemId] : 0;

    #region Set Camp
    /// <summary>
    /// ⚔️ ฟังก์ชันมหาเทพ: ย้ายสัตว์เลี้ยงออกจากคลังสำรอง (AnimalInventory) ไปบรรจุเฝ้าค่ายย่อย (OutpostDataSO)
    /// </summary>
    /// <param name="unitUniqueId">รหัส ID เฉพาะตัวของสัตว์เลี้ยงตัวที่จะส่งไป</param>
    /// <param name="targetOutpostId">รหัสไอดีค่ายปลายทาง (เช่น "North_01")</param>
    public bool AssignAnimalToOutpost(string unitUniqueId, string targetOutpostId)
    {
        // 1. ตรวจสอบความพร้อมของคลังสัตว์เลี้ยงกลาง
        if (AnimalInventory.Instance == null) return false;

        // 2. ค้นหาค่ายเป้าหมายจาก Database SO ทั่วโลกที่เรามี
        OutpostDataSO targetOutpost = allOutpostsInWorld.Find(o => o.outpostID == targetOutpostId);
        if (targetOutpost == null || !targetOutpost.isCaptured)
        {
            Debug.LogWarning($"[Outpost System] ❌ ไม่สามารถส่งทหารไปได้ เนื่องจากหาค่ายไอดี {targetOutpostId} ไม่พบ หรือค่ายยังไม่ถูกยึด!");
            return false;
        }

        // 3. ควานหาตัว `UnitInstance` ในกระเป๋าสัตว์เลี้ยงของผู้เล่น
        UnitInstance unitToDeploy = AnimalInventory.Instance.inventoryUnits.Find(u => u != null && u.uniqueId == unitUniqueId);
        if (unitToDeploy == null)
        {
            Debug.LogWarning($"[Outpost System] ❌ หาสัตว์เลี้ยงรหัส {unitUniqueId} ในคลังหลักไม่เจอเพื่อน!");
            return false;
        }

        // 🟢 [ดักเคสพิเศษ]: ถ้าสัตว์ตัวนี้ถูกจัดลงทีมจริง (Active Formation) อยู่ ให้ถอดออกจากขบวนทัพก่อน เพื่อป้องกันบั๊กทหารเดินทับไลน์!
        if (TeamFormationManager.Instance != null && unitToDeploy.isInFormation)
        {
            TeamFormationManager.Instance.RemoveUnitFromFormationById(unitUniqueId);
            Debug.Log($"[Outpost System] 🔄 ถอด {unitToDeploy.customName} ออกจากขบวนทัพหลักเพื่อเตรียมย้ายไปเฝ้าฐาน");
        }

        // 4. ทำการย้ายดาต้า: ถอดออกจากกระเป๋าผู้เล่น ➡️ ยัดเข้าลิสต์ทหารเฝ้าค่ายใน SO
        if (AnimalInventory.Instance.RemoveUnitFromInventory(unitUniqueId))
        {
            targetOutpost.garrisonUnits.Add(unitToDeploy);

            Debug.Log($"[Outpost System] 💂 บรรจุยูนิต [{unitToDeploy.customName}] เข้าประจำการที่ [{targetOutpost.outpostName}] สำเร็จ!");

            // 💾 5. สั่งรีเฟรชหุ่นโมเดลทหารยืนในฉากทันที (ถ้าผู้เล่นยืนอยู่ในซีนสำรวจนั้นอยู่)
            OutpostGarrisonVisualizer visualizer = FindAnyObjectByType<OutpostGarrisonVisualizer>();
            if (visualizer != null && visualizer.outpostData == targetOutpost)
            {
                visualizer.SpawnGarrisonUnitsInScene();
            }

            // 💾 6. สั่งบันทึกไฟล์เซฟหลักลงเครื่องทันที ทหารจะได้อยู่ถาวรไม่หายตอนเปลี่ยนซีน
            if (SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.SaveGame(true);
            }

            return true;
        }

        return false;
    }
    /// <summary>
    /// 🔄 ฟังก์ชันเรียกทหารคืนทัพ: ถอนกำลังพลจากค่ายย่อย กลับเข้าสู่กระเป๋าสำรองหลักของผู้เล่น
    /// </summary>
    public bool RecallAnimalFromOutpost(string unitUniqueId, string sourceOutpostId)
    {
        OutpostDataSO sourceOutpost = allOutpostsInWorld.Find(o => o.outpostID == sourceOutpostId);
        if (sourceOutpost == null) return false;

        UnitInstance unitToRecall = sourceOutpost.garrisonUnits.Find(u => u != null && u.uniqueId == unitUniqueId);
        if (unitToRecall == null) return false;

        // ถอนออกจาก SO ค่าย ➡️ ยัดกลับเข้าคลัง AnimalInventory
        sourceOutpost.garrisonUnits.Remove(unitToRecall);
        AnimalInventory.Instance.AddUnitToInventory(unitToRecall);

        Debug.Log($"[Outpost System] 🔄 เรียกทหาร [{unitToRecall.customName}] คืนสู่คลังหลักสำเร็จ!");

        // รีเฟรชโมเดลในฉาก + เซฟเกม
        OutpostGarrisonVisualizer visualizer = FindAnyObjectByType<OutpostGarrisonVisualizer>();
        if (visualizer != null && visualizer.outpostData == sourceOutpost)
        {
            visualizer.SpawnGarrisonUnitsInScene();
        }

        if (SaveLoadManager.Instance != null) SaveLoadManager.Instance.SaveGame(true);

        return true;
    }
    #endregion
}