using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// ข้อมูล Unit แต่ละ entry ที่ต้องการ spawn — กำหนดใน Inspector ได้เลย
/// </summary>
[Serializable]
public class UnitSpawnEntry
{
    [Tooltip("Prefab ของ Unit ที่ต้องการ spawn")]
    public GameObject prefab;

    [Tooltip("ตำแหน่ง spawn (ลาก Transform จาก Scene มาใส่)")]
    public Transform spawnPoint;

    [Tooltip("จำนวนที่ต้องการ spawn จาก entry นี้")]
    public int count = 1;
}

public class SpawnAllUnit : MonoBehaviour
{
    public static SpawnAllUnit Instance;
    [Header("Unit Spawn Entries")]
    [Tooltip("กำหนด Prefab, SpawnPoint, และจำนวนของ Unit แต่ละชนิดที่นี่")]
    public List<UnitSpawnEntry> spawnEntries = new List<UnitSpawnEntry>();

    [Header("Settings")]
    public Transform unitFolder;
    public string layerName = "Unit";

    [Tooltip("ระยะ offset แบบ grid เวลา spawn หลายตัวในจุดเดียวกัน (เพื่อไม่ให้ซ้อนกัน)")]
    public float spawnSpacing = 1.2f;
    public int starterCapacity;

    private void Awake(){
        if(Instance == null){
            Instance = this;
        }else{
            Destroy(gameObject);
        }
    }

    void Start()
    {
        SpawnUnits();
    }

    void SpawnUnits()
    {
        if (RTS_movement.instance == null)
        {
            Debug.LogError("[SpawnAllUnit] ไม่พบ RTS_movement.instance!");
            return;
        }

        foreach (UnitSpawnEntry entry in spawnEntries)
        {
            if (entry.prefab == null)
            {
                Debug.LogWarning("[SpawnAllUnit] Prefab เป็น null — ข้าม entry นี้");
                continue;
            }
            if (entry.spawnPoint == null)
            {
                Debug.LogWarning($"[SpawnAllUnit] SpawnPoint ของ '{entry.prefab.name}' เป็น null — ข้าม");
                continue;
            }

            for (int i = 0; i < entry.count; i++)
            {
                // คำนวณ offset แบบ grid เพื่อไม่ให้ unit ซ้อนกัน
                Vector3 offset = GetGridOffset(i, entry.count);
                Vector3 spawnPos = entry.spawnPoint.position + offset;

                Transform parent = unitFolder != null ? unitFolder : transform;
                GameObject spawned = Instantiate(entry.prefab, spawnPos, entry.spawnPoint.rotation, parent);

                // ตั้งค่า Component
                UnitBase unitBase = spawned.GetComponent<UnitBase>();
                if (unitBase != null)
                    unitBase.enabled = true;

                AnimalAIController aiCtrl = spawned.GetComponent<AnimalAIController>();
                if (aiCtrl != null)
                    aiCtrl.enabled = false;

                // ตั้ง tag และ layer
                spawned.tag = "Unit";
                int layerIndex = LayerMask.NameToLayer(layerName);
                if (layerIndex >= 0)
                    spawned.layer = layerIndex;

                // เพิ่มเข้า allUnits
                if (unitBase != null)
                    RTS_movement.instance.allUnits.Add(unitBase);

                Debug.Log($"[SpawnAllUnit] Spawned: {entry.prefab.name} #{i + 1} at {spawnPos}");
            }
        }
    }

    /// <summary>
    /// คำนวณตำแหน่ง offset แบบ grid สี่เหลี่ยม เพื่อกระจาย unit ไม่ให้ซ้อนกัน
    /// </summary>
    private Vector3 GetGridOffset(int index, int total)
    {
        if (total <= 1) return Vector3.zero;

        int cols = Mathf.CeilToInt(Mathf.Sqrt(total));
        int row = index / cols;
        int col = index % cols;

        float centerOffsetX = (cols - 1) * spawnSpacing * 0.5f;
        int rows = Mathf.CeilToInt((float)total / cols);
        float centerOffsetZ = (rows - 1) * spawnSpacing * 0.5f;

        return new Vector3(
            col * spawnSpacing - centerOffsetX,
            0f,
            row * spawnSpacing - centerOffsetZ
        );
    }
}