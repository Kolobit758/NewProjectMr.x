using UnityEngine;
using TMPro;
using UnityEngine.UI; // 🟢 เพิ่มบรรทัดนี้ไว้บนสุดของโค้ดเพื่อใช้งานคลาส Image

public class MachineStructure : MonoBehaviour
{
    [Header("Energy & Production Setup")]
    public SO_ItemData resourceItem; // แร่ที่จะผลิต
    public int amountPerCycle = 5;   // จำนวนแร่ที่ผลิตได้ต่อรอบ
    public float energyCostPerCycle = 15f; // ค่าไฟที่ต้องจ่ายเมื่อผลิตเสร็จ
    public float productionCycleTime = 2f; // เวลาการผลิตต่อรอบ (วินาที)

    [Header("Storage (คลังเก็บของประจำเครื่อง)")]
    public int maxStorage = 50;      // เก็บได้สูงสุดเท่าไหร่ก่อนเครื่องจะเต็มแล้วหยุดผลิต
    public int currentStoredAmount = 0; // จำนวนของที่ค้างอยู่ในเครื่อง ณ ตอนนี้

    private float cycleTimer = 0f;
    public IslandController myIsland;
    public bool isPowerOn = true;
    public bool isPlayerInArea;


    public Image icon;
    public TMP_Text resourceName;
    public TMP_Text resourceCount;

    void Start()
    {
        if (myIsland != null)
        {
            myIsland.OnIslandPowerStateChanged += HandlePowerStateChanged;
            isPowerOn = (myIsland.currentEnergy > 0f);

            if (resourceItem.itemIcon != null)
            {
                icon.sprite = resourceItem.itemIcon;
            }
            if (resourceItem.itemName != null)
            {
                resourceName.text = resourceItem.itemName;
            }

            // 🟢 เรียกฟังก์ชันแสดงผลครั้งแรกตอนเครื่องจักรตื่น
            UpdateMachineUI();
        }
    }
    private void UpdateMachineUI()
    {
        if (resourceCount != null)
        {
            resourceCount.text = $"{currentStoredAmount}/{maxStorage}";
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && isPlayerInArea)
        {
            GetResource();

            // 🟢 เปลี่ยนมาใช้ตัว Force Direct สั่งรีเฟรชทั้งจอทันที
            if (ResourceInventory.Instance != null)
            {
                ResourceInventory.Instance.ForceRefreshAllUI();
            }
        }

        // ⚡ วาล์วตัดไฟของเกาะ: ถ้าไม่มีไฟ ให้หยุดแค่ระบบผลิตแร่ถัดจากนี้ไป
        if (!isPowerOn) return;

        // 🛡️ เช็กดัก: ถ้าของในเครื่องเต็มคลังแล้ว ให้หยุดผลิตชั่วคราวจนกว่าผู้เล่นจะมาเก็บ
        if (currentStoredAmount >= maxStorage)
        {
            cycleTimer = 0f;
            return;
        }

        cycleTimer += Time.deltaTime;

        if (cycleTimer >= productionCycleTime)
        {
            cycleTimer = 0f;
            TryExecuteProduction();
        }
    }

    private void TryExecuteProduction()
    {
        if (myIsland == null || resourceItem == null) return;

        bool hasEnoughEnergy = myIsland.RequestImmediateEnergy(energyCostPerCycle);

        if (hasEnoughEnergy)
        {
            currentStoredAmount += amountPerCycle;
            currentStoredAmount = Mathf.Clamp(currentStoredAmount, 0, maxStorage);

            // 🟢 [อัปเดตจุดที่ 1]: ผลิตแร่ได้สำเร็จ -> สั่งอัปเดตตัวเลขบนเครื่องทันที!
            UpdateMachineUI();

            Debug.Log($"[Machine] 🏭 {gameObject.name} จ่ายไฟสำเร็จ! ผลิต {resourceItem.itemName} สะสมในเครื่องค้างไว้: {currentStoredAmount}/{maxStorage}");
        }
        else
        {
            Debug.LogWarning($"[Machine] ❌ {gameObject.name} ไฟบนเกาะไม่พอจ่าย! การผลิตหยุดชะงัก");
        }
    }

    public void GetResource()
    {
        if (currentStoredAmount > 0)
        {
            if (ResourceInventory.Instance != null)
            {
                ResourceInventory.Instance.AddResource(resourceItem, currentStoredAmount);

                Debug.Log($"[Harvest] 🎒 เก็บเกี่ยว {resourceItem.itemName} x{currentStoredAmount} ชิ้นเข้ากระเป๋าตัวเองแล้ว!");

                currentStoredAmount = 0; // เคลียร์ของในเครื่องเป็นศูนย์

                // 🟢 [อัปเดตจุดที่ 2]: ผู้เล่นกด E สูบแร่ออกไปหมดคลัง -> สั่งรีเซ็ตตัวเลขบนตึกกลับเป็น 0 ทันที!
                UpdateMachineUI();
            }
        }
        else
        {
            Debug.Log("[Machine] เครื่องนี้ยังไม่มีผลผลิตให้เก็บจ้า");
        }
    }

    // 🟢 [ฟังก์ชันไม้ตาย]: เอาไว้ให้ผู้เล่นเดินมาเอาเมาส์คลิกที่ตัวตึกนี้เพื่อเก็บของเข้ากระเป๋าหลัก!
    // private void OnMouseDown()
    // {
    //     // 🛡️ เช็กดักระยะห่างระหว่างตัวผู้เล่นกับเครื่องจักร (ป้องกันผู้เล่นคลิกข้ามแมพสไนเปอร์เก็บของ)
    //     GameObject player = GameObject.FindGameObjectWithTag("Player");
    //     if (player != null)
    //     {
    //         float distance = Vector3.Distance(transform.position, player.transform.position);
    //         if (distance > 5f) // ถ้าอยู่ห่างเกิน 5 เมตร ไม่ยอมให้เก็บ
    //         {
    //             Debug.LogWarning("[Machine] 🏃 เดินเข้าไปใกล้เครื่องจักรอีกหน่อยเพื่อเก็บของ!");
    //             return;
    //         }
    //     }

    //     // ถ้ามีของค้างอยู่ในเครื่อง ให้ขนเข้ากระเป๋าผู้เล่นเลย!
    //     if (currentStoredAmount > 0)
    //     {
    //         if (ResourceInventory.Instance != null)
    //         {
    //             // โอนเข้ากระเป๋าหลักของผู้เล่นโดยตรง
    //             ResourceInventory.Instance.AddResource(resourceItem, currentStoredAmount);

    //             Debug.Log($"[Harvest] 🎒 เก็บเกี่ยว {resourceItem.itemName} x{currentStoredAmount} ชิ้นเข้ากระเป๋าตัวเองแล้ว!");

    //             currentStoredAmount = 0; // เคลียร์ของในเครื่องเป็นศูนย์เพื่อเริ่มสับถังผลิตใหม่!
    //         }
    //     }
    //     else
    //     {
    //         Debug.Log("[Machine] เครื่องนี้ยังไม่มีผลผลิตให้เก็บจ้า");
    //     }
    // }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            isPlayerInArea = true;
        }

    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            isPlayerInArea = false;
        }
    }


    private void HandlePowerStateChanged(bool hasPower)
    {
        isPowerOn = hasPower;
    }

    private void OnDestroy()
    {
        if (myIsland != null)
        {
            myIsland.OnIslandPowerStateChanged -= HandlePowerStateChanged;
        }
    }


}