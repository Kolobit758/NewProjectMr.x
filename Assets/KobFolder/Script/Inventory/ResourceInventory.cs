using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceInventory : MonoBehaviour
{
    public static ResourceInventory Instance { get; private set; }
    public event Action OnInventoryChanged;

    [Header("ตั้งค่าขนาดกระเป๋า")]
    public int inventorySize = 24;
    public InventorySlotData[] slots;

    [Header("Item Database Setup (ทางลัดดึงของขากลับ)")]
    // 💡 รวบรวมฐานข้อมูลไอเทมทั้งหมดในเกมของคุณเพื่อกู้คืนเซฟขากลับ
    public List<SO_ItemData> itemDatabase = new List<SO_ItemData>();
    public List<ScriptableObject> itemDatabaseObjects = new List<ScriptableObject>();
    public SO_ItemData testItemData;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (slots == null || slots.Length == 0)
        {
            slots = new InventorySlotData[inventorySize];
            for (int i = 0; i < inventorySize; i++)
            {
                slots[i] = new InventorySlotData();
            }
        }
    }

    private void Start()
    {
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.TryLoadPendingInventory();
        }
        OnInventoryChanged?.Invoke();
    }

    public void AddResource(SO_ItemData item, int amount)
    {
        if (item == null) return;

        bool changed = false;

        // 1. หาช่องเดิมเพื่อรวมจำนวน (Stack)
        for (int i = 0; i < inventorySize; i++)
        {
            if (!slots[i].IsEmpty && slots[i].itemData.itemId == item.itemId)
            {
                slots[i].amount += amount;
                changed = true;
                break;
            }
        }

        // 2. ถ้าไม่มีช่องเดิม ยัดใส่ช่องว่างแรก
        if (!changed)
        {
            for (int i = 0; i < inventorySize; i++)
            {
                if (slots[i].IsEmpty)
                {
                    slots[i].SetItem(item, amount);
                    changed = true;
                    break;
                }
            }
        }

        if (changed)
        {
            OnInventoryChanged?.Invoke();
            if (SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.MarkInventoryDirty();
                SaveLoadManager.Instance.SaveGame();
            }
            return;
        }

        Debug.LogWarning("กระเป๋าเต็มแล้วจ้า!");
    }

    /// <summary>
    /// 🔥 ปรับปรุงใหม่: ฟังก์ชันสลับไอเทมปลอดภัยขั้นสุด รองรับการลากข้าม Canvas ลื่นๆ
    /// </summary>
    public void SwapItems(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= inventorySize || toIndex < 0 || toIndex >= inventorySize) return;

        // 🛡️ ดักจับเซฟตี้: ถ้าลากไอเทมมาปล่อยใส่ช่องพิกัดเดิม/ตัวเอง (ลากข้ามตู้แต่ดัชนีชนกัน)
        // สั่งให้ดีดของกลับที่เดิมทันที ป้องกันข้อมูลในแรมพังกลายเป็นศูนย์ถาวร!
        if (fromIndex == toIndex)
        {
            Debug.Log("[Inventory] 🛡️ พิกัดเดียวกัน: ยกเลิกการสลับค่า เพื่อความปลอดภัยของข้อมูล");
            return;
        }

        // เก็บข้อมูลไส้ในช่องต้นทาง (From) เอาไว้ก่อน
        SO_ItemData fromItem = slots[fromIndex].itemData;
        int fromAmount = slots[fromIndex].amount;

        // ดักลอจิกกรณีลากไอเทมชนิดเดียวกันมาทับกัน -> ให้รวมจำนวน (Stack)
        if (!slots[fromIndex].IsEmpty && !slots[toIndex].IsEmpty && slots[fromIndex].itemData.itemId == slots[toIndex].itemData.itemId)
        {
            slots[toIndex].amount += fromAmount;
            slots[fromIndex].Clear(); // เคลียร์ช่องเดิมทิ้ง
        }
        else
        {
            // 🔄 สลับไส้ในข้อมูล (ลื่นไหล ปลอดภัย ข้อมูลไม่มีวันหาย)
            slots[fromIndex].SetItem(slots[toIndex].itemData, slots[toIndex].amount);
            slots[toIndex].SetItem(fromItem, fromAmount);
        }

        NotifyChanged(); // 🟢 สั่งรีเฟรช UI ทั้งหมดพร้อมกันทันที!

        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.MarkInventoryDirty();
            SaveLoadManager.Instance.SaveGame();
        }
    }

    // 🟢 NEW: แปะฟังก์ชันนี้เพิ่มไว้ใต้ SwapItems เพื่อส่งสัญญาณให้หน้าจอลบรูปไอเทมทันทีหลังจากโยนเสร็จ
    public void NotifyChanged()
    {
        OnInventoryChanged?.Invoke();
    }
    public bool ConsumeResource(SO_ItemData item, int amountNeeded)
    {
        for (int i = 0; i < inventorySize; i++)
        {
            if (!slots[i].IsEmpty && slots[i].itemData.itemId == item.itemId)
            {
                if (slots[i].amount >= amountNeeded)
                {
                    slots[i].amount -= amountNeeded;
                    if (slots[i].amount <= 0) slots[i].Clear();

                    OnInventoryChanged?.Invoke();
                    if (SaveLoadManager.Instance != null)
                    {
                        SaveLoadManager.Instance.MarkInventoryDirty();
                        SaveLoadManager.Instance.SaveGame();
                    }
                    return true;
                }
            }
        }
        return false;
    }

    #region 🟢 ประตูระบายข้อมูลสำหรับเซฟโหลดแบบรวมศูนย์ (Centralized Bridge)

    public List<string> GetSaveItemIds()
    {
        List<string> ids = new List<string>();
        for (int i = 0; i < inventorySize; i++)
        {
            ids.Add(slots[i].IsEmpty ? "" : slots[i].itemData.itemId);
        }
        return ids;
    }

    public List<int> GetSaveItemAmounts()
    {
        List<int> amounts = new List<int>();
        for (int i = 0; i < inventorySize; i++)
        {
            amounts.Add(slots[i].IsEmpty ? 0 : slots[i].amount);
        }
        return amounts;
    }

    public void LoadSavedSlots(List<string> savedIds, List<int> savedAmounts)
    {
        if (savedIds == null || savedAmounts == null) return;

        for (int i = 0; i < inventorySize; i++) slots[i].Clear();

        for (int i = 0; i < Mathf.Min(inventorySize, savedIds.Count); i++)
        {
            if (!string.IsNullOrEmpty(savedIds[i]))
            {
                SO_ItemData foundItem = FindItemDataById(savedIds[i]);
                if (foundItem != null)
                {
                    slots[i].SetItem(foundItem, savedAmounts[i]);
                }
            }
        }
        OnInventoryChanged?.Invoke();
    }

    private SO_ItemData FindItemDataById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (itemDatabase != null)
        {
            for (int i = 0; i < itemDatabase.Count; i++)
            {
                if (itemDatabase[i] != null && itemDatabase[i].itemId == id)
                    return itemDatabase[i];
            }
        }

        if (itemDatabaseObjects != null)
        {
            for (int i = 0; i < itemDatabaseObjects.Count; i++)
            {
                if (itemDatabaseObjects[i] == null) continue;
                var obj = itemDatabaseObjects[i];
                var type = obj.GetType();
                var field = type.GetField("itemId");
                if (field != null)
                {
                    object value = field.GetValue(obj);
                    if (value is string stringValue && stringValue == id)
                    {
                        return obj as SO_ItemData;
                    }
                }
            }
        }

        if (testItemData != null && testItemData.itemId == id) return testItemData;

        SO_ItemData autoFound = Resources.Load<SO_ItemData>($"Items/{id}");
        if (autoFound != null) return autoFound;

        SO_ItemData[] allLoadedItems = Resources.LoadAll<SO_ItemData>("");
        if (allLoadedItems != null)
        {
            for (int i = 0; i < allLoadedItems.Length; i++)
            {
                if (allLoadedItems[i] != null && allLoadedItems[i].itemId == id)
                    return allLoadedItems[i];
            }
        }

#if UNITY_EDITOR
        // 🟢 FIX SUCCESS: เติม FindObjectsSortMode.None ดับขีดแดงตัวแรก
        SO_ItemData[] editorItems = FindObjectsByType<SO_ItemData>(FindObjectsSortMode.None);
        if (editorItems != null)
        {
            for (int i = 0; i < editorItems.Length; i++)
            {
                if (editorItems[i] != null && editorItems[i].itemId == id)
                    return editorItems[i];
            }
        }

        // 🟢 FIX SUCCESS: เติม FindObjectsSortMode.None ดับขีดแดงตัวที่สอง
        ScriptableObject[] allScriptables = FindObjectsByType<ScriptableObject>(FindObjectsSortMode.None);
        if (allScriptables != null)
        {
            for (int i = 0; i < allScriptables.Length; i++)
            {
                var obj = allScriptables[i];
                if (obj == null) continue;
                var type = obj.GetType();
                var field = type.GetField("itemId");
                if (field == null) continue;
                object value = field.GetValue(obj);
                if (value is string stringValue && stringValue == id)
                {
                    var itemData = obj as SO_ItemData;
                    if (itemData != null) return itemData;
                }
            }
        }
#endif

        Debug.LogWarning($"[Inventory Load] ❌ ไม่พบไอเทมด้วย ID '{id}' ในระบบค้นหาจ้า");
        return null;
    }
    #endregion
}