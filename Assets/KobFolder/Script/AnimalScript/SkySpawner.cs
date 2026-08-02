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
    [SerializeField] private string unitDefaultLayer;
    [SerializeField] private LayerMask groundLayer;

    [Header("Quantity Settings")]
    [SerializeField] private int minSpawnCount = 2;
    [SerializeField] private int maxSpawnCount = 5;

    [Header("Area Settings (สุ่มอิสระรอบจุด Spawner)")]
    [SerializeField] private float worldSpawnRadius = 100f; // รัศมีกว้างๆ ที่จะให้สัตว์สุ่มเกิดรอบๆ ตําแหน่งตัว Script นี้
    [SerializeField] private float individualSpreadRadius = 4f; // รัศรีย่อยตอนกระจายตัวสัตว์แต่ละตัว
    [SerializeField] private float raycastHeight = 30f;

    void Update()
    {
        // ตัวอย่าง: กดปุ่ม Alpha0 (เลข 0) เพื่อสั่งสุ่มเสกสัตว์ลงมาจากฟ้าในพื้นที่อิสระ
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Debug.Log("Random Spawn from Sky");
            SpawnRandomlyInWorld();
        }
    }

    void Start()
    {
        // หรือถ้าอยากให้มันสุ่มเสกทันทีตอนเริ่มเกม ก็เปิดคอมเมนต์บรรทัดล่างนี้ได้ครับ
        // SpawnRandomlyInWorld();
    }

    // 🟢 ฟังก์ชันหลักในการสุ่มพิกัดอิสระรอบๆ ตัว Spawner
    public void SpawnRandomlyInWorld()
    {
        if (unitList == null || unitList.Count == 0)
        {
            Debug.LogError("SkySpawner: ไม่มี Prefab ใน Unit List!");
            return;
        }

        // 1. สุ่มจุดศูนย์กลาง (Center Point) ภายในรัศมี worldSpawnRadius รอบๆ ตำแหน่งที่ตึก/สปอนเซอร์ตัวนี้วางอยู่
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * worldSpawnRadius;
        Vector3 randomCenterPos = new Vector3(transform.position.x + randomCircle.x, transform.position.y + raycastHeight, transform.position.z + randomCircle.y);

        Vector3 groundHitPoint = randomCenterPos;
        Vector3 rayOrigin = randomCenterPos;

        // 2. 🛠️ ยิงลำแสงลงพื้นเพื่อหา Ground Layer ที่แท้จริง ณ จุดที่สุ่มได้
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer))
        {
            groundHitPoint = hit.point;
            Debug.Log($"SkySpawner: [Raycast Hit] สุ่มเจอพื้นผิว Ground ที่พิกัด: {groundHitPoint}");
        }
        else
        {
            // ถ้าไม่โดนพื้น ให้ใช้ความสูงปัจจุบันเป็นค่าสำรอง
            groundHitPoint = new Vector3(randomCenterPos.x, transform.position.y, randomCenterPos.z);
            Debug.LogWarning($"SkySpawner: [Raycast Miss] ยิงไม่เจอเลเยอร์ Ground ใช้พิกัดสำรองที่: {groundHitPoint}");
        }

        int actualSpawnCount = UnityEngine.Random.Range(minSpawnCount, maxSpawnCount + 1);

        for (int i = 0; i < actualSpawnCount; i++)
        {
            GameObject selectedPrefab = GetRandomUnitPrefab();
            if (selectedPrefab == null) continue;

            // 3. สุ่มกระจายตำแหน่งย่อยรอบจุดศูนย์กลางที่สุ่มได้
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-individualSpreadRadius, individualSpreadRadius),
                0.2f, // ยกลอยขึ้นเหนือผิวเล็กน้อย
                UnityEngine.Random.Range(-individualSpreadRadius, individualSpreadRadius)
            );

            Vector3 desiredSpawnPos = groundHitPoint + randomOffset;

            // 4. ตรวจสอบพื้นที่บน NavMesh หรือวางตามพิกัดดิบ
            if (NavMesh.SamplePosition(desiredSpawnPos, out NavMeshHit navHit, individualSpreadRadius * 2f, NavMesh.AllAreas))
            {
                GameObject newAnimal = Instantiate(selectedPrefab, navHit.position, Quaternion.identity);
                newAnimal.tag = "Enemy";
                newAnimal.gameObject.layer = LayerMask.NameToLayer(unitDefaultLayer);
            }
            else
            {
                GameObject newAnimal = Instantiate(selectedPrefab, desiredSpawnPos, Quaternion.identity);
                newAnimal.tag = "Enemy";
                newAnimal.gameObject.layer = LayerMask.NameToLayer(unitDefaultLayer);
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