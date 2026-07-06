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
        LoadGame();
        Debug.Log("[Load] 🔄 โหลดข้อมูลเกมจากเครื่องเสร็จ!");
        TryResolvePendingSaveData();
    }

    public void MarkInventoryDirty()
    {
        inventoryDirty = true;
    }

    public void SaveGame(bool forceSave = false)
    {
        if (isLoadingGame) return;
        if (!forceSave && !inventoryDirty) return;

        SaveData data = new SaveData();

        // 🟢 ป้องกันการทับข้อมูลเก่า: โหลดประวัติเดิมขึ้นมารอก่อนข้อมูลจะได้ไม่หาย
        if (File.Exists(SavePath))
        {
            try
            {
                string oldJson = File.ReadAllText(SavePath);
                SaveData oldData = JsonUtility.FromJson<SaveData>(oldJson);
                if (oldData != null)
                {
                    data = oldData;
                }
            }
            catch { }
        }

        // 1. อัปเดตข้อมูลกระเป๋าผู้เล่นล่าสุด
        if (ResourceInventory.Instance != null)
        {
            data.playerInventoryKeys = ResourceInventory.Instance.GetSaveItemIds();
            data.playerInventoryValues = ResourceInventory.Instance.GetSaveItemAmounts();
        }

        // 2. 🔥 อัปเดตสะพานเชื่อมสัตว์เลี้ยงข้ามมิติ ดึงค่าเป็นสตริง/ตัวเลข ป้องกัน ScriptableObject หายตอนปิดเอนจิ้น!
        if (AnimalInventory.Instance != null)
        {
            data.savedAnimalTemplates = AnimalInventory.Instance.GetSaveTemplateIds();
            data.savedAnimalUniqueIds = AnimalInventory.Instance.GetSaveUniqueIds();
            data.savedAnimalCustomNames = AnimalInventory.Instance.GetSaveCustomNames();
            data.savedAnimalHPs = AnimalInventory.Instance.GetSaveCurrentHPs();
        }

        // 3. อัปเดตข้อมูล Outpost ล่าสุด
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

        data.currentMapName = SceneManager.GetActiveScene().name;

        // 💾 บันทึกลงไฟล์เซฟ JSON
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        inventoryDirty = false;

        Debug.Log("[Save System] 💾 ปลอดภัย 100%! บันทึกข้อมูลและซิงค์ระบบสัตว์เลี้ยงสำเร็จ!");
    }

    /// <summary>
    /// 🔄 ฟังก์ชันคืนชีพความคืบหน้ารวมศูนย์
    /// </summary>
    public void LoadGame()
    {
        if (!File.Exists(SavePath)) return;

        isLoadingGame = true;
        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (data == null) return;

        pendingSaveData = data;
        TryResolvePendingSaveData();

        if (data.playerInventoryKeys != null && data.playerInventoryKeys.Count > 0)
        {
            if (ResourceInventory.Instance != null)
            {
                ResourceInventory.Instance.LoadSavedSlots(data.playerInventoryKeys, data.playerInventoryValues);
                Debug.Log("[Load] 🎒 คืนค่าไอเทมลงช่องกระเป๋าตัวละครสำเร็จ!");

                KobInventoryUI ui = FindAnyObjectByType<KobInventoryUI>();
                if (ui != null) ui.RefreshGridDisplay();
            }
            else
            {
                pendingInventoryLoad = true;
                pendingSaveItemIds = new List<string>(data.playerInventoryKeys);
                pendingSaveItemAmounts = new List<int>(data.playerInventoryValues);
                Debug.Log("[Load] ⏳ รอ ResourceInventory พร้อม เพื่อโหลด inventory ต่อไป");
            }
        }

        // 🔥 คืนชีพสัตว์เลี้ยงผ่านฐานข้อมูลกลาง ปิดเปิด Unity ใหม่ ของก็ไม่หาย ID ไม่เพี้ยน!
        if (AnimalInventory.Instance != null)
        {
            AnimalInventory.Instance.LoadSavedAnimalSlots(
                data.savedAnimalTemplates,
                data.savedAnimalUniqueIds,
                data.savedAnimalCustomNames,
                data.savedAnimalHPs
            );
        }

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
            
            // 🟢 FIX SUCCESS: เติม FindObjectsSortMode.None ในวงเล็บ สยบบั๊ก Unity 6.0 ดับขีดแดงตัวแรก!
            OutPosManager[] allOutposts = FindObjectsByType<OutPosManager>(FindObjectsSortMode.None);
            foreach (var outpost in allOutposts)
            {
                if (data.capturedOutpostIDs.Contains(outpost.outpostId) && outpost.resourceToProduce != null)
                {
                    OutpostVaultManager.OutpostProduction prod = new OutpostVaultManager.OutpostProduction();
                    prod.item = outpost.resourceToProduce;
                    prod.amountPerTick = outpost.amountPerTick;
                    OutpostVaultManager.Instance.activeOutpostRates.Add(prod);
                    Debug.Log($"[Load] ✅ Recreated production for outpost: {outpost.outpostId} -> {prod.item.itemName}");
                }
            }

            // 🟢 FIX SUCCESS: เติม FindObjectsSortMode.None ดับขีดแดงตัวที่สอง!
            OutPosManager[] allOutpostsForState = FindObjectsByType<OutPosManager>(FindObjectsSortMode.None);
            for (int i = 0; i < data.outpostStateIds.Count; i++)
            {
                string loadedOutpostId = data.outpostStateIds[i];
                bool loadedOccupied = data.outpostStateOccupied[i];

                foreach (var outpost in allOutpostsForState)
                {
                    if (outpost.outpostId == loadedOutpostId)
                    {
                        outpost.isPlayerOccupy = loadedOccupied;
                        Debug.Log($"[Load] ✅ Restored outpost state: {loadedOutpostId} -> Occupied={loadedOccupied}");
                        break;
                    }
                }
            }
        }

        inventoryDirty = false;
        isLoadingGame = false;
        Debug.Log("[Save System] 🔄 เรียกคืนสถานะและความคืบหน้าของทุกระบบในเกมเรียบร้อย!");
    }

    public void TryLoadPendingInventory()
    {
        if (!pendingInventoryLoad || ResourceInventory.Instance == null) return;

        ResourceInventory.Instance.LoadSavedSlots(pendingSaveItemIds, pendingSaveItemAmounts);
        pendingInventoryLoad = false;
        pendingSaveItemIds = null;
        pendingSaveItemAmounts = null;

        Debug.Log("[Load] 🎒 โหลด inventory ค้างเติ่งกลับเข้ากระเป๋าแล้ว");

        KobInventoryUI ui = FindAnyObjectByType<KobInventoryUI>();
        if (ui != null) ui.RefreshGridDisplay();
    }

    public void TryResolvePendingSaveData()
    {
        if (pendingSaveData == null) return;

        bool needsRetry = false;

        // Inventory
        if (pendingSaveData.playerInventoryKeys != null && pendingSaveData.playerInventoryKeys.Count > 0)
        {
            if (ResourceInventory.Instance != null)
            {
                ResourceInventory.Instance.LoadSavedSlots(pendingSaveData.playerInventoryKeys, pendingSaveData.playerInventoryValues);
                Debug.Log("[Load] 🎒 คืนค่าไอเทมจาก pending save สำเร็จ!");

                KobInventoryUI ui = FindAnyObjectByType<KobInventoryUI>();
                if (ui != null) ui.RefreshGridDisplay();
            }
            else
            {
                needsRetry = true;
            }
        }

        // Outpost state
        if ((pendingSaveData.capturedOutpostIDs != null && pendingSaveData.capturedOutpostIDs.Count > 0)
            || (pendingSaveData.outpostStateIds != null && pendingSaveData.outpostStateIds.Count > 0))
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
                
                // 🟢 FIX SUCCESS: เติม FindObjectsSortMode.None ในวงเล็บ ดับขีดแดงตัวที่สาม!
                OutPosManager[] allOutposts = FindObjectsByType<OutPosManager>(FindObjectsSortMode.None);
                foreach (var outpost in allOutposts)
                {
                    if (pendingSaveData.capturedOutpostIDs.Contains(outpost.outpostId) && outpost.resourceToProduce != null)
                    {
                        OutpostVaultManager.OutpostProduction prod = new OutpostVaultManager.OutpostProduction();
                        prod.item = outpost.resourceToProduce;
                        prod.amountPerTick = outpost.amountPerTick;
                        OutpostVaultManager.Instance.activeOutpostRates.Add(prod);
                        Debug.Log($"[Load] ✅ Recreated production for outpost: {outpost.outpostId} -> {prod.item.itemName}");
                    }
                }

                for (int i = 0; i < pendingSaveData.outpostStateIds.Count; i++)
                {
                    string loadedOutpostId = pendingSaveData.outpostStateIds[i];
                    bool loadedOccupied = pendingSaveData.outpostStateOccupied[i];
                    foreach (var outpost in allOutposts)
                    {
                        if (outpost.outpostId == loadedOutpostId)
                        {
                            outpost.isPlayerOccupy = loadedOccupied;
                            Debug.Log($"[Load] ✅ Restored outpost state: {loadedOutpostId} -> Occupied={loadedOccupied}");
                            break;
                        }
                    }
                }
            }
            else
            {
                needsRetry = true;
            }
        }

        // Animal inventory
        if (pendingSaveData.savedAnimalTemplates != null && pendingSaveData.savedAnimalTemplates.Count > 0)
        {
            if (AnimalInventory.Instance != null)
            {
                // 🔥 เรียกใช้ท่อคืนชีพสัตว์เลี้ยงก้อนประวัติ Database 
                AnimalInventory.Instance.LoadSavedAnimalSlots(
                    pendingSaveData.savedAnimalTemplates,
                    pendingSaveData.savedAnimalUniqueIds,
                    pendingSaveData.savedAnimalCustomNames,
                    pendingSaveData.savedAnimalHPs
                );
                Debug.Log("[Load] 🐺 คืนค่าข้อมูลสัตว์เลี้ยงจาก pending save แล้ว");
            }
            else
            {
                needsRetry = true;
            }
        }

        if (!needsRetry)
        {
            pendingSaveData = null;
            Debug.Log("[Load] ✅ pending save data ถูกประมวลผลครบแล้ว");
        }
    }
}