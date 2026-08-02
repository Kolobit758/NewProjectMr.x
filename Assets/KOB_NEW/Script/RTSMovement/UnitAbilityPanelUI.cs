using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// แผงสกิลแบบ StarCraft (เฉพาะฝั่ง Auto Gather)
///
/// กดปุ่มหลัก -> เปิดแถบเลือก "จะเก็บทรัพยากรอะไร" -> เลือกแล้ว unit ที่เลือกอยู่
/// จะวิ่งไปหา ResourceNodeBase ที่ผลิตของชนิดนั้นที่ใกล้ตัวเองที่สุดทันที (ไม่ต้องคลิกจุดในโลก)
///
/// ฝั่ง Auto Farm/Auto Care ย้ายไปเป็นคนละ flow แล้ว ดู FarmPlotPanelUI.cs
/// (คลิกแปลงก่อน -> ตั้งค่า -> กดหาคนงาน แทนที่จะเลือก unit ก่อน)
/// </summary>
public class UnitAbilityPanelUI : MonoBehaviour
{
    [Header("Panel Root (Panel ทั้งก้อนที่จะโชว์/ซ่อนตอนมี unit ถูกเลือก)")]
    public GameObject panelRoot;

    // ---------------- Auto Gather ----------------
    [Header("Auto Gather")]
    [Tooltip("ปุ่มหลัก กดแล้วเปิด/ปิดแถบเลือกชนิดทรัพยากร")]
    public Button autoGatherMainButton;
    [Tooltip("GameObject ของแถบปุ่มเลือกชนิดทรัพยากร (ซ่อนไว้ก่อน จะโชว์ตอนกด autoGatherMainButton)")]
    public GameObject gatherResourceSubPanel;
    [Tooltip("ลาก DynamicItemButtonSpawner ที่ตั้งค่า items = ทรัพยากรทั้งหมดในเกมไว้แล้ว มาใส่ตรงนี้ " +
             "จะ spawn ปุ่มให้เองครบทุกชนิดตอน Start ไม่ต้องลากปุ่มทีละอันเอง")]
    public DynamicItemButtonSpawner gatherResourceButtonSpawner;
    [Tooltip("ปุ่ม/แถบนี้จะกดได้ต่อเมื่อวิจัยปลดล็อกสกิล Auto Gather แล้วเท่านั้น")]
    public bool lockAutoGatherUntilResearched = true;

    // ---------------- Selection Info ----------------
    // หมายเหตุ: ส่วน Auto Farm/Auto Care ย้ายไปอยู่ที่ FarmPlotPanelUI.cs แล้ว
    // (flow ใหม่: คลิกแปลงก่อน -> ตั้งค่า -> กดหาคนงาน แทนที่จะเลือก unit ก่อนแล้วค่อยคลิกแปลง)
    [Header("แสดงผล (ไม่บังคับ)")]
    public Text selectionCountLabel;

    private List<UnitBase> currentSelection = new List<UnitBase>();

    void OnEnable() => RTS_movement.OnSelectionChanged += HandleSelectionChanged;
    void OnDisable() => RTS_movement.OnSelectionChanged -= HandleSelectionChanged;

    void Start()
    {
        if (autoGatherMainButton != null)
            autoGatherMainButton.onClick.AddListener(ToggleGatherSubPanel);

        if (gatherResourceButtonSpawner != null)
        {
            gatherResourceButtonSpawner.Spawn(StartAutoGatherForSelected);
        }

        if (gatherResourceSubPanel != null) gatherResourceSubPanel.SetActive(false);

        HidePanel();
    }

    private void HandleSelectionChanged(List<UnitBase> selectedUnits)
    {
        currentSelection = selectedUnits ?? new List<UnitBase>();

        if (currentSelection.Count == 0)
        {
            HidePanel();
            if (gatherResourceSubPanel != null) gatherResourceSubPanel.SetActive(false);
            return;
        }

        ShowPanel();
        RefreshUIFromSelection();
    }

    private void ShowPanel() { if (panelRoot != null) panelRoot.SetActive(true); }
    private void HidePanel() { if (panelRoot != null) panelRoot.SetActive(false); }

    private void RefreshUIFromSelection()
    {
        if (currentSelection.Count == 0) return;

        bool autoGatherUnlocked = !lockAutoGatherUntilResearched
            || (BuildingUnlockManager.Instance != null && BuildingUnlockManager.Instance.isAutoGatherUnlocked);

        if (autoGatherMainButton != null)
            autoGatherMainButton.interactable = autoGatherUnlocked;

        if (gatherResourceButtonSpawner != null)
            gatherResourceButtonSpawner.SetInteractable(autoGatherUnlocked);

        if (selectionCountLabel != null)
        {
            selectionCountLabel.text = currentSelection.Count == 1
                ? currentSelection[0].name
                : $"{currentSelection.Count} units selected";
        }
    }

    // ==================== Auto Gather ====================

    private void ToggleGatherSubPanel()
    {
        if (gatherResourceSubPanel == null) return;
        gatherResourceSubPanel.SetActive(!gatherResourceSubPanel.activeSelf);
    }

    private void CloseGatherSubPanel()
    {
        if (gatherResourceSubPanel != null) gatherResourceSubPanel.SetActive(false);
    }

    /// <summary>
    /// 🟢 หัวใจของ Auto Gather: เลือกชนิดทรัพยากรแล้ว ให้ทุก unit ที่เลือกอยู่
    /// วิ่งไปหา ResourceNodeBase ชนิดนั้นที่ใกล้ตัวเองที่สุดในฉาก แล้วเริ่มเก็บทันที
    /// (การเก็บซ้ำอัตโนมัติหลังจากนี้ อาศัย canAutoGather ที่ปลดล็อกไว้แล้วจาก UnitBase เดิม)
    /// </summary>
    private void StartAutoGatherForSelected(SO_ItemData resourceType)
    {
        if (resourceType == null || currentSelection.Count == 0) return;

        ResourceNodeBase[] allNodes = FindObjectsByType<ResourceNodeBase>(FindObjectsSortMode.None);

        foreach (var unit in currentSelection)
        {
            if (unit == null) continue;

            ResourceNodeBase nearest = null;
            float minDst = Mathf.Infinity;

            foreach (var node in allNodes)
            {
                if (node == null || !node.canGathering) continue;
                if (node.resourceToProduce != resourceType) continue;

                float dst = Vector3.Distance(unit.transform.position, node.transform.position);
                if (dst < minDst)
                {
                    minDst = dst;
                    nearest = node;
                }
            }

            if (nearest != null)
            {
                unit.CommandGather(nearest);
            }
            else
            {
                Debug.Log($"⚠️ ไม่พบทรัพยากร '{resourceType.itemName}' ที่เก็บได้อยู่ในฉากตอนนี้ สำหรับ {unit.name}");
            }
        }

        CloseGatherSubPanel();
    }
}