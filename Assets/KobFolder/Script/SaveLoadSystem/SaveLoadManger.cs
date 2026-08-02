using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }
    private string SavePath => Path.Combine(Application.persistentDataPath, "gamesave.sav");
    private bool isLoadingGame;
    private bool inventoryDirty;
    private bool pendingInventoryLoad;
    private List<string> pendingSaveItemIds;
    private List<int> pendingSaveItemAmounts;
    private SaveData pendingSaveData;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 🟢 ใส่ Invoke หรือ Coroutine ให้มันโหลดช้าลงหน่อย ให้ Manager ทุกตัวในฉากตื่นให้ครบก่อน
        Invoke("DelayedLoad", 0.5f);
    }

    void DelayedLoad()
    {
        LoadGame();
        Debug.Log("[Load] 🔄 เริ่มโหลดข้อมูลหลังรอ Manager ตื่นครบ!");
        TryResolvePendingSaveData();
    }

    public void MarkInventoryDirty() { inventoryDirty = true; }

    public void SaveGame(bool forceSave = false)
    {
        if (isLoadingGame) return;
        if (!forceSave && !inventoryDirty) return;

        SaveData data = new SaveData();

        if (File.Exists(SavePath))
        {
            try
            {
                string oldJson = File.ReadAllText(SavePath);
                SaveData oldData = JsonUtility.FromJson<SaveData>(oldJson);
                if (oldData != null) data = oldData;
            }
            catch { }
        }

        // 1. ข้อมูลกระเป๋า
        if (ResourceInventory.Instance != null)
        {
            data.playerInventoryKeys = ResourceInventory.Instance.GetSaveItemIds();
            data.playerInventoryValues = ResourceInventory.Instance.GetSaveItemAmounts();
        }

        // 2. ข้อมูลสัตว์เลี้ยงหลัก
        if (AnimalInventory.Instance != null)
        {
            data.savedAnimalTemplates = AnimalInventory.Instance.GetSaveTemplateIds();
            data.savedAnimalUniqueIds = AnimalInventory.Instance.GetSaveUniqueIds();
            data.savedAnimalCustomNames = AnimalInventory.Instance.GetSaveCustomNames();
            data.savedAnimalHPs = AnimalInventory.Instance.GetSaveCurrentHPs();
        }

        // 3. ข้อมูลคลังส่วยแร่
        if (OutpostVaultManager.Instance != null)
        {
            data.capturedOutpostIDs = new List<string>(OutpostVaultManager.Instance.capturedOutpostIDs);
            Dictionary<string, int> vaultData = OutpostVaultManager.Instance.GetVaultInventoryData();
            data.outpostVaultKeys.Clear();
            data.outpostVaultValues.Clear();
            foreach (var kvp in vaultData)
            {
                data.outpostVaultKeys.Add(kvp.Key);
                data.outpostVaultValues.Add(kvp.Value);
            }
        }

        // 4. สถานะการยึดครองสิ่งก่อสร้างในฉากปัจจุบัน
        OutPosManager[] allOutposts = FindObjectsByType<OutPosManager>(FindObjectsSortMode.None);
        data.outpostStateIds.Clear();
        data.outpostStateOccupied.Clear();
        foreach (var outpost in allOutposts)
        {
            if (outpost != null)
            {
                data.outpostStateIds.Add(outpost.outpostId);
                data.outpostStateOccupied.Add(outpost.isPlayerOccupy);
            }
        }

        // 🟢 5. NEW: บรรจุข้อมูลความหิว + ทหาร UnitInstance ประจำค่ายย่อยลง SaveData ก่อนส่งเข้า JSON
        if (OutpostVaultManager.Instance != null)
        {
            data.outpostHungerLevels.Clear();
            data.garrisonOutpostIDs.Clear();
            data.garrisonTemplateIDs.Clear();
            data.garrisonUniqueIDs.Clear();
            data.garrisonCustomNames.Clear();
            data.garrisonHPs.Clear();

            foreach (var outpost in OutpostVaultManager.Instance.allOutpostsInWorld)
            {
                if (outpost == null) continue;
                data.outpostHungerLevels.Add(outpost.hungerLevel);

                foreach (var unit in outpost.garrisonUnits)
                {
                    if (unit == null || unit.template == null) continue;
                    data.garrisonOutpostIDs.Add(outpost.outpostID);
                    data.garrisonTemplateIDs.Add(unit.template.name);
                    data.garrisonUniqueIDs.Add(unit.uniqueId);
                    data.garrisonCustomNames.Add(unit.customName);
                    data.garrisonHPs.Add(unit.currentHP);
                }
            }
        }

        data.currentMapName = SceneManager.GetActiveScene().name;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        inventoryDirty = false;
        Debug.Log("[Save System] 💾 บันทึกระบบความหิวและทหารประจำการลงไฟล์เซฟถาวรสำเร็จ!");
    }

    public void LoadGame()
    {
        if (!File.Exists(SavePath)) return;

        isLoadingGame = true;
        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (data == null) return;

        pendingSaveData = data;
        TryResolvePendingSaveData();

        // คืนชีพกระเป๋า
        if (data.playerInventoryKeys != null && data.playerInventoryKeys.Count > 0)
        {
            if (ResourceInventory.Instance != null)
            {
                ResourceInventory.Instance.LoadSavedSlots(data.playerInventoryKeys, data.playerInventoryValues);
                KobInventoryUI mainUI = FindAnyObjectByType<KobInventoryUI>();
                if (mainUI != null) mainUI.RefreshGridDisplay();
                KobToolbarUI toolbarUI = FindAnyObjectByType<KobToolbarUI>();
                if (toolbarUI != null) toolbarUI.RefreshToolbarDisplay();
            }
            else
            {
                pendingInventoryLoad = true;
                pendingSaveItemIds = new List<string>(data.playerInventoryKeys);
                pendingSaveItemAmounts = new List<int>(data.playerInventoryValues);
            }
        }

        // คืนชีพสัตว์เลี้ยงในคลังหลัก
        if (AnimalInventory.Instance != null)
        {
            AnimalInventory.Instance.LoadSavedAnimalSlots(data.savedAnimalTemplates, data.savedAnimalUniqueIds, data.savedAnimalCustomNames, data.savedAnimalHPs);
        }

        // คืนชีพคลังกลางระบบเก่า + คืนชีพความหิวและทหารระบบใหม่มึงกอบ
        if (OutpostVaultManager.Instance != null)
        {
            OutpostVaultManager.Instance.capturedOutpostIDs = data.capturedOutpostIDs;
            Dictionary<string, int> vaultData = new Dictionary<string, int>();
            for (int i = 0; i < data.outpostVaultKeys.Count; i++)
            {
                vaultData[data.outpostVaultKeys[i]] = data.outpostVaultValues[i];
            }
            OutpostVaultManager.Instance.SetVaultInventoryData(vaultData);
            OutpostVaultManager.Instance.activeOutpostRates.Clear();

            OutPosManager[] allOutposts = FindObjectsByType<OutPosManager>(FindObjectsSortMode.None);
            foreach (var outpost in allOutposts)
            {
                if (data.capturedOutpostIDs.Contains(outpost.outpostId) && outpost.resourceToProduce != null)
                {
                    OutpostVaultManager.Instance.activeOutpostRates.Add(new OutpostVaultManager.OutpostProduction { item = outpost.resourceToProduce, amountPerTick = outpost.amountPerTick });
                }
            }

            // 🟢 คืนค่าสถานะความหิว + คืนชีพยูนิตทหารเฝ้าค่ายรายตัวยัดกลับเข้าไฟล์ SO
            RestoreOutpostHungerAndGarrison(data);

            // คืนค่าสถานะยึดครองและรีเฟรชธง/สีโมเดลในฉาก
            for (int i = 0; i < data.outpostStateIds.Count; i++)
            {
                string loadedOutpostId = data.outpostStateIds[i];
                bool loadedOccupied = data.outpostStateOccupied[i];
                foreach (var outpost in allOutposts)
                {
                    if (outpost.outpostId == loadedOutpostId)
                    {
                        outpost.isPlayerOccupy = loadedOccupied;
                        outpost.UpdateOutpostVisual();
                        break;
                    }
                }
            }
        }

        inventoryDirty = false;
        isLoadingGame = false;
    }

    // 🟢 ฟังก์ชันแยกเพื่อประมวลผลการเกิดใหม่ของทหารเฝ้าค่ายข้ามมิติ
    public void RestoreOutpostHungerAndGarrison(SaveData data)
    {
        if (OutpostVaultManager.Instance == null || OutpostVaultManager.Instance.allOutpostsInWorld == null)
        {
            Debug.LogError("🔴 Vault Manager ยังไม่พร้อม! อย่าเพิ่งโหลดข้อมูลทหาร");
            return;
        }

        // 1. เคลียร์แค่ค่ายที่มีชื่ออยู่ในเซฟไฟล์เท่านั้น (ป้องกันการล้างค่ายที่เซฟไม่ได้บันทึกไว้)
        foreach (var outpost in OutpostVaultManager.Instance.allOutpostsInWorld)
        {
            if (outpost != null && data.garrisonOutpostIDs.Contains(outpost.outpostID))
            {
                outpost.garrisonUnits.Clear(); // ล้างแค่ค่ายที่จะโหลดใหม่
            }
        }

        // 2. เติมข้อมูลทหารกลับเข้าไปตาม ID
        for (int i = 0; i < data.garrisonOutpostIDs.Count; i++)
        {
            string outpostId = data.garrisonOutpostIDs[i];
            string templateId = data.garrisonTemplateIDs[i];

            OutpostDataSO targetOutpost = OutpostVaultManager.Instance.allOutpostsInWorld
                                          .Find(o => o.outpostID == outpostId);

            if (targetOutpost != null)
            {
                // หา Template จาก Database ของ AnimalInventory (หรือสร้างตัวช่วยหา)
                UnitDataSO template = AnimalInventory.Instance.animalDatabase
                                      .Find(d => d.name == templateId);

                if (template != null)
                {
                    UnitInstance restoredUnit = new UnitInstance(template);
                    restoredUnit.uniqueId = data.garrisonUniqueIDs[i];
                    restoredUnit.customName = data.garrisonCustomNames[i];
                    restoredUnit.currentHP = data.garrisonHPs[i];

                    targetOutpost.garrisonUnits.Add(restoredUnit);
                    Debug.Log($"[Load] กู้คืน {restoredUnit.customName} เข้า {targetOutpost.outpostName}");
                }
            }
        }
    }
    // เพิ่มฟังก์ชันนี้ไว้ใน SaveLoadManager เลยมึง
    private System.Collections.IEnumerator DelaySpawn(OutpostGarrisonVisualizer vis)
    {
        yield return new WaitForSeconds(0.5f); // รอ 0.5 วินาทีให้ดาต้าเข้า SO จนชัวร์
        vis.SpawnGarrisonUnitsInScene();
        Debug.Log($"[SaveLoad] 📡 Visualizer ของค่าย {vis.outpostData.outpostName} วาดหุ่นทหารแล้ว!");
    }

    public void TryLoadPendingInventory()
    {
        if (!pendingInventoryLoad || ResourceInventory.Instance == null) return;
        ResourceInventory.Instance.LoadSavedSlots(pendingSaveItemIds, pendingSaveItemAmounts);
        pendingInventoryLoad = false;
        pendingSaveItemIds = null;
        pendingSaveItemAmounts = null;
        KobInventoryUI ui = FindAnyObjectByType<KobInventoryUI>();
        if (ui != null) ui.RefreshGridDisplay();
    }

    public void TryResolvePendingSaveData()
    {
        if (pendingSaveData == null) return;
        bool needsRetry = false;

        if (pendingSaveData.playerInventoryKeys != null && pendingSaveData.playerInventoryKeys.Count > 0)
        {
            if (ResourceInventory.Instance != null)
            {
                ResourceInventory.Instance.LoadSavedSlots(pendingSaveData.playerInventoryKeys, pendingSaveData.playerInventoryValues);
                KobInventoryUI ui = FindAnyObjectByType<KobInventoryUI>();
                if (ui != null) ui.RefreshGridDisplay();
            }
            else needsRetry = true;
        }

        if ((pendingSaveData.capturedOutpostIDs != null && pendingSaveData.capturedOutpostIDs.Count > 0) || (pendingSaveData.outpostStateIds != null && pendingSaveData.outpostStateIds.Count > 0))
        {
            if (OutpostVaultManager.Instance != null)
            {
                OutpostVaultManager.Instance.capturedOutpostIDs = new List<string>(pendingSaveData.capturedOutpostIDs ?? new List<string>());
                Dictionary<string, int> vaultData = new Dictionary<string, int>();
                for (int i = 0; i < pendingSaveData.outpostVaultKeys.Count; i++)
                {
                    vaultData[pendingSaveData.outpostVaultKeys[i]] = pendingSaveData.outpostVaultValues[i];
                }
                OutpostVaultManager.Instance.SetVaultInventoryData(vaultData);
                OutpostVaultManager.Instance.activeOutpostRates.Clear();

                OutPosManager[] allOutposts = FindObjectsByType<OutPosManager>(FindObjectsSortMode.None);
                foreach (var outpost in allOutposts)
                {
                    if (pendingSaveData.capturedOutpostIDs.Contains(outpost.outpostId) && outpost.resourceToProduce != null)
                    {
                        OutpostVaultManager.Instance.activeOutpostRates.Add(new OutpostVaultManager.OutpostProduction { item = outpost.resourceToProduce, amountPerTick = outpost.amountPerTick });
                    }
                }

                RestoreOutpostHungerAndGarrison(pendingSaveData);

                for (int i = 0; i < pendingSaveData.outpostStateIds.Count; i++)
                {
                    string loadedOutpostId = pendingSaveData.outpostStateIds[i];
                    bool loadedOccupied = pendingSaveData.outpostStateOccupied[i];
                    foreach (var outpost in allOutposts)
                    {
                        if (outpost.outpostId == loadedOutpostId)
                        {
                            outpost.isPlayerOccupy = loadedOccupied;
                            outpost.UpdateOutpostVisual();
                            break;
                        }
                    }
                }
            }
            else needsRetry = true;
        }

        if (pendingSaveData.savedAnimalTemplates != null && pendingSaveData.savedAnimalTemplates.Count > 0)
        {
            if (AnimalInventory.Instance != null)
            {
                AnimalInventory.Instance.LoadSavedAnimalSlots(pendingSaveData.savedAnimalTemplates, pendingSaveData.savedAnimalUniqueIds, pendingSaveData.savedAnimalCustomNames, pendingSaveData.savedAnimalHPs);
            }
            else needsRetry = true;
        }

        if (!needsRetry) pendingSaveData = null;
    }
}