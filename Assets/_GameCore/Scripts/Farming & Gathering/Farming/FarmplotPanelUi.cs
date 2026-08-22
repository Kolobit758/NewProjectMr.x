using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // เพิ่ม namespace ของ TextMeshPro

/// <summary>
/// แผงตั้งค่าการดูแลแปลงเกษตร แบบ "คลิกแปลงก่อน แล้วค่อยตั้งค่า" (เวอร์ชัน TextMeshPro)
/// </summary>
public class FarmPlotPanelUI : MonoBehaviour
{
    [Header("Panel Root")]
    public GameObject panelRoot;

    [Header("รดน้ำ")]
    public Toggle waterToggle;
    [Tooltip("จำนวนรอบที่ต้องการรดน้ำ ต่อ 1 รอบการเติบโตของพืช (เช่น 2 = รดน้ำ 2 ครั้งกว่าจะโตเต็มที่)")]
    public TMP_InputField waterTimesPerCycleInput;

    [Header("ใส่ปุ๋ย")]
    public Toggle fertilizeToggle;
    [Tooltip("จำนวนรอบที่ต้องการใส่ปุ๋ย ต่อ 1 รอบการเติบโตของพืช")]
    public TMP_InputField fertilizeTimesPerCycleInput;
    [Tooltip("ลาก DynamicItemButtonSpawner ที่ตั้งค่า items = ปุ๋ยทั้งหมดในเกมไว้แล้ว มาใส่ตรงนี้ " +
             "จะ spawn ปุ่มให้เองครบทุกชนิดตอน Start ไม่ต้องลากปุ่มทีละอันเอง")]
    public DynamicItemButtonSpawner fertilizerButtonSpawner;

    [Header("หาคนงาน")]
    public Button findWorkerButton;
    [Tooltip("ค้นหายูนิตว่างงานในระยะเท่าไหร่ จากจุดกึ่งกลางกลุ่ม (0 = ไม่จำกัด ค้นทั้งแมพ)")]
    public float workerSearchRadius = 0f;

    [Header("แสดงผล (ไม่บังคับ)")]
    public TextMeshProUGUI groupNameLabel;
    public TextMeshProUGUI assignedWorkerCountLabel;

    private CropPlotsGroup currentGroup;
    private SO_ItemData _selectedFertilizerItem;
    private bool _isUpdatingUIFromCode = false;

    void OnEnable() => RTS_movement.OnFarmPlotClicked += HandleFarmPlotClicked;
    void OnDisable() => RTS_movement.OnFarmPlotClicked -= HandleFarmPlotClicked;

    void Start()
    {
        if (waterToggle != null) waterToggle.onValueChanged.AddListener(_ => PushUIToGroup());
        if (waterTimesPerCycleInput != null) waterTimesPerCycleInput.onEndEdit.AddListener(_ => PushUIToGroup());

        if (fertilizeToggle != null) fertilizeToggle.onValueChanged.AddListener(_ => PushUIToGroup());
        if (fertilizeTimesPerCycleInput != null) fertilizeTimesPerCycleInput.onEndEdit.AddListener(_ => PushUIToGroup());

        if (fertilizerButtonSpawner != null)
        {
            fertilizerButtonSpawner.Spawn(OnFertilizerItemPicked);
        }

        if (findWorkerButton != null)
        {
            findWorkerButton.onClick.AddListener(OnFindWorkerClicked);
        }

        HidePanel();
    }

    private void HandleFarmPlotClicked(CropPlotsGroup group)
    {
        currentGroup = group;

        if (currentGroup == null)
        {
            HidePanel();
            return;
        }

        ShowPanel();
        PullGroupIntoUI();
    }

    private void ShowPanel() { if (panelRoot != null) panelRoot.SetActive(true); }
    private void HidePanel() { if (panelRoot != null) panelRoot.SetActive(false); }

    /// <summary>อ่านค่าปัจจุบันที่เก็บไว้ในกลุ่มที่คลิก มาโชว์ในแผง (ไม่ทำให้ค่าที่เคยตั้งไว้หาย)</summary>
    private void PullGroupIntoUI()
    {
        if (currentGroup == null) return;
        _isUpdatingUIFromCode = true;

        if (waterToggle != null) waterToggle.SetIsOnWithoutNotify(currentGroup.careEnabledWater);
        if (waterTimesPerCycleInput != null)
            waterTimesPerCycleInput.text = currentGroup.waterTimesPerCycle.ToString("0.##");

        if (fertilizeToggle != null) fertilizeToggle.SetIsOnWithoutNotify(currentGroup.careEnabledFertilize);
        if (fertilizeTimesPerCycleInput != null)
            fertilizeTimesPerCycleInput.text = currentGroup.fertilizeTimesPerCycle.ToString("0.##");

        _selectedFertilizerItem = currentGroup.fertilizerItem;

        if (groupNameLabel != null)
            groupNameLabel.text = $"{currentGroup.name} ({currentGroup.plots.Count} แปลง)";

        if (assignedWorkerCountLabel != null)
            assignedWorkerCountLabel.gameObject.SetActive(false); // ซ่อนไปเลยเพราะไม่มีคนงานแล้ว

        _isUpdatingUIFromCode = false;
    }

    /// <summary>เขียนค่าจาก UI กลับไปเก็บถาวรที่ตัว CropPlotsGroup เอง เพื่อให้ "หาคนงาน" ใช้ค่าล่าสุดเสมอ</summary>
    private void PushUIToGroup()
    {
        if (_isUpdatingUIFromCode || currentGroup == null) return;

        currentGroup.careEnabledWater = waterToggle == null || waterToggle.isOn;
        currentGroup.waterTimesPerCycle = ParseTimesPerCycle(waterTimesPerCycleInput, 2f);

        currentGroup.careEnabledFertilize = fertilizeToggle != null && fertilizeToggle.isOn;
        currentGroup.fertilizeTimesPerCycle = ParseTimesPerCycle(fertilizeTimesPerCycleInput, 1f);
        currentGroup.fertilizerItem = _selectedFertilizerItem;
    }

    private float ParseTimesPerCycle(TMP_InputField field, float fallback)
    {
        if (field != null && float.TryParse(field.text, out float parsed) && parsed > 0f)
            return parsed;
        return fallback;
    }

    /// <summary>
    /// บันทึกการตั้งค่าลงแปลง
    /// </summary>
    private void OnFindWorkerClicked()
    {
        if (currentGroup == null) return;

        PushUIToGroup(); // กันเผื่อผู้เล่นพิมพ์ค่าค้าง ยังไม่กด Enter ก็กดหาคนงานเลย

        currentGroup.ApplySettingsToPlots();
        PullGroupIntoUI(); // รีเฟรชหน้าต่าง

        Debug.Log($"✅ บันทึกการตั้งค่า '{currentGroup.name}' ({currentGroup.plots.Count} แปลง) แล้ว");
    }

    private void OnFertilizerItemPicked(SO_ItemData item)
    {
        _selectedFertilizerItem = item;
        PushUIToGroup();
    }
}