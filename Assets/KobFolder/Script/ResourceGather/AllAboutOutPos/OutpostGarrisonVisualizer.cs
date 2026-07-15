using UnityEngine;
using System.Collections.Generic;

public class OutpostGarrisonVisualizer : MonoBehaviour
{
    [Header("Outpost Data Link")]
    public OutpostDataSO outpostData;

    [Header("Spawn Layout")]
    public Transform[] spawnPoints;

    public List<GameObject> spawnedVisualUnits = new List<GameObject>();

    private void Start()
    {
        Invoke("SpawnGarrisonUnitsInScene", 1.0f);
    }

    public void SpawnGarrisonUnitsInScene()
    {
        // 1. เคลียร์ของเก่า
        foreach (var go in spawnedVisualUnits)
        {
            if (go != null) Destroy(go);
        }
        spawnedVisualUnits.Clear();

        if (outpostData == null || !outpostData.isCaptured) return;

        for (int i = 0; i < outpostData.garrisonUnits.Count; i++)
        {
            if (i >= spawnPoints.Length) break;

            UnitInstance unit = outpostData.garrisonUnits[i];

            if (unit != null && unit.template != null && unit.template.unitPrefab != null)
            {
                GameObject visualUnit = Instantiate(
                    unit.template.unitPrefab,
                    spawnPoints[i].position,
                    spawnPoints[i].rotation,
                    transform
                );

                // --- 🟢 ขั้นตอนการ Setup ทหารเฝ้าฐาน ---

                // 1. ปิดระบบ AI เดิมทิ้ง (รวมไว้ในจุดเดียว ไม่ต้องเรียกซ้ำ)
                var ai = visualUnit.GetComponent<AnimalAIController>();
                if (ai != null) ai.enabled = false;

                var formation = visualUnit.GetComponent<FormationUnitController>();
                if (formation != null) formation.enabled = false;

                // 2. แปะสคริปต์สมองเฝ้าฐาน
                var garrisonAI = visualUnit.AddComponent<GarrisonUnitAI>();

                // 3. ปรับการส่งพิกัด: 
                // ในเมื่อ GarrisonUnitAI คำนวณจุดวนรอบตัวเองแล้ว
                // คุณไม่ต้องส่ง spawnPoints เข้าไปครับ ให้มันเริ่มคำนวณจากจุดที่มันยืน (spawnPoints[i].position)
                // ตัวสคริปต์ GarrisonUnitAI จะจัดการ startPosition เองใน Start()

                // --- 🟢 จบขั้นตอน Setup ---

                spawnedVisualUnits.Add(visualUnit);
                visualUnit.name = $"{unit.customName} ({unit.uniqueId})";
            }
        }
        Debug.Log($"[Visualizer] 🐺 เสกทหารเฝ้าค่าย [{outpostData.outpostName}] พร้อมติดตั้งระบบ AI เรียบร้อย!");
    }
}