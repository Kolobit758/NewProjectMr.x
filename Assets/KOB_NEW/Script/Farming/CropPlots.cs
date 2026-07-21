using UnityEngine;
using System.Collections; // 🟢 จำเป็นต้องใช้สำหรับ Coroutine

// 🟢 อัปเกรดสถานะให้ละเอียดขึ้นตามลำดับการปลูก
public enum CropStage { Empty, NeedsFertilizer, NeedsWater, Growing, ReadyToHarvest }

public class CropPlots : MonoBehaviour, ITaskable // 🟢 ต้องใส่ ITaskable ให้ยูนิตคุยได้
{
    [Header("Current Status")]
    public CropStage currentStage = CropStage.Empty;
    public SO_PlantData plantedPlantData;

    private float growthTimer = 0f;
    private float autoCallCooldown = 0f; // กันการตะโกนเรียกยูนิตรัวๆ
    private GameObject currentVisualObject;

    [Header("Plot Durability Settings")]
    public int maxPlotHealth = 3;
    public int currentPlotHealth = 3;

    [Header("Requirements Info")]
    public string fertilizerKey = "Fertilizer"; // ชื่อหรือ ID ปุ๋ยที่จะใช้ (ต้องตรงกับใน Database)
    public string waterKey = "Water";           // ชื่อหรือ ID น้ำที่จะใช้
    public SO_ItemData fertilizerItemData;
    public SO_ItemData waterItemData;
    
    [Header("AI Lock")]
    public bool isBeingServiced = false; // 🟢 ล็อคไว้กันเรียกยูนิตซ้ำซ้อน


    void Update()
    {
        // ⏰ ระบบรันการเจริญเติบโตเฉพาะตอนกลางวัน 
        if (MotherTreeController.Instance != null && MotherTreeController.Instance.isNightTime) return;

        // 🟢 ถ้าอยู่ในช่วงกำลังโต ก็จับเวลาไป
        if (currentStage == CropStage.Growing)
        {
            growthTimer += Time.deltaTime;

            if (growthTimer >= plantedPlantData.timeToGrow)
            {
                SwitchStage(CropStage.ReadyToHarvest);
            }
        }
        // 🟢 ถ้ารอน้ำอยู่ ให้กวักมือเรียกยูนิตที่ว่างงานแถวนั้นแบบ Auto!
        else if (currentStage == CropStage.NeedsWater)
        {
            // 🟢 ถ้ายังไม่มีใครกำลังเดินมาช่วย และหมดคูลดาวน์ ถึงจะเรียก
            if (!isBeingServiced)
            {
                autoCallCooldown -= Time.deltaTime;
                if (autoCallCooldown <= 0f)
                {
                    CallNearbyIdleUnitForWater();
                    autoCallCooldown = 3f;
                }
            }
        }
    }

    // 🌱 ฟังก์ชันหยอดเมล็ดลงแปลง (คนเล่นคลิกจาก FarmingManager)
    public bool PlantSeed(SO_PlantData plantData)
    {
        if (currentStage != CropStage.Empty) return false;

        plantedPlantData = plantData;
        growthTimer = 0f;
        SwitchStage(CropStage.NeedsFertilizer); // ปลูกปุ๊บ ต้องการปุ๋ยทันที!
        return true;
    }

    // ในฟังก์ชัน OnUnitInteract เมื่อยูนิตมาถึง
    public void OnUnitInteract(UnitBase unit)
    {
        if (currentStage == CropStage.NeedsFertilizer)
        {
            if (unit.isCarrying && unit.carriedItem != null && unit.carriedItem == fertilizerItemData)
            {
                unit.DropItemAtVault();
                SwitchStage(CropStage.NeedsWater);
                isBeingServiced = false; // 🟢 ปลดล็อคให้รอน้ำต่อได้
            }
            else
            {
                isBeingServiced = true; // 🟢 ล็อคว่ากำลังมีคนจัดการ
                unit.GoFetchItemAndReturn(fertilizerItemData, this);
            }
        }
        else if (currentStage == CropStage.NeedsWater)
        {
            if (unit.isCarrying && unit.carriedItem != null && unit.carriedItem == waterItemData)
            {
                unit.DropItemAtVault();
                SwitchStage(CropStage.Growing);
                isBeingServiced = false; // 🟢 ปลดล็อคเมื่อเสร็จสิ้น
            }
            else
            {
                isBeingServiced = true; // 🟢 ล็อคว่ากำลังมีคนจัดการ
                unit.GoFetchItemAndReturn(waterItemData, this);
            }
        }
    }

