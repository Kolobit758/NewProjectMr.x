using UnityEngine;
using System.Collections.Generic;

public class GlobalBatSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject batPrefab;

    [Header("Global Spawn Settings")]
    public float spawnInterval = 5f; // สุ่มเสกค้างคาวทุก ๆ 5 วินาที
    private float spawnTimer = 0f;
    public float spawnRadius = 25f; // รัศมีวงกลมห่างจากจุดศูนย์กลางแม็พ (ให้มันเกิดไกล ๆ นอกจอ)
    public float flyHeight = 10f;    // ความสูงตอนค้างคาวบินร่อนมา

    public int EnergyLessThanToAttack = 0;

    void Update()
    {
        // 🌙 เงื่อนไขระดับโลก: ต้องเป็นเวลากลางคืนเท่านั้นถึงจะรันลูปเสก
        if (MotherTreeController.Instance != null && MotherTreeController.Instance.isNightTime)
        {
            HandleGlobalNightSpawning();
        }
        else
        {
            spawnTimer = 0f;
        }
    }

    private void HandleGlobalNightSpawning()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnBatToAttackVulnerablePlot();
        }
    }

    // 🎯 [ContextMenu]: กดคลิกขวาสั่งบอมบ์ค้างคาวจากนอกแม็พได้ทันที!
    [ContextMenu("Test Global Spawn Bat")]
    private void SpawnBatToAttackVulnerablePlot()
    {
        // 🔍 1. ค้นหาแปลงผักที่ "ผักโตเต็มที่" ทั่วทั้งแม็พสด ๆ ร้อน ๆ
        CropPlots targetPlot = FindVulnerableCropPlotInWorld();

        if (targetPlot != null)
        {
            // 🗺️ 2. สุ่มพิกัดเกิดเป็นวงกลมรอบนอกแม็พ (สุ่มมุม 0-360 องศา)
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 spawnPosition = new Vector3(
                Mathf.Cos(randomAngle) * spawnRadius,
                flyHeight,
                Mathf.Sin(randomAngle) * spawnRadius
            );

            // 🦇 3. เสกค้างคาวตรงพิกัดวงนอก แล้วส่งข้อมูลแปลงเป้าหมายให้มันบินไปแดก
            GameObject newBat = Instantiate(batPrefab, spawnPosition, Quaternion.identity);
            BatEntity batAI = newBat.GetComponent<BatEntity>();
            
            if (batAI != null)
            {
                batAI.InitializeTarget(targetPlot);
                Debug.Log($"🦇 [Global Spawner]: เสกค้างคาวรอบนอกพิกัด {spawnPosition} บินมุ่งหน้าไปแดกแปลง {targetPlot.name} แล้วมึงกอบ!");
            }
        }
        else
        {
            Debug.LogWarning("⚠ [Global Spawner]: เสกไม่ติด เพราะไม่มีแปลงผักไหนในโลกนี้ที่ผักโตเต็มที่ หรือเกาะเป้าหมายมีไฟป้องกันอยู่!");
        }
    }

    private CropPlots FindVulnerableCropPlotInWorld()
    {
        // ดึงรายชื่อแปลงผักทั้งหมดที่มีอยู่ในโลกตอนนั้น
        CropPlots[] allPlotsInWorld = FindObjectsByType<CropPlots>(FindObjectsSortMode.None);
        List<CropPlots> vulnerablePlots = new List<CropPlots>();

        // foreach (CropPlots plot in allPlotsInWorld)
        // {
        //     Debug.Log("plottt found : " + plot.gameObject.name);
        // }

        foreach (CropPlots plot in allPlotsInWorld)
        {
            if (plot != null && (plot.currentStage == CropStage.ReadyToHarvest || plot.currentStage == CropStage.Growing))
            {

                // 🏝️ [เช็คเงื่อนไขความเปราะบาง]: แปลงผักนั้นต้องอยู่บนเกาะที่ "ไฟดับพลังงานเหลือ 0" เท่านั้น!
                IslandController parentIsland = plot.GetComponentInParent<IslandController>();
                if (parentIsland != null && parentIsland.currentEnergy <= EnergyLessThanToAttack)
                {
                    vulnerablePlots.Add(plot); // แปลงนี้โดนค้างคาวเล็งได้!
                }
            }
        }

        // ถ้ามีแปลงที่เข้าข่ายโดนแดกหลายแปลง ให้สุ่มเลือกมา 1 แปลงเพื่อกระจายการโจมตี
        if (vulnerablePlots.Count > 0)
        {
            int randomIndex = Random.Range(0, vulnerablePlots.Count);
            return vulnerablePlots[randomIndex];
        }

        return null; // ทุกเกาะปลอดภัยดี ปิดไฟนอน
    }
}