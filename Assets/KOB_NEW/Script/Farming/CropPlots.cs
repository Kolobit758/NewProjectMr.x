using UnityEngine;
using System.Collections; // 🟢 จำเป็นต้องใช้สำหรับ Coroutine

// 🟢 อัปเกรดสถานะให้ละเอียดขึ้นตามลำดับการปลูก
public enum CropStage { Empty, NeedsFertilizer, NeedsWater, Growing, ReadyToHarvest }

public class CropPlots : MonoBehaviour, ITaskable, IFertilizable // 🟢 เพิ่ม IFertilizable ให้ SetFertilizerType ทำงานจริง
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

    [Header("Dedicated Worker (จาก CropPlotsGroup)")]
    [Tooltip("true เมื่อแปลงนี้มีคนงานประจำถูกมอบหมายมาดูแลแล้ว (ผ่าน CropPlotsGroup.ApplySettingsToWorker) " +
             "ระบบ auto-call ยูนิตว่างงานตัวอื่นแบบสุ่มด้านล่างจะถูกปิด เพื่อไม่ให้แย่งงานกับคนงานประจำ")]
    public bool hasDedicatedWorker = false;

    [Header("Soil & Nutrient System (ระบบธาตุอาหารในดิน)")]
    public NutrientData currentSoilNutrients; // ธาตุอาหาร N, P, K สะสมในดินของแปลงนี้
    public SO_FertilizerData appliedFertilizer; // ปุ๋ยล่าสุดที่ใส่ลงแปลง
    public bool isMutated = false;              // สถานะกลายพันธุ์
    [Header("Visual Settings")]
    [Tooltip("ระยะยกตัวของโมเดลพืชขึ้นมาจากแปลง เพื่อไม่ให้จมดิน")]
    public float plantHeightOffset = 0.05f; // 🟢 สามารถปรับค่านี้ได้จาก Inspector เลยครับ


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
            // 🔒 ถ้ามีคนงานประจำดูแลแปลงนี้อยู่แล้ว (hasDedicatedWorker) ไม่ต้องสุ่มเรียกคนอื่นมาซ้ำ
            // ปล่อยให้ AutoCareTask ของคนงานประจำจัดการตามรอบเวลาของมันเอง
            if (!isBeingServiced && !hasDedicatedWorker)
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
                isBeingServiced = false;

                // ทำเสร็จแล้ว วิ่งไปทำแปลงอื่นต่อทันที!
                FinishServiceAndContinuePatrol(unit);
            }
            else
            {
                isBeingServiced = true;
                // สั่งไปเอาของ แล้วระบุให้กลับมาทำที่ plot นี้ต่อ
                unit.GoFetchItemAndReturn(fertilizerItemData, this);
            }
        }
        else if (currentStage == CropStage.NeedsWater)
        {
            if (unit.isCarrying && unit.carriedItem != null && unit.carriedItem == waterItemData)
            {
                unit.DropItemAtVault();
                SwitchStage(CropStage.Growing);
                isBeingServiced = false;

                // ทำเสร็จแล้ว วิ่งไปทำแปลงอื่นต่อทันที!
                FinishServiceAndContinuePatrol(unit);
            }
            else
            {
                isBeingServiced = true;
                unit.GoFetchItemAndReturn(waterItemData, this);
            }
        }
        else if (currentStage == CropStage.ReadyToHarvest)
        {
            HarvestCrop();
            FinishServiceAndContinuePatrol(unit);
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
            if (unit != null && unit.currentState == UnitBehavior.Idle)
            {
                Debug.Log($"🤖 [Auto] แปลงผักเรียก {unit.name} มารดน้ำ!");
                // 🔒 Lock ก่อน เพื่อกันเรียกซ้ำสองคนก่อนยูนิตจะมาถึง
                isBeingServiced = true;
                unit.MoveTo(GetInteractionPoint(), this);
                break; // เรียกได้ตัวนึงก็พอแล้ว ออกลูป
            }
        }
    }


    // ตัวอย่างจุดเรียกใช้งานเมื่อทำภารกิจเสร็จใน CropPlots.cs (เช่น ใน OnUnitInteract หรือหลังเก็บเกี่ยว)
    public void FinishServiceAndContinuePatrol(UnitBase unit)
    {
        if (unit == null) return;

        isBeingServiced = false;

        // รีเซ็ตเส้นทางและสั่งให้ยูนิตกลับเข้าสู่โหมดเดินตรวจแปลง/ทำฟาร์มต่อ
        unit.agent.isStopped = false;
        unit.CommandFarmingPatrol();
    }

    public void HarvestCrop()
    {
        if (currentStage != CropStage.ReadyToHarvest || plantedPlantData == null) return;

        // 🟢 เรียกใช้ระบบคำนวณคุณภาพธาตุอาหาร NPK และการกลายพันธุ์แทนการแอดของตรงๆ
        EvaluateHarvestQuality(plantedPlantData);

        Debug.Log($"🌾 [Harvest]: เก็บเกี่ยวแปลงเสร็จสิ้น!");

        // เคลียร์ค่าแปลงเตรียมปลูกรอบถัดไป
        plantedPlantData = null;
        appliedFertilizer = null;
        isMutated = false;
        currentSoilNutrients = new NutrientData(); // ล้างธาตุอาหารในดินรอบใหม่
        SwitchStage(CropStage.Empty);
        isBeingServiced = false;
    }

    private void SwitchStage(CropStage newStage)
    {
        currentStage = newStage;

        if (currentVisualObject != null) Destroy(currentVisualObject);
        if (plantedPlantData == null) return;

        GameObject prefabToSpawn = null;
        switch (currentStage)
        {
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

            // 🟢 ใช้ค่าจากตัวแปร plantHeightOffset ที่ปรับตั้งค่าได้ใน Inspector
            Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y + plantHeightOffset, transform.position.z);

            currentVisualObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, transform);

            currentVisualObject.transform.localScale = originalPrefabScale;
        }
    }
    // ตัวอย่างลอจิกการคำนวณคุณภาพตอนเก็บเกี่ยว
    // 🟢 ฟังก์ชันรับปุ๋ย (รองรับทั้งผู้เล่นกดใช้ผ่าน Manager หรือยูนิตขนมาส่ง)
    public bool ApplyFertilizer(SO_FertilizerData fertilizer)
    {
        if (currentStage == CropStage.Empty || currentStage == CropStage.ReadyToHarvest)
        {
            Debug.LogWarning("แปลงนี้ยังไม่พร้อมรับปุ๋ย!");
            return false;
        }

        appliedFertilizer = fertilizer;

        // 1. เติมธาตุอาหาร N, P, K ของปุ๋ยสะสมลงไปในดินของแปลงนี้
        currentSoilNutrients.nitrogen += fertilizer.providedNutrients.nitrogen;
        currentSoilNutrients.phosphorus += fertilizer.providedNutrients.phosphorus;
        currentSoilNutrients.potassium += fertilizer.providedNutrients.potassium;

        Debug.Log(($"🧪 ใส่ปุ๋ย {fertilizer.itemName} สำเร็จ! ธาตุในดินตอนนี้ -> N:{currentSoilNutrients.nitrogen} P:{currentSoilNutrients.phosphorus} K:{currentSoilNutrients.potassium}"));

        // 2. สุ่มเช็คโอกาสเกิดการกลายพันธุ์ (Mutation) ตามค่าของปุ๋ย
        float roll = Random.Range(0f, 100f);
        if (roll <= fertilizer.mutationChance && plantedPlantData != null && plantedPlantData.mutatedProduct != null)
        {
            isMutated = true;
            Debug.Log($"✨ [Mutation!]: ปุ๋ยทำปฏิกิริยาสำเร็จ พืชในแปลงนี้เกิดการกลายพันธุ์!");
        }

        return true;
    }

    // 🟢 ฟังก์ชันประเมินคุณภาพตอนเก็บเกี่ยว
    public void EvaluateHarvestQuality(SO_PlantData plantData)
    {
        if (plantData == null) return;

        // ถ้าเกิดการกลายพันธุ์สำเร็จ ให้ผลผลิตกลายพันธุ์ทันที
        if (isMutated && plantData.mutatedProduct != null)
        {
            GivePlayerProduct(plantData.mutatedProduct);
            Debug.Log("✨ พืชกลายพันธุ์สำเร็จ! ได้ผลผลิตพิเศษหายาก");
            return;
        }

        // คำนวณความต่างของธาตุอาหารที่ดินมีเทียบกับที่พืชชอบ (Ideal)
        float diffN = Mathf.Abs(currentSoilNutrients.nitrogen - plantData.idealNutrients.nitrogen);
        float diffP = Mathf.Abs(currentSoilNutrients.phosphorus - plantData.idealNutrients.phosphorus);
        float diffK = Mathf.Abs(currentSoilNutrients.potassium - plantData.idealNutrients.potassium);

        float totalError = diffN + diffP + diffK;

        // เช็คเกรดพรีเมียม (ธาตุอาหารอยู่ในช่วงที่พืชยอมรับได้ toleranceRange)
        if (totalError <= plantData.toleranceRange * 3)
        {
            GivePlayerProduct(plantData.premiumProduct != null ? plantData.premiumProduct : plantData.cropProduct);
            Debug.Log("🌟 ยอดเยี่ยม! ธาตุอาหาร NPK ลงตัวเป๊ะ ได้ผลผลิตเกรดพรีเมียม!");
        }
        else
        {
            // โตแบบธรรมดา
            GivePlayerProduct(plantData.cropProduct);
            Debug.Log("🌱 พืชเติบโตตามปกติ (ธาตุอาหารยังไม่ค่อยลงตัว ลองปรับสูตรปุ๋ยดูคราวหน้า)");
        }
    }

    // 🟢 ฟังก์ชันแจกจ่ายผลผลิตเข้ากระเป๋า
    private void GivePlayerProduct(SO_ItemData productToGive)
    {
        if (productToGive == null || ResourceInventory.Instance == null) return;

        int cropAmount = Random.Range(plantedPlantData.minProductAmount, plantedPlantData.maxProductAmount + 1);
        ResourceInventory.Instance.AddResource(productToGive, cropAmount);

        // คืนเมล็ดหรือของพลอยได้อื่นๆ (ถ้ามี)
        if (plantedPlantData.cropSeed != null)
            ResourceInventory.Instance.AddResource(plantedPlantData.cropSeed, cropAmount);

        if (plantedPlantData.poopFertilizerProduct != null)
            ResourceInventory.Instance.AddResource(plantedPlantData.poopFertilizerProduct, plantedPlantData.fertilizerAmount);
    }

    public void OnUnitExit(UnitBase unit)
    {
        // 🔒 เมื่อยูนิตถูก cancel กลางคัน ต้อง release lock ให้แปลงสามารถเรียกคนใหม่ได้
        isBeingServiced = false;
    }

    // 🟢 จำเป็นต้องมีสำหรับ ITaskable
    public Vector3 GetInteractionPoint() => transform.position;

    /// <summary>
    /// 🟢 [แก้บั๊ก] Implement ของจริงให้ IFertilizable — เดิม interface นี้ประกาศไว้ใน UnitBase.cs
    /// แต่ CropPlots ไม่เคย implement มันเลย ทำให้ (task.targetPlot as IFertilizable)?.SetFertilizerType(...)
    /// ใน UnitBase.TickAutoCareTasks() cast ไม่ผ่านและไม่ทำอะไรเลยแบบเงียบๆ ผลคือปุ๋ยที่เลือกจาก
    /// CropPlotsGroup/แผง UI ไม่เคยถูกส่งมาถึงแปลงจริง สุดท้ายแปลงใช้ fertilizerItemData ที่ตั้งค้างไว้
    /// ใน Inspector ของตัวเองแทน (เช่น "Fertilizer01" ที่ไม่มีในคลัง)
    ///
    /// ตอนนี้พอ implement แล้ว ทุกครั้งที่คนงานประจำจะไปใส่ปุ๋ยตามรอบ AutoCareTask ระบบจะเซ็ต
    /// fertilizerItemData ของแปลงนี้ให้ตรงกับปุ๋ยที่ตั้งไว้ในกลุ่มก่อนเสมอ
    /// </summary>
    public void SetFertilizerType(SO_ItemData fertilizerItem)
    {
        if (fertilizerItem == null) return;
        fertilizerItemData = fertilizerItem;
    }

    #region Destroy Product
    // 🟢 ฟังก์ชันให้แปลงผักรับดาเมจจากศัตรูที่เข้ามาบุกแทะ
    public void TakeDamageFromEnemy(int damageAmount)
    {
        if (currentStage == CropStage.Empty) return; // ถ้าแปลงว่างอยู่แล้ว ไม่ต้องตีซ้ำ

        currentPlotHealth -= damageAmount;
        Debug.LogWarning($"⚠️ [CropPlots]: แปลงผักโดนศัตรูโจมตี! เลือดแปลงเหลือ {currentPlotHealth}/{maxPlotHealth}");

        // สามารถเพิ่มเอฟเฟกต์สั่นสะดุ้งตรงนี้ได้ถ้าต้องการ

        if (currentPlotHealth <= 0)
        {
            DestroyCropCompletely();
        }
    }

    // 💀 ฟังก์ชันทำลายพืชผลทิ้งเมื่อเลือดแปลงหมด
    private void DestroyCropCompletely()
    {
        Debug.LogError($"💥 [CropPlots]: แปลงผักถูกทำลายเสียหายย่อยยับจนเกลี้ยงแปลงแล้ว!");

        // เคลียร์ค่าข้อมูลพืชทั้งหมด
        plantedPlantData = null;
        appliedFertilizer = null;
        isMutated = false;
        currentSoilNutrients = new NutrientData();
        currentPlotHealth = maxPlotHealth; // รีเซ็ตเลือดแปลงเตรียมไว้ปลูกรอบใหม่
        isBeingServiced = false;

        // ลบโมเดลพืช 3D บนแปลงทิ้ง
        if (currentVisualObject != null)
        {
            Destroy(currentVisualObject);
        }

        // สลับสถานะกลับเป็นแปลงว่าง
        SwitchStage(CropStage.Empty);
    }
    #endregion


    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(transform.position, 20f);
    }
}