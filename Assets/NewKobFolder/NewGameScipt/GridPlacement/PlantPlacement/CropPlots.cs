using UnityEngine;

public enum CropStage { Empty, Seed, Growing, ReadyToHarvest }

public class CropPlots : MonoBehaviour
{
    [Header("Current Status")]
    public CropStage currentStage = CropStage.Empty;
    public SO_PlantData plantedPlantData;

    private float growthTimer = 0f;
    private GameObject currentVisualObject;
    [Header("Plot Durability Settings")]
    public int maxPlotHealth = 3; // ต้องโดนแทะ 3 รอบถึงจะทำลายแปลงผัก
    public int currentPlotHealth = 3;

    void Update()
    {
        // ⏰ ระบบรันการเจริญเติบโตเฉพาะตอนกลางวัน (หรือถ้ามึงอยากให้โตกลางคืนด้วยก็เอา isNightTime ออกได้มึง)
        if (MotherTreeController.Instance != null && MotherTreeController.Instance.isNightTime) return;

        if (currentStage == CropStage.Seed || currentStage == CropStage.Growing)
        {
            growthTimer += Time.deltaTime;

            // เปลี่ยนเฟสจาก เมล็ด -> กำลังโต (ที่ครึ่งทาง)
            if (currentStage == CropStage.Seed && growthTimer >= plantedPlantData.timeToGrow * 0.5f)
            {
                SwitchStage(CropStage.Growing);
            }
            // เปลี่ยนเฟสจาก กำลังโต -> โตเต็มที่พร้อมเก็บเกี่ยว
            if (currentStage == CropStage.Growing && growthTimer >= plantedPlantData.timeToGrow)
            {
                SwitchStage(CropStage.ReadyToHarvest);
            }
        }
    }

    // 🌱 ฟังก์ชันหยอดเมล็ดลงแปลง
    public bool PlantSeed(SO_PlantData plantData)
    {
        if (currentStage != CropStage.Empty) return false; // ถ้าแปลงไม่ว่าง ห้ามปลูกซ้ำ!

        plantedPlantData = plantData;
        growthTimer = 0f;
        SwitchStage(CropStage.Seed);
        return true;
    }

    public void HarvestCrop()
    {
        if (currentStage != CropStage.ReadyToHarvest || plantedPlantData == null) return;

        if (ResourceInventory.Instance != null)
        {
            // 1. สุ่มจำนวนและแจกผลผลิตผักเข้าคลังกระเป๋าจริง
            int cropAmount = Random.Range(plantedPlantData.minProductAmount, plantedPlantData.maxProductAmount + 1);
            if (plantedPlantData.cropProduct != null)
            {
                ResourceInventory.Instance.AddResource(plantedPlantData.cropProduct, cropAmount);
            }
            if (plantedPlantData.seedPrefab != null)
            {
                ResourceInventory.Instance.AddResource(plantedPlantData.cropSeed, cropAmount);
            }

            // 2. แจกไอเทมปุ๋ยมูลสัตว์แถมพ่วงยัดเข้าคลังกระเป๋าจริง
            if (plantedPlantData.poopFertilizerProduct != null)
            {
                ResourceInventory.Instance.AddResource(plantedPlantData.poopFertilizerProduct, plantedPlantData.fertilizerAmount);
            }

            Debug.Log($"🌾 [Harvest]: เก็บเกี่ยวสำเร็จ! ได้ {plantedPlantData.cropProduct.itemName} x{cropAmount} และ {plantedPlantData.poopFertilizerProduct.itemName} x{plantedPlantData.fertilizerAmount}!");
        }

        // รีเซ็ตแปลงผักให้ว่างเปล่า
        plantedPlantData = null;
        SwitchStage(CropStage.Empty);
    }

    // 🔄 ฟังก์ชันสลับโมเดลกราฟิกตามเฟสการโตแบบเกรย์บ็อกซ์
    private void SwitchStage(CropStage newStage)
    {
        currentStage = newStage;

        if (currentVisualObject != null) Destroy(currentVisualObject);

        if (plantedPlantData == null) return;

        GameObject prefabToSpawn = null;
        switch (currentStage)
        {
            case CropStage.Seed: prefabToSpawn = plantedPlantData.seedPrefab; break;
            case CropStage.Growing: prefabToSpawn = plantedPlantData.growingPrefab; break;
            case CropStage.ReadyToHarvest: prefabToSpawn = plantedPlantData.fullyGrownPrefab; break;
        }

        if (prefabToSpawn != null)
        {
            // 1. เก็บค่า Scale ดั้งเดิมจากตัว Prefab ต้นฉบับไว้ก่อนมึงกอบ
            Vector3 originalPrefabScale = prefabToSpawn.transform.localScale;

            // 2. เสกไอเทมออกมาโดยตั้งให้แปลงผักเป็น Parent ตามเดิม
            currentVisualObject = Instantiate(prefabToSpawn, transform.position, Quaternion.identity, transform);

            // 3. 🟢 [หมัดฮุกแก้บั๊ก]: บังคับให้ localScale ของตัวลูกกลับไปเท่ากับค่าดั้งเดิมของ Prefab ชัวร์ 100%
            // วิธีนี้ต่อให้ตัวแปลงผักจะแบน ขนาดเพี้ยน หรือโดนบีบแค่ไหน ตัวพืชที่เจนออกมาก็จะทรงสูงสง่าตรงตามที่มึงตั้งไว้เป๊ะ ๆ!
            currentVisualObject.transform.localScale = originalPrefabScale;
        }
    }

    #region Destroy Product
    // 💀 ฟังก์ชันโดนค้างคาวทำลาย (สลายทิ้งจริง ห้ามเข้ากระเป๋าผู้เล่น!)
    public void TakeDamageFromBat()
    {
        currentPlotHealth--;
        Debug.LogWarning($"⚠ [CropPlots]: แปลง {gameObject.name} โดนค้างคาวแทะ! เลือดเหลือ {currentPlotHealth}/{maxPlotHealth}");

        if (currentPlotHealth <= 0)
        {
            DestroyCropCompletely();
        }
    }

    // 🌪 ล้างกระดานถอนรากถอนโคน ดินกลับเป็นสภาพแห้งโง่ ๆ
    private void DestroyCropCompletely()
    {
        currentStage = CropStage.Empty;
        currentPlotHealth = maxPlotHealth; // รีเซ็ตเลือดแปลงไว้รอรอบหน้า

        // วนลูปสแกนหา Object ลูกที่เป็นตัวโคลนผักงอกอยู่ แล้วสั่งลบทำลายทิ้งทันที
        foreach (Transform child in transform)
        {
            if (child.name.Contains("Clone") || child.name.Contains("GlandRice"))
            {
                Destroy(child.gameObject);
            }
        }
        Debug.LogError($"💀 [CropPlots]: บรรลัยแล้ว! แปลง {gameObject.name} โดนรุมแทะจนผักเน่าสลายร่าง 100%!");
    }
    #endregion
}