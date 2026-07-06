using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SkySpawner : MonoBehaviour
{
    [System.Serializable]
    public struct SpawnableUnitData
    {
        public GameObject unitPrefab;
        [Range(0, 100)] public float spawnChance;
    }

    [Header("Spawn Settings")]
    [SerializeField] private List<SpawnableUnitData> unitList = new List<SpawnableUnitData>();
    [SerializeField] private LayerMask groundLayer;

    [Header("Quantity Settings")]
    [SerializeField] private int minSpawnCount = 2;
    [SerializeField] private int maxSpawnCount = 5;

    [Header("Area Settings")]
    [SerializeField] private float spawnRadius = 4f;
    [SerializeField] private float raycastHeight = 30f;
    // สคริปต์ตัวอย่าง ณ จุดที่กดสั่งเสก (เช่น ในสคริปต์ควบคุมเกมของคุณ)
    void Update()
    {
        // ตัวอย่าง: ถ้ากดปุ่มเลข 9 บนคีย์บอร์ดให้เรียกสัตว์ลงมาจากฟ้าตรงตำแหน่งตัวผู้เล่น
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Debug.Log("Create animal");
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            SkySpawner spawner = FindAnyObjectByType<SkySpawner>(); // ค้นหาตัวสปอว์นเนอร์ในฉาก

            if (spawner != null && playerObj != null)
            {
                // ส่งตำแหน่งเท้าของผู้เล่นไปให้สคริปต์เสกทำงานรอบ ๆ ตัวเขาเลย
                spawner.SpawnFromSky(playerObj.transform.position);
            }
        }
    }
    void Start()
    {
        Debug.Log("Create animal");
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        SkySpawner spawner = FindAnyObjectByType<SkySpawner>(); // ค้นหาตัวสปอว์นเนอร์ในฉาก

        if (spawner != null && playerObj != null)
        {
            // ส่งตำแหน่งเท้าของผู้เล่นไปให้สคริปต์เสกทำงานรอบ ๆ ตัวเขาเลย
            spawner.SpawnFromSky(playerObj.transform.position);
        }
    }
    public void SpawnFromSky(Vector3 targetPosition)
    {
        if (unitList == null || unitList.Count == 0)
        {
            Debug.LogError("SkySpawner: ไม่มี Prefab ใน Unit List!");
            return;
        }

        Vector3 groundHitPoint = targetPosition; // ตั้งค่าเริ่มต้นอิงตำแหน่งดิบที่ส่งมาก่อนกันเหนียว
        Vector3 rayOrigin = new Vector3(targetPosition.x, targetPosition.y + raycastHeight, targetPosition.z);

        // 🛠️ ยิงลำแสงเช็คพื้นผิว (ถ้าเจอเลเยอร์ Ground จะได้ความสูงผิวสัมผัสที่แม่นยำ)
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer))
        {
            groundHitPoint = hit.point;
            Debug.Log($"SkySpawner: [Raycast Hit] เจอพื้นผิวเลเยอร์ Ground ที่: {groundHitPoint}");
        }
        else
        {
            // 🚨 แผนสำรองถ้าระบบเลเยอร์เอ๋อ: บังคับใช้ตำแหน่งแกน Y ที่ส่งมาเป็นฐานตั้งต้นเลย เกมจะไม่ค้างและเสกออกแน่นอน
            Debug.LogWarning($"SkySpawner: [Raycast Miss] ยิงไม่เจอเลเยอร์ Ground แต่สคริปต์จะใช้พิกัดทดแทนที่: {groundHitPoint}");
        }

        int actualSpawnCount = UnityEngine.Random.Range(minSpawnCount, maxSpawnCount + 1);

        for (int i = 0; i < actualSpawnCount; i++)
        {
            GameObject selectedPrefab = GetRandomUnitPrefab();
            if (selectedPrefab == null) continue;

            // สุ่มพิกัดกระจายรอบจุดตกกระทบ
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-spawnRadius, spawnRadius),
                0.2f, // ยกลอยขึ้นเหนือผิวเล็กน้อยเพื่อความปลอดภัย
                UnityEngine.Random.Range(-spawnRadius, spawnRadius)
            );

            Vector3 desiredSpawnPos = groundHitPoint + randomOffset;

            // ตรวจสอบพื้นที่บน NavMesh สีฟ้า
            if (NavMesh.SamplePosition(desiredSpawnPos, out NavMeshHit navHit, spawnRadius * 2f, NavMesh.AllAreas))
            {
                Instantiate(selectedPrefab, navHit.position, Quaternion.identity);
            }
            else
            {
                // ถ้าสุ่มไปนอก NavMesh ให้เสกตำแหน่งดิบตรงนั้นเลย สัตว์จะได้เกิดครบถ้วนตามจำนวน
                Instantiate(selectedPrefab, desiredSpawnPos, Quaternion.identity);
            }
        }
    }

    private GameObject GetRandomUnitPrefab()
    {
        float totalChance = 0f;
        foreach (var unit in unitList) totalChance += unit.spawnChance;
        if (totalChance <= 0) return null;

        float randomValue = UnityEngine.Random.Range(0f, totalChance);
        float cumulativeChance = 0f;

        foreach (var unit in unitList)
        {
            cumulativeChance += unit.spawnChance;
            if (randomValue <= cumulativeChance) return unit.unitPrefab;
        }
        return null;
    }
}