    // 🔍 ฟังก์ชันให้แปลงผักสแกนหายูนิตว่างงานมารดน้ำ
    private void CallNearbyIdleUnitForWater()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 20f); // รัศมี 20 เมตร
        foreach (var hitCollider in hitColliders)
        {
            UnitBase unit = hitCollider.GetComponent<UnitBase>();
            // ถ้าเจอยูนิตที่กำลัง Idle ว่างงานอยู่ สั่งมันมาทำงานเลย!
            if (unit != null && unit.currentBehavior == UnitBehavior.Idle)
            {
                Debug.Log($"🤖 [Auto] แปลงผักเรียก {unit.name} มารดน้ำ!");
                unit.MoveTo(GetInteractionPoint(), this);
                break; // เรียกได้ตัวนึงก็พอแล้ว ออกลูป
            }
        }
    }

    public void HarvestCrop()
    {
        if (currentStage != CropStage.ReadyToHarvest || plantedPlantData == null) return;

        if (ResourceInventory.Instance != null)
        {
            int cropAmount = Random.Range(plantedPlantData.minProductAmount, plantedPlantData.maxProductAmount + 1);
            if (plantedPlantData.cropProduct != null)
                ResourceInventory.Instance.AddResource(plantedPlantData.cropProduct, cropAmount);

            if (plantedPlantData.cropSeed != null) // แก้บัคเล็กๆ ของเก่าพิมพ์ seedPrefab ผิด
                ResourceInventory.Instance.AddResource(plantedPlantData.cropSeed, cropAmount);

            if (plantedPlantData.poopFertilizerProduct != null)
                ResourceInventory.Instance.AddResource(plantedPlantData.poopFertilizerProduct, plantedPlantData.fertilizerAmount);

            Debug.Log($"🌾 [Harvest]: เก็บเกี่ยวสำเร็จ!");
        }

        plantedPlantData = null;
        SwitchStage(CropStage.Empty);
        isBeingServiced = false;
        plantedPlantData = null;
        SwitchStage(CropStage.Empty);
    }

    private void SwitchStage(CropStage newStage)
    {
        currentStage = newStage;

        if (currentVisualObject != null) Destroy(currentVisualObject);
        if (plantedPlantData == null) return;

        GameObject prefabToSpawn = null;
        switch (currentStage)
        {
            // 🟢 ช่วงรอน้ำ รอปุ๋ย ให้แสดงเป็นเมล็ดไปก่อน
            case CropStage.NeedsFertilizer:
            case CropStage.NeedsWater:
                prefabToSpawn = plantedPlantData.seedPrefab;
                break;
            case CropStage.Growing:
                prefabToSpawn = plantedPlantData.growingPrefab;
                break;
            case CropStage.ReadyToHarvest:
                prefabToSpawn = plantedPlantData.fullyGrownPrefab;
                break;
        }

        if (prefabToSpawn != null)
        {
            Vector3 originalPrefabScale = prefabToSpawn.transform.localScale;
            currentVisualObject = Instantiate(prefabToSpawn, transform.position, Quaternion.identity, transform);
            currentVisualObject.transform.localScale = originalPrefabScale;
        }
    }

    public void OnUnitExit(UnitBase unit){

    }

    // 🟢 จำเป็นต้องมีสำหรับ ITaskable
    public Vector3 GetInteractionPoint() => transform.position;

    #region Destroy Product
    // public void TakeDamageFromBat()
    // {
    //     currentPlotHealth--;
    //     Debug.LogWarning($"⚠ [CropPlots]: แปลงโดนแทะ! เลือดเหลือ {currentPlotHealth}/{maxPlotHealth}");

    //     if (currentPlotHealth <= 0) DestroyCropCompletely();
    // }

    // private void DestroyCropCompletely()
    // {
    //     currentStage = CropStage.Empty;
    //     currentPlotHealth = maxPlotHealth; 

    //     foreach (Transform child in transform)
    //     {
    //         if (child.name.Contains("Clone") || child.name.Contains("GlandRice"))
    //         {
    //             Destroy(child.gameObject);
    //         }
    //     }
    // }
    #endregion
}