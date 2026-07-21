using UnityEngine;

public enum CropStage { Empty, Growing, NeedsFertilizer, NeedsWater, ReadyToHarvest }


public class CropPlots : MonoBehaviour, ITaskable
{
    public CropStage currentStage = CropStage.Empty;
    public bool isBeingServiced = false;
    public SO_PlantData plantedSeed;

    private float growthTimer = 0f;
    private GameObject currentVisualInstance;

    void Update()
    {
        // ระบบจำลองเวลาพืชโต
        if (currentStage == CropStage.Growing && plantedSeed != null)
        {
            growthTimer += Time.deltaTime;
            if (growthTimer >= plantedSeed.timeToGrow)
            {
                UpdateStage(CropStage.ReadyToHarvest);
            }
        }
    }

    public Vector3 GetInteractionPoint() => transform.position;

    public void UpdateStage(CropStage newStage)
    {
        currentStage = newStage;
        SpawnVisualForCurrentStage();
    }

    private void SpawnVisualForCurrentStage()
    {
        // ลบโมเดลเก่าทิ้งก่อน
        if (currentVisualInstance != null)
        {
            Destroy(currentVisualInstance);
        }

        if (plantedSeed == null || currentStage == CropStage.Empty) return;

        GameObject prefabToSpawn = null;

        switch (currentStage)
        {
            case CropStage.NeedsFertilizer:
            case CropStage.NeedsWater:
                prefabToSpawn = plantedSeed.seedPrefab;
                break;
            case CropStage.Growing:
                prefabToSpawn = plantedSeed.growingPrefab;
                break;
            case CropStage.ReadyToHarvest:
                prefabToSpawn = plantedSeed.fullyGrownPrefab;
                break;
        }

        if (prefabToSpawn != null)
        {
            currentVisualInstance = Instantiate(prefabToSpawn, transform.position, transform.rotation, transform);
        }
    }

    public bool PlantSeed(SO_PlantData seedData)
    {
        if (currentStage != CropStage.Empty) return false;

        plantedSeed = seedData;
        growthTimer = 0f;
        UpdateStage(CropStage.NeedsFertilizer); // หยอดเมล็ดเสร็จ เปลี่ยนเป็นสถานะรอใส่ปุ๋ย พร้อมโชว์ Model เมล็ด
        return true;
    }

    public void OnUnitInteract(UnitBase unit)
    {
        if (currentStage == CropStage.NeedsFertilizer)
        {
            if (!unit.isCarrying)
            {
                unit.RequestFetchItem(plantedSeed, 1);
                return;
            }
            unit.isCarrying = false;
            UpdateStage(CropStage.NeedsWater); // ใส่ปุ๋ยเสร็จ เปลี่ยนเป็นรดน้ำ
            unit.CompleteOrder(this);
        }
        else if (currentStage == CropStage.NeedsWater)
        {
            UpdateStage(CropStage.Growing); // รดน้ำเสร็จ เข้าสู่โหมดเติบโตตามเวลา พร้อมเปลี่ยน Model
            unit.CompleteOrder(this);
        }
        else if (currentStage == CropStage.ReadyToHarvest)
        {
            HarvestCrop();
            unit.CompleteOrder(this);
        }
    }

    public void HarvestCrop()
    {
        if (plantedSeed != null && plantedSeed.cropProduct != null)
        {
            int amount = Random.Range(plantedSeed.minProductAmount, plantedSeed.maxProductAmount + 1);
            ResourceInventory.Instance?.AddResource(plantedSeed.cropProduct, amount);
            Debug.Log($"✨ เก็บเกี่ยวสำเร็จ! ได้รับ {plantedSeed.cropProduct.itemName} จำนวน {amount} ชิ้น");
        }

        if (currentVisualInstance != null) Destroy(currentVisualInstance);

        currentStage = CropStage.Empty;
        plantedSeed = null;
        isBeingServiced = false;
    }

    public void OnUnitExit(UnitBase unit)
    {
        isBeingServiced = false;
    }
}