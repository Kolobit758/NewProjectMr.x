using UnityEngine;
using System;
using System.Collections.Generic; // 🌟 เพิ่มตรงนี้เพื่อใช้ List

public class IslandController : MonoBehaviour
{
    [Header("Island Profile")]
    public string islandName = "Island";
    public bool isHomeIsland = false;
    public bool isConnectedToNet = false;
    public bool isReceivingEnergy = false;

    [Header("Energy Stats")]
    public float currentEnergy = 50f;
    public float maxEnergy = 100f;
    public float energyProductionRate = 5f;
    public float energyConsumptionRate = 2f;
    public float energyReceiveRate = 8f;

    [Header("Visual Feedback (Greybox)")]
    [SerializeField] private MeshRenderer islandRenderer;
    [SerializeField] private Color fullEnergyColor = Color.green;
    [SerializeField] private Color emptyEnergyColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("🌟 Crisis Settings (ระบบทำลายสิ่งปลูกสร้าง)")]
    public List<GameObject> placedStructures = new List<GameObject>(); // รายชื่อตึก/พืชบนเกาะนี้


    private bool isWorldFrozen = false;
    public Action<float> OnEnergyChanged;
    public Action<bool> OnFreezeStatusChanged;

    public event Action<bool> OnIslandPowerStateChanged;
    private bool isIslandPowerOn = true;
    // 🟢 แปะตัวแปรคูลดาวน์สุ่มทำลายของไว้ตรงโซนประกาศตัวแปรด้านบน หรือบนหัวฟังก์ชันเลยมึง
    [Header("Ecosystem Disaster (ระบบสุ่มทำลายของกอบ)")]

    public float destructionInterval = 4f; // โดนสุ่มทำลายทุกๆ 4 วินาทีที่ไฟหมด
    private float destructionTimer = 0f;


    void Update()
    {
        if (isWorldFrozen) return;

        if (isHomeIsland)
        {
            // 🟢 [แก้ไขจุดตาย]: ลบบรรทัด currentEnergy += energyProductionRate * Time.deltaTime; ออกไปเลยมึง!
            // ปล่อยให้ช่องนี้ว่างไว้ เพื่อให้ MotherTreeController เป็นคนคุมจังหวะยัดไฟเข้ามาทีละก้อนตามอารมณ์เอง
            // currentEnergy += energyProductionRate * Time.deltaTime;
        }
        else
        {
            // เกาะลูกรอบนอกยัง CONSUME หรือ RECEIVE พลังงานตามเดิมปกติมึงกอบ
            if (isConnectedToNet && isReceivingEnergy)
            {
                currentEnergy += energyReceiveRate * Time.deltaTime;
            }
            else
            {
                currentEnergy -= energyConsumptionRate * Time.deltaTime;
            }

            if (currentEnergy <= 0f)
            {
                HandleEnergyCrisis();
            }
            else
            {
                destructionTimer = 0f;
            }
        }

        // ล็อกขอบเขตและอัปเดตสี/UI ตามปกติ
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
        float energyPercent = currentEnergy / maxEnergy;

        OnEnergyChanged?.Invoke(energyPercent);

        if (islandRenderer != null)
        {
            islandRenderer.material.color = Color.Lerp(emptyEnergyColor, fullEnergyColor, energyPercent);
        }
    }

    // 💀 ฟังก์ชันคำนวณเวลาสุ่มทำลายเมื่อวิกฤต
    private void HandleEnergyCrisis()
    {
        // เกาะหลัก (Home Island) จะไม่โดนสุ่มทำลายจนพัง (แค่เกมหยุดทำงานตามที่ตั้งเป้าไว้)
        if (isHomeIsland) return;

        destructionTimer += Time.deltaTime;

        if (destructionTimer >= destructionInterval)
        {
            destructionTimer = 0f;
            DestroyRandomStructureOnIsland();
        }
    }

    // 💣 ลอจิกสุ่มดึงของบนเกาะนี้ออกไประเบิดทิ้ง
    private void DestroyRandomStructureOnIsland()
    {
        if (placedStructures.Count > 0)
        {
            // ทำการกรองเอาพวกที่เป็น Null ออกไปก่อน (เผื่อมีไอเทมโดนลบด้วยวิธีอื่น)
            placedStructures.RemoveAll(item => item == null);

            if (placedStructures.Count == 0) return;

            int randomIndex = UnityEngine.Random.Range(0, placedStructures.Count);
            GameObject targetToDestroy = placedStructures[randomIndex];

            if (targetToDestroy != null)
            {
                placedStructures.RemoveAt(randomIndex);

                // 💡 ตรงนี้ถ้ามี Effect ควัน/ไฟระเบิด ค่อยเอามา Instantiate ใส่ตรงนี้ได้เลย
                Destroy(targetToDestroy);

                Debug.LogWarning($"💥 [ภัยพิบัติเกาะ {islandName}]: พลังงานหมด! {targetToDestroy.name} ถูกทำลายเนื่องจากระบบล่มสลาย!");
            }
        }
    }

    private void SetIslandPowerState(bool state)
    {
        if (isIslandPowerOn == state) return;
        isIslandPowerOn = state;

        OnIslandPowerStateChanged?.Invoke(isIslandPowerOn);

        Debug.LogWarning(isIslandPowerOn
            ? $"☀️ [เกาะ {islandName}]: มีพลังงานแล้ว! สิ่งก่อสร้างเริ่มทำงาน"
            : $"⚡ [เกาะ {islandName}]: พลังงานหมดเกลี้ยง! สิ่งก่อสร้างหยุดทำงานชั่วคราว");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isHomeIsland) return;

        BranchSegment segment = other.GetComponent<BranchSegment>();
        if (segment == null) segment = other.GetComponentInParent<BranchSegment>();

        if (segment != null)
        {
            isConnectedToNet = true;
            Debug.Log($"✨ [ระบบเครือข่าย]: กิ่งไม้ต่อสัญญาณเข้าสู่เกาะ {islandName} สำเร็จ!");
        }
    }

    public void ToggleEnergySupply()
    {
        if (!isConnectedToNet)
        {
            Debug.LogWarning($"❌ เกาะ {islandName} ยังไม่มีกิ่งไม้เลื้อยมาเชื่อมต่อ! เปิดวาล์วไม่ได้มึง");
            return;
        }

        isReceivingEnergy = !isReceivingEnergy;
        Debug.Log($"🔌 เกาะ {islandName} เปลี่ยนสถานะวาล์วพลังงานเป็น: {(isReceivingEnergy ? "🟢 เปิดรับพลังงาน" : "🔴 ปิดรับพลังงาน")}");
    }

    public void SetFreezeState(bool freeze)
    {
        isWorldFrozen = freeze;
        OnFreezeStatusChanged?.Invoke(freeze);
        if (freeze && islandRenderer != null) islandRenderer.material.color = emptyEnergyColor;
    }

    #region Energy

    public void IncreaseMaxEnergy(float amount)
    {
        maxEnergy += amount;
        currentEnergy += amount;
        OnEnergyChanged?.Invoke(currentEnergy / maxEnergy);
    }


    // 🟢 [เพิ่มฟังก์ชันนี้เข้าไปใน IslandController.cs]
    // เอาไว้ให้เครื่องจักรต่างๆ เรียกสั่งหักกระแสไฟทันทีเมื่อทำงานเสร็จ
    public bool RequestImmediateEnergy(float amountNeeded)
    {
        // ถ้าเกาะถูกแช่แข็ง หรือไฟหมดอยู่แล้ว จ่ายให้ไม่ได้จ้า
        if (isWorldFrozen || currentEnergy <= 0f) return false;

        // เช็กว่าพลังงานบนเกาะมีพอให้กระชากไปใช้งานไหม
        if (currentEnergy >= amountNeeded)
        {
            currentEnergy -= amountNeeded; // ⚡ หักไฟออกทันทีแบบก้อนใหญ่!
            currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);

            // อัปเดต UI หน้าจอเกาะทันที
            float energyPercent = currentEnergy / maxEnergy;
            OnEnergyChanged?.Invoke(energyPercent);

            return true; // จ่ายไฟสำเร็จ เครื่องจักรทำงานต่อได้
        }

        return false; // ไฟไม่พอจ่าย!
    }
    #endregion
}