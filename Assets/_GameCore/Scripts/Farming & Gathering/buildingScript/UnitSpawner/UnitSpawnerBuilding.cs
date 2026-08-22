using UnityEngine;

public class UnitSpawnerBuilding : MonoBehaviour, ITaskable
{
    public enum SpawnerType { AnimalShelter, RobotFactory }
    public SpawnerType spawnerType;

    [Header("Production Settings")]
    public GameObject unitPrefabToSpawn; 
    public int unitCostGold = 50;        
    public float productionTime = 3f;    
    private bool isSpawning = false;

    // เปิด UI สร้างยูนิต (เชื่อมกับ UnitSpawnerUI)
    public void OnClickOpenSpawnerUI()
    {
        if (UnitSpawnerUI.Instance != null)
        {
            UnitSpawnerUI.Instance.OpenSpawnerPanel(this);
        }
    }

    public void RequestSpawnUnit()
    {
        if (isSpawning) return;

        if (spawnerType == SpawnerType.AnimalShelter)
        {
            if (AnimalShelter.IsTotalCapacityFull())
            {
                Debug.LogWarning("❌ [Shelter]: คอกสัตว์เต็มแล้ว!");
                return;
            }
        }

        StartCoroutine(SpawnRoutine());
    }

    private System.Collections.IEnumerator SpawnRoutine()
    {
        isSpawning = true;
        Debug.Log($"🔨 กำลังผลิตยูนิต...");
        yield return new WaitForSeconds(productionTime);

        Vector3 spawnPos = transform.position + transform.forward * 2f;
        GameObject newUnitObj = Instantiate(unitPrefabToSpawn, spawnPos, Quaternion.identity);

        UnitBase newUnit = newUnitObj.GetComponent<UnitBase>();
        if (newUnit != null && spawnerType == SpawnerType.AnimalShelter)
        {
            AnimalShelter shelter = AnimalShelter.GetAvailableShelter();
            if (shelter != null) shelter.RegisterUnit(newUnit);
        }

        isSpawning = false;
        Debug.Log($"✨ ผลิตยูนิตสำเร็จ!");
    }

    public void OnUnitInteract(UnitBase unit) { }
    public void OnUnitExit(UnitBase unit) { }
    public Vector3 GetInteractionPoint() => transform.position + transform.forward * 2f;
}