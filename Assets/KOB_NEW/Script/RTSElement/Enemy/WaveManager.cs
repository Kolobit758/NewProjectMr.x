using UnityEngine;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Enemy Database (สำรองกรณีฟาร์มว่างเปล่า)")]
    public List<SO_EnemyData> allAvailableEnemies = new List<SO_EnemyData>();

    [Header("Spawn Points")]
    public Transform[] enemySpawnPoints;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnTimeChanged += HandleTimeChanged;
        }
    }

    private void HandleTimeChanged(bool isNight)
    {
        if (isNight) TriggerEnemyWave();
    }

    public void TriggerEnemyWave()
    {
        Debug.Log("🚨 [WaveManager]: คลื่นศัตรูบุกฐานกำลังมา!");

        // 1. ค้นหาว่าพืชในฟาร์มดึงดูดศัตรูตัวไหนบ้าง
        List<SO_EnemyData> enemiesToSpawn = DetermineAttractedEnemiesFromFarm();

        if (enemiesToSpawn == null || enemiesToSpawn.Count == 0)
        {
            // ถ้าฟาร์มไม่มีคนปลูกผัก หรือผักไม่มีตัวดึงดูด ให้สุ่มจาก Database สำรองทั่วไป
            if (allAvailableEnemies.Count > 0)
            {
                enemiesToSpawn.Add(allAvailableEnemies[Random.Range(0, allAvailableEnemies.Count)]);
            }
            else return;
        }

        int waveCount = 3 + (DayNightManager.Instance != null ? DayNightManager.Instance.currentDay * 2 : 0);

        // 2. สกาวน์ศัตรูออกมาตามรายชื่อที่พืชดึงดูด
        for (int i = 0; i < waveCount; i++)
        {
            // สุ่มหยิบจากรายชื่อศัตรูที่ถูกดึงดูดมาจากแปลงพืช
            SO_EnemyData chosenEnemy = enemiesToSpawn[Random.Range(0, enemiesToSpawn.Count)];
            SpawnEnemy(chosenEnemy);
        }
    }

    // 🌾 ฟังก์ชันกวาดหาแปลงพืชในฟาร์ม แล้วดึง SO_EnemyData ที่ผูกไว้
    private List<SO_EnemyData> DetermineAttractedEnemiesFromFarm()
    {
        CropPlots[] allPlots = Object.FindObjectsByType<CropPlots>(FindObjectsSortMode.None);
        List<SO_EnemyData> attractedEnemiesList = new List<SO_EnemyData>();

        if (allPlots == null || allPlots.Length == 0) return null;

        foreach (var plot in allPlots)
        {
            if (plot.currentStage != CropStage.Empty && plot.plantedPlantData != null)
            {
                SO_PlantData plantData = plot.plantedPlantData as SO_PlantData;

                // 🟢 ถ้าพืชชนิดนี้มีรายการศัตรูใน List ให้ดึงออกมาทั้งหมด
                if (plantData != null && plantData.specificAttractedEnemies != null)
                {
                    foreach (var enemyData in plantData.specificAttractedEnemies)
                    {
                        if (enemyData != null && !attractedEnemiesList.Contains(enemyData))
                        {
                            attractedEnemiesList.Add(enemyData);
                        }
                    }
                }
            }
        }

        return attractedEnemiesList;
    }

    private void SpawnEnemy(SO_EnemyData enemyData)
    {
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0 || enemyData == null || enemyData.enemyPrefab == null) return;

        Transform spawnPoint = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
        GameObject enemyObj = Instantiate(enemyData.enemyPrefab, spawnPoint.position, Quaternion.identity);

        EnemyController controller = enemyObj.GetComponent<EnemyController>();
        if (controller != null) controller.enemyData = enemyData;
    }
}