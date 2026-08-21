using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class OutpostUIController : MonoBehaviour
{
    public static OutpostUIController Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject mainUIPanel; // ตัวหน้าต่าง UI หลักที่มึงจะเปิด/ปิด

    [Header("References Needed (แกะพิมพ์เขียวมาจาก UITeamManager)")]
    public GameObject unitButtonPrefab;       // 🟢 เปลี่ยนมาใช้การ์ดพรีเฟบตัวเดียวกับระบบจัดทีมของมึงเลย!
    public Transform scrollviewContentParent; // จุดที่จะเสกปุ่มรายชื่อสัตว์เลี้ยงออกมา (inventoryContentParent เดิม)

    [Header("Confirmation Box")]
    public GameObject confirmPanel;
    public TMP_Text confirmText;

    private OutpostDataSO currentViewingOutpost; // ค่ายที่ผู้เล่นกำลังเปิดดูอยู่ ณ ตอนนี้
    private UnitInstance selectedUnitForGarrison; // สัตว์เลี้ยงตัวที่มึงกดคลิกเลือกไว้

    private List<GameObject> spawnedButtons = new List<GameObject>(); // ลิสต์ช่วยจำตัวปุ่มเพื่อล้างขยะ


    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        mainUIPanel.SetActive(false);
        confirmPanel.SetActive(false);
    }

    // 🟢 เรียกเปิดหน้าต่าง UI ตอนผู้เล่นเดินมากด Interact ที่ค่าย
    public void OpenPanel(OutpostDataSO outpostData)
    {
        currentViewingOutpost = outpostData;
        mainUIPanel.SetActive(true);

        Debug.Log($"[Outpost UI] ⛺ เริ่มเปิดหน้าต่าง UI ค่าย: {outpostData.outpostName}");
        RefreshUnitList();
    }

    public void ClosePanel()
    {
        mainUIPanel.SetActive(false);
        confirmPanel.SetActive(false);
        currentViewingOutpost = null;
        selectedUnitForGarrison = null;
    }

    // 🔄 วนลูปสร้างการ์ดสัตว์เลี้ยงขึ้น UI ค่ายย่อย
    public void RefreshUnitList()
    {
        Debug.Log("[Outpost UI] 🔄 กำลังดึงข้อมูลสัตว์เลี้ยงลงหน้าจอค่ายย่อย...");

        // 🧹 1. ล้างขยะปุ่มเก่า
        foreach (var btn in spawnedButtons)
        {
            if (btn != null) Destroy(btn);
        }
        spawnedButtons.Clear();

        if (AnimalInventory.Instance == null) return;

        // 🟢 2. วนลูปเสกการ์ดสัตว์เลี้ยง
        foreach (var unit in AnimalInventory.Instance.inventoryUnits)
        {
            if (unit == null) continue;

            // สัตว์เฝ้าค่ายอื่นอยู่หรือติดฟอร์เมชันอยู่? สามารถดักเพิ่มตรงนี้ได้
            if (unit.isInFormation) continue;

            // เสกพรีเฟบการ์ดตัวใหม่ (สำหรับค่ายย่อย) ออกมา
            GameObject newCard = Instantiate(unitButtonPrefab, scrollviewContentParent);
            spawnedButtons.Add(newCard);

            // 🎯 เรียกสคริปต์ตัวใหม่จัดหน้าให้ ดับบั๊กวิ่งไปหน้าจัดทีมทันที!
            OutpostAnimalButtonUI buttonScript = newCard.GetComponent<OutpostAnimalButtonUI>();
            if (buttonScript != null)
            {
                buttonScript.SetupOutpostButton(unit, this);
            }
            else
            {
                Debug.LogError($"🔴 หาสคริปต์ OutpostAnimalButtonUI บนพรีเฟบไม่เจอ!");
            }
        }
        Debug.Log($"[Outpost UI] 🏁 โชว์การ์ดสัตว์เลี้ยงว่างงานสำเร็จทั้งหมด {spawnedButtons.Count} ใบ!");
    }
    // 👆 ฟังก์ชันตอนมึงคลิกเลือกสัตว์เลี้ยงในรายการ
    public void OnSelectUnitToDeploy(UnitInstance unit)
    {
        selectedUnitForGarrison = unit;

        // เปิดกล่อง Pop-up ถามซ้ำเพื่อความชัวร์
        confirmPanel.SetActive(true);
        confirmText.text = $"มึงต้องการส่ง [{unit.customName}] ไปประจำการที่ [{currentViewingOutpost.outpostName}] ใช่หรือไม่มึงกอบ?";
        Debug.Log($"[Outpost UI] 👆 คลิกเลือกการ์ด [{unit.customName}] เปิดหน้าต่างยืนยัน");
    }

    // ✅ ปุ่มกดยืนยัน (Confirm Button ใน UI)
    public void ClickConfirmDeploy()
    {
        if (selectedUnitForGarrison == null || currentViewingOutpost == null) return;

        if (OutpostVaultManager.Instance != null)
        {
            bool success = OutpostVaultManager.Instance.AssignAnimalToOutpost(
                selectedUnitForGarrison.uniqueId,
                currentViewingOutpost.outpostID
            );

            if (success)
            {
                Debug.Log($"🎉 [Outpost UI]: ย้าย {selectedUnitForGarrison.customName} เข้าประจำค่ายย่อยเสร็จสมบูรณ์!");
                ClosePanel();
            }
        }
    }

    // ❌ ปุ่มกดยกเลิกในหน้าต่างป็อปอัพ (Cancel Button)
    public void ClickCancelDeploy()
    {
        confirmPanel.SetActive(false);
        selectedUnitForGarrison = null;
    }
}