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
    public TMP_InputField waterIntervalMinutesInput; // เปลี่ยนเป็น TMP_InputField

    [Header("ใส่ปุ๋ย")]
    public Toggle fertilizeToggle;
    public TMP_InputField fertilizeIntervalMinutesInput; // เปลี่ยนเป็น TMP_InputField
    [Tooltip("ลาก DynamicItemButtonSpawner ที่ตั้งค่า items = ปุ๋ยทั้งหมดในเกมไว้แล้ว มาใส่ตรงนี้ " +
             "จะ spawn ปุ่มให้เองครบทุกชนิดตอน Start ไม่ต้องลากปุ่มทีละอันเอง")]
    public DynamicItemButtonSpawner fertilizerButtonSpawner;

    [Header("หาคนงาน")]
    public Button findWorkerButton;
    [Tooltip("ค้นหายูนิตว่างงานในระยะเท่าไหร่ จากจุดกึ่งกลางกลุ่ม (0 = ไม่จำกัด ค้นทั้งแมพ)")]
    public float workerSearchRadius = 0f;

    [Header("แสดงผล (ไม่บังคับ)")]
    public TextMeshProUGUI groupNameLabel; // เปลี่ยนเป็น TextMeshProUGUI
    public TextMeshProUGUI assignedWorkerCountLabel; // เปลี่ยนเป็น TextMeshProUGUI

    private CropPlotsGroup currentGroup;
    private SO_ItemData _selectedFertilizerItem;
    private bool _isUpdatingUIFromCode = false;

    void OnEnable() => RTS_movement.OnFarmPlotClicked += HandleFarmPlotClicked;
    void OnDisable() => RTS_movement.OnFarmPlotClicked -= HandleFarmPlotClicked;

    void Start()
    {
        Debug.Log("Start");

        if (findWorkerButton != null)
        {
            Debug.Log("Assign");

            findWorkerButton.onClick.AddListener(() =>
            {
                Debug.Log("BUTTON CLICK");
            });

            findWorkerButton.onClick.AddListener(OnFindWorkerClicked);
        }
        if (waterToggle != null) waterToggle.onValueChanged.AddListener(_ => PushUIToGroup());
        if (waterIntervalMinutesInput != null) waterIntervalMinutesInput.onEndEdit.AddListener(_ => PushUIToGroup());

        if (fertilizeToggle != null) fertilizeToggle.onValueChanged.AddListener(_ => PushUIToGroup());
        if (fertilizeIntervalMinutesInput != null) fertilizeIntervalMinutesInput.onEndEdit.AddListener(_ => PushUIToGroup());

        if (fertilizerButtonSpawner != null)
        {
            fertilizerButtonSpawner.Spawn(OnFertilizerItemPicked);
        }

        if (findWorkerButton != null) findWorkerButton.onClick.AddListener(OnFindWorkerClicked);

        HidePanel();

        Debug.Log(findWorkerButton.name);
        Debug.Log(findWorkerButton.GetInstanceID());
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
        if (waterIntervalMinutesInput != null)
            waterIntervalMinutesInput.text = SecondsToMinutesText(currentGroup.waterIntervalSeconds);

        if (fertilizeToggle != null) fertilizeToggle.SetIsOnWithoutNotify(currentGroup.careEnabledFertilize);
        if (fertilizeIntervalMinutesInput != null)
            fertilizeIntervalMinutesInput.text = SecondsToMinutesText(currentGroup.fertilizeIntervalSeconds);

        _selectedFertilizerItem = currentGroup.fertilizerItem;

        if (groupNameLabel != null)
            groupNameLabel.text = $"{currentGroup.name} ({currentGroup.plots.Count} แปลง)";

        if (assignedWorkerCountLabel != null)
            assignedWorkerCountLabel.text = $"คนงานดูแลอยู่: {currentGroup.assignedWorkers.Count}";

        _isUpdatingUIFromCode = false;
    }

    /// <summary>เขียนค่าจาก UI กลับไปเก็บถาวรที่ตัว CropPlotsGroup เอง เพื่อให้ "หาคนงาน" ใช้ค่าล่าสุดเสมอ</summary>
    private void PushUIToGroup()
    {
        if (_isUpdatingUIFromCode || currentGroup == null) return;

        currentGroup.careEnabledWater = waterToggle == null || waterToggle.isOn;
        currentGroup.waterIntervalSeconds = ParseMinutesToSeconds(waterIntervalMinutesInput, 5f);

        currentGroup.careEnabledFertilize = fertilizeToggle != null && fertilizeToggle.isOn;
        currentGroup.fertilizeIntervalSeconds = ParseMinutesToSeconds(fertilizeIntervalMinutesInput, 10f);
        currentGroup.fertilizerItem = _selectedFertilizerItem;
    }

    /// <summary>
    /// 🟢 หัวใจของฟีเจอร์: หา unit ว่างงานที่ใกล้ที่สุด 1 ตัว มามอบหมายดูแล "ทุกแปลงในกลุ่มนี้"
    /// </summary>
    private void OnFindWorkerClicked()
    {
        if (currentGroup == null || RTS_movement.instance == null) return;

        Debug.Log("Find worker");

        PushUIToGroup(); // กันเผื่อผู้เล่นพิมพ์ค่าค้าง ยังไม่กด Enter ก็กดหาคนงานเลย

        Vector3 center = currentGroup.GetCenterPoint();
        UnitBase best = null;
        float minDst = Mathf.Infinity;

        foreach (var unit in RTS_movement.instance.allUnits)
        {
            if (unit == null) continue;
            if (unit.currentState != UnitBehavior.Idle) continue;
            if (unit.isCarrying) continue;
            if (unit.isSleepingInShelter) continue;
            if (!unit.canAutoFarm) continue; // ต้องปลดล็อกสกิลนี้ก่อน ไม่งั้น AutoCareTask จะไม่ถูก tick

            float dst = Vector3.Distance(unit.transform.position, center);
            if (workerSearchRadius > 0f && dst > workerSearchRadius) continue;

            if (dst < minDst)
            {
                minDst = dst;
                best = unit;
            }
        }

        if (best == null)
        {
            Debug.Log("⚠️ ไม่พบยูนิตว่างงานที่ปลดล็อกสกิล Auto Farm อยู่แถวนี้ตอนนี้");
            return;
        }

        currentGroup.ApplySettingsToWorker(best);
        PullGroupIntoUI(); // รีเฟรชจำนวนคนงานที่แสดงผล

        Debug.Log($"✅ มอบหมาย {best.name} ให้ดูแล '{currentGroup.name}' ({currentGroup.plots.Count} แปลง) แล้ว");
    }

    private void OnFertilizerItemPicked(SO_ItemData item)
    {
        _selectedFertilizerItem = item;
        Debug.Log("OnFertilizerItemPicked");
        PushUIToGroup();
    }

    private float ParseMinutesToSeconds(TMP_InputField field, float fallbackMinutes)
    {
        float minutes = fallbackMinutes;
        if (field != null && float.TryParse(field.text, out float parsed) && parsed > 0f)
        {
            minutes = parsed;
        }
        return minutes * 60f;
    }

    private string SecondsToMinutesText(float seconds) => (seconds / 60f).ToString("0.##");
}