using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// จับกลุ่มแปลงเกษตรย่อยหลายแปลง (CropPlots ที่เป็นลูกของตัวเอง) ให้ตั้งค่าดูแลรวดเดียวได้
///
/// วิธีใช้:
/// - วาง component นี้ไว้ที่ GameObject แม่ที่มี CropPlots เป็นลูก (ปกติจะ auto-scan ให้เองใน Awake)
/// - ถ้าอยากคุมเองว่ากลุ่มนี้มีแปลงไหนบ้าง ก็ลาก CropPlots ใส่ list "plots" ใน Inspector ได้เลย
/// - ผู้เล่นคลิกซ้ายที่แปลงใดก็ได้ในกลุ่ม (RTS_movement จะไต่หา component นี้จาก parent ให้)
///   แล้วตั้งค่ารดน้ำ/ใส่ปุ๋ยผ่าน FarmPlotPanelUI ซึ่งจะเขียนค่ากลับมาเก็บไว้ในตัวแปรพวกนี้โดยตรง
///
/// หมายเหตุ: ถ้าคลิกโดนแปลง "เดี่ยว" ที่ยังไม่มี component นี้ RTS_movement จะ AddComponent ให้เอง
/// อัตโนมัติแบบกลุ่มขนาด 1 แปลง เพื่อให้ flow เดียวกันใช้ได้ทั้งแปลงเดี่ยวและแปลงกลุ่ม
/// </summary>
public class CropPlotsGroup : MonoBehaviour
{
    [Header("แปลงย่อยทั้งหมดในกลุ่มนี้")]
    [Tooltip("ถ้าปล่อยว่างไว้ ระบบจะสแกนหา CropPlots ทุกตัวที่เป็นลูกของ object นี้ให้เองตอน Awake")]
    public List<CropPlots> plots = new List<CropPlots>();

    [Header("ตั้งค่าการดูแล (ใช้ร่วมกันทั้งกลุ่ม)")]
    public bool careEnabledWater = true;
    public float waterIntervalSeconds = 300f; // ค่าเริ่มต้น 5 นาที

    public bool careEnabledFertilize = false;
    public float fertilizeIntervalSeconds = 600f; // ค่าเริ่มต้น 10 นาที
    public SO_ItemData fertilizerItem;

    [Header("คนงานที่รับผิดชอบกลุ่มนี้อยู่ตอนนี้")]
    public List<UnitBase> assignedWorkers = new List<UnitBase>();

    void Awake()
    {
        if (plots == null) plots = new List<CropPlots>();

        if (plots.Count == 0)
        {
            plots.AddRange(GetComponentsInChildren<CropPlots>());
        }
    }

    /// <summary>จุดกึ่งกลางของกลุ่ม ใช้หาคนงานที่ใกล้ที่สุด</summary>
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

    /// <summary>
    /// 🟢 มอบหมายให้ unit ตัวนี้ดูแล "ทุกแปลง" ในกลุ่ม ตามการตั้งค่าปัจจุบัน
    /// ใช้ระบบ AutoCareTask เดิมของ UnitBase (แค่ยิงคำสั่งซ้ำทีละแปลง) ไม่ต้องแก้ UnitBase เลย
    /// </summary>
    public void ApplySettingsToWorker(UnitBase worker)
    {
        if (worker == null) return;

        foreach (var plot in plots)
        {
            if (plot == null) continue;

            if (careEnabledWater)
                worker.AssignAutoWaterTask(plot, waterIntervalSeconds);

            if (careEnabledFertilize && fertilizerItem != null)
                worker.AssignAutoFertilizeTask(plot, fertilizeIntervalSeconds, fertilizerItem);

            // 🔒 แปลงมีคนงานประจำแล้ว ปิดระบบ auto-call สุ่มเรียกยูนิตว่างงานตัวอื่นของ CropPlots เอง
            plot.hasDedicatedWorker = true;
        }

        if (!assignedWorkers.Contains(worker))
            assignedWorkers.Add(worker);
    }

    /// <summary>เผื่ออยากทำปุ่ม "เลิกจ้าง" ในอนาคต — ถอนหน้าที่ดูแลทุกแปลงในกลุ่มออกจาก worker ตัวนี้</summary>
    public void RemoveWorker(UnitBase worker)
    {
        if (worker == null) return;

        foreach (var plot in plots)
        {
            if (plot != null) worker.RemoveAutoCareTask(plot);
        }

        assignedWorkers.Remove(worker);

        // 🔓 ถ้าไม่เหลือคนงานประจำแล้ว เปิด auto-call ของแปลงกลับมาให้เรียกคนช่วยเองได้เหมือนเดิม
        if (assignedWorkers.Count == 0)
        {
            foreach (var plot in plots)
            {
                if (plot != null) plot.hasDedicatedWorker = false;
            }
        }
    }
}