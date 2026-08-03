using System.Collections.Generic;
using UnityEngine;

public class CropPlotsGroup : MonoBehaviour
{
    [Header("แปลงย่อยทั้งหมดในกลุ่มนี้")]
    public List<CropPlots> plots = new List<CropPlots>();

    [Header("ตั้งค่าการดูแล (ใช้ร่วมกันทั้งกลุ่ม)")]
    public bool careEnabledWater = true;
    [Tooltip("รดน้ำกี่รอบต่อ 1 รอบการเติบโตของพืชแต่ละแปลง (override ค่า default ใน SO_PlantData)")]
    public float waterTimesPerCycle = 2f;

    public bool careEnabledFertilize = false;
    [Tooltip("ใส่ปุ๋ยกี่รอบต่อ 1 รอบการเติบโตของพืชแต่ละแปลง (override ค่า default ใน SO_PlantData)")]
    public float fertilizeTimesPerCycle = 1f;
    public SO_ItemData fertilizerItem;



    void Awake()
    {
        if (plots == null) plots = new List<CropPlots>();
        if (plots.Count == 0) plots.AddRange(GetComponentsInChildren<CropPlots>());
    }

    public Vector3 GetCenterPoint()
    {
        if (plots == null || plots.Count == 0) return transform.position;

        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (var p in plots)
        {
            if (p == null) continue;
            sum += p.transform.position;
            count++;
        }
        return count > 0 ? sum / count : transform.position;
    }

    /// <summary>ตั้งค่าความถี่การดูแลให้ทุกแปลงในกลุ่ม</summary>
    public void ApplySettingsToPlots()
    {
        foreach (var plot in plots)
        {
            if (plot == null) continue;

            plot.SetCareFrequencyOverride(careEnabledWater, waterTimesPerCycle, careEnabledFertilize, fertilizeTimesPerCycle);
            
            if (careEnabledFertilize && fertilizerItem != null)
            {
                plot.fertilizerItemData = fertilizerItem;
            }
        }
    }
}