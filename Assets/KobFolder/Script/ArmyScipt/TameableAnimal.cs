// using UnityEngine;

// public class TameableAnimal : MonoBehaviour
// {
//     [Header("Tame Template")]
//     public UnitDataSO unitTemplate;

//     [Header("Interaction Settings")]
//     public float tameRange = 3f;
//     public KeyCode tameKey = KeyCode.T;
//     public float hpPercentToTame = 0.2f;

//     private bool isTamed = false;

//     // ✅ สัตว์ป่าตัวนี้จะถือข้อมูลที่สุ่มแล้วไว้ตั้งแต่เกิด
//     public UnitInstance MyRuntimeStats { get; private set; }
//     public CharacterStats characterStats{ get; private set; }

//     private void Awake()
//     {
//         if (unitTemplate != null)
//         {
//             // ✅ สุ่มสร้างข้อมูลสเตตัสรายตัวตั้งแต่เกิด (Awake) ทันที
//             MyRuntimeStats = new UnitInstance(unitTemplate);
//             characterStats = GetComponent<CharacterStats>();
//         }
//     }

//     // 📄 ปรับแก้ภายในสคริปต์ TameableAnimal.cs ตรงฟังก์ชัน Start นะครับเพื่อน
//     private void Start()
//     {
//         AnimalStatsManager statsManager = GetComponent<AnimalStatsManager>();

//         if (statsManager != null)
//         {
//             // 🟢 FIX SUCCESS: เช็คก่อนว่า ณ ตอนนี้ตัวมันมีไอดีแท้ (ActiveUnitData) ผูกมาจากคลังหรือยัง?
//             // ถ้ามีอยู่แล้ว (เช่น เกิดจากการจัดทัพของ ArmyController) -> ให้ข้ามไปเลย ห้ามเจดไอดีใหม่ทับเด็ดขาด!
//             if (statsManager.ActiveUnitData != null && !string.IsNullOrEmpty(statsManager.ActiveUnitData.uniqueId))
//             {
//                 Debug.Log($"[TameableAnimal] 🔒 ปลอดภัย: ยูนิตตัวนี้มีรหัสแท้อยู่แล้ว ({statsManager.ActiveUnitData.uniqueId}) ข้ามการสร้างไอดีซ้ำรอบสอง!");
//             }
//             else
//             {
//                 // 💡 โค้ดสร้างไอเทม/สุ่มไอดีเก่าของเพื่อน (ปล่อยทำงานเฉพาะตอนที่เป็นสัตว์ป่าเกิดตามธรรมชาติในฉาก)
//                 // ตัวอย่างเช่น:
//                 // UnitInstance wildInstance = new UnitInstance(myTemplateSO);
//                 // wildInstance.uniqueId = System.Guid.NewGuid().ToString();
//                 // statsManager.SetupFromInstance(wildInstance);
//             }
//         }
//     }

//     private void Update()
//     {
//         if (isTamed) return;

//         if (Input.GetKeyDown(tameKey))
//         {
//             GameObject player = GameObject.FindGameObjectWithTag("Player");
//             if (player != null)
//             {
//                 float distance = Vector3.Distance(transform.position, player.transform.position);
//                 int maxHP = MyRuntimeStats.maxHP;
//                 if (distance <= tameRange && characterStats.currentHP <= 20)
//                 {
//                     Debug.Log("Try to tame");
//                     Tame();
//                 } 
//             }
//             else
//             {
//                 PlayerActionMovement playerMovement = FindAnyObjectByType<PlayerActionMovement>();
//                 if (playerMovement != null)
//                 {
//                     float distance = Vector3.Distance(transform.position, playerMovement.transform.position);
//                     int maxHP = MyRuntimeStats.maxHP;
//                     if (distance <= tameRange && characterStats.currentHP <= ((hpPercentToTame) * maxHP))
//                     {
//                         Tame();
//                     } 
//                 }
//             }
//         }
//     }

//     public void Tame()
//     {
//         if (isTamed) return;
//         isTamed = true;

//         if (AnimalInventory.Instance == null)
//         {
//             Debug.LogError("TamedUnitsManager not found! Cannot add tamed unit.");
//             return;
//         }

//         // ✅ ดึงค่า MyRuntimeStats ที่สุ่มคาไว้ตั้งแต่เกิด ส่งเข้าคลังกองทัพโดยตรง (ไม่มีการสุ่มใหม่)
//         AnimalInventory.Instance.AddUnitToInventory(MyRuntimeStats);

//         Debug.Log($"Successfully tamed {MyRuntimeStats.customName}! Stats grabbed from wild -> HP: {MyRuntimeStats.maxHP}, ATK: {MyRuntimeStats.attackDamage}");

//         // 🟢 บังคับให้ SaveSystem เซฟทันที เพราะเราเปลี่ยนข้อมูลสัตว์เลี้ยงแล้ว
//         if (SaveLoadManager.Instance != null)
//         {
//             SaveLoadManager.Instance.SaveGame(forceSave: true);
//             Debug.Log("[Save] 💾 บันทึกข้อมูลการจับสัตว์ลงเครื่องสำเร็จ!");
//         }

//         Destroy(gameObject);
//     }

//     private void OnDrawGizmosSelected()
//     {
//         Gizmos.color = Color.cyan;
//         Gizmos.DrawWireSphere(transform.position, tameRange);
//     }
// }

using UnityEngine;

public class TameableAnimal : MonoBehaviour
{
    [Header("Tame Template")]
    public UnitDataSO unitTemplate;

    [Header("Taming Preferences")]
    [Tooltip("ประเภทหรือรายชื่อไอเทมอาหารที่สัตว์ตัวนี้ชอบ (เช่น ผัก, ผลไม้)")]
    public SO_ItemData[] acceptedFoodTypes;

    private bool isTamed = false;

    public UnitInstance MyRuntimeStats { get; private set; }
    public CharacterStats characterStats { get; private set; }

    private void Awake()
    {
        if (unitTemplate != null)
        {
            MyRuntimeStats = new UnitInstance(unitTemplate);
            characterStats = GetComponent<CharacterStats>();
        }
    }

    private void Start()
    {
        AnimalStatsManager statsManager = GetComponent<AnimalStatsManager>();
        if (statsManager != null)
        {
            if (statsManager.ActiveUnitData != null && !string.IsNullOrEmpty(statsManager.ActiveUnitData.uniqueId))
            {
                // มีข้อมูลอยู่แล้ว ข้ามการสร้างใหม่
            }
        }
    }

    // 🟢 ฟังก์ชันถูกเรียกเมื่อมี "อาหาร" ถูกขว้างมาโดนตัวหรือตกใกล้ๆ
    public void TryTameWithFood(SO_ItemData foodItem)
    {
        if (isTamed) return;

        // เช็คว่าอาหารชิ้นนี้เป็นของที่สัตว์ตัวนี้กิน/ชอบไหม
        bool isLikedFood = false;
        foreach (var food in acceptedFoodTypes)
        {
            if (food == foodItem || (food != null && food.itemName == foodItem.itemName))
            {
                isLikedFood = true;
                break;
            }
        }

        if (isLikedFood)
        {
            Debug.Log($"💖 สัตว์ป่ากิน {foodItem.itemName} และรู้สึกเชื่องแล้ว!");
            Tame();
        }
        else
        {
            Debug.Log($"😒 สัตว์ป่าเมิน {foodItem.itemName} มันไม่ชอบกิน!");
        }
    }

    public void Tame()
    {
        if (isTamed) return;
        isTamed = true;

        if (AnimalInventory.Instance == null)
        {
            Debug.LogError("AnimalInventory not found! Cannot add tamed unit.");
            return;
        }

        // 1. Add ข้อมูลเข้าไปเก็บในคลังกองทัพตามระบบเดิมของคุณ
        AnimalInventory.Instance.AddUnitToInventory(MyRuntimeStats);
        Debug.Log($"Successfully tamed {MyRuntimeStats.customName}!");

        // 2. 🟢 ทำการ Instantiate ยูนิตจริงขึ้นมาตรงตำแหน่งที่สัตว์ป่ายืนอยู่ก่อนตาย!
        if (unitTemplate != null && unitTemplate.unitPrefab != null)
        {
            // ดึงตำแหน่งและองศาการหันหน้าของสัตว์ป่าตัวนี้
            Vector3 spawnPos = transform.position;
            Quaternion spawnRot = transform.rotation;

            // สร้าง Prefab ยูนิตจริง (อ้างอิงจาก unitTemplate.unitPrefab หรือถ้ามีช่อง Folder เก็บยูนิต สามารถใส่เพิ่มได้)
            GameObject tamedUnit = Instantiate(unitTemplate.unitPrefab, spawnPos, spawnRot);

            // 3. ปรับแต่งค่าสคริปต์ (เปิด UnitBase และปิด AI สัตว์ป่า ตามสไตล์การ Setup ของคุณ)
            UnitBase unitBaseComp = tamedUnit.GetComponent<UnitBase>();
            if (unitBaseComp != null)
            {
                unitBaseComp.enabled = true;
            }

            AnimalAIController aiController = tamedUnit.GetComponent<AnimalAIController>();
            if (aiController != null)
            {
                aiController.enabled = false;
            }

            // 4. เซ็ต Tag และ Layer ให้เหมือนกับตอนที่ระบบ SpawnAllUnit ทำงาน
            tamedUnit.tag = "Unit";

            // กำหนด Layer (สามารถปรับชื่อ Layer ตรงนี้ให้ตรงกับที่คุณตั้งใน SpawnAllUnit ได้เลย เช่น "Unit" หรือตัวแปร string ที่ต้องการ)
            string targetLayerName = "Unit"; // หรือกำหนดเป็น [Header] ให้เลือกใน Inspector ได้
            tamedUnit.layer = LayerMask.NameToLayer(targetLayerName);

            // 5. ลงทะเบียนเข้าสู่ระบบควบคุม RTS ทันที (RTS_movement)
            if (RTS_movement.instance != null && unitBaseComp != null)
            {
                RTS_movement.instance.allUnits.Add(unitBaseComp);
            }

            Debug.Log($"✨ แปลงร่างสัตว์ป่าเป็นยูนิตกองทัพพร้อมใช้งานที่ตำแหน่ง {spawnPos} สำเร็จ!");
        }
        else
        {
            Debug.LogWarning("⚠️ unitTemplate หรือ unitPrefab เป็น Null ไม่สามารถ Instantiate ร่างจริงออกมาได้!");
        }

        // 6. บันทึกเกมทันทีหลังจับสำเร็จ
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGame(forceSave: true);
        }

        // 7. ทำลายร่างสัตว์ป่าตัวเก่าทิ้ง
        Destroy(gameObject);
    }
}