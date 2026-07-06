using UnityEngine;

public class TameableAnimal : MonoBehaviour
{
    [Header("Tame Template")]
    public UnitDataSO unitTemplate;

    [Header("Interaction Settings")]
    public float tameRange = 3f;
    public KeyCode tameKey = KeyCode.T;
    public float hpPercentToTame = 0.2f;

    private bool isTamed = false;

    // ✅ สัตว์ป่าตัวนี้จะถือข้อมูลที่สุ่มแล้วไว้ตั้งแต่เกิด
    public UnitInstance MyRuntimeStats { get; private set; }
    public CharacterStats characterStats{ get; private set; }

    private void Awake()
    {
        if (unitTemplate != null)
        {
            // ✅ สุ่มสร้างข้อมูลสเตตัสรายตัวตั้งแต่เกิด (Awake) ทันที
            MyRuntimeStats = new UnitInstance(unitTemplate);
            characterStats = GetComponent<CharacterStats>();
        }
    }

    // 📄 ปรับแก้ภายในสคริปต์ TameableAnimal.cs ตรงฟังก์ชัน Start นะครับเพื่อน
    private void Start()
    {
        AnimalStatsManager statsManager = GetComponent<AnimalStatsManager>();

        if (statsManager != null)
        {
            // 🟢 FIX SUCCESS: เช็คก่อนว่า ณ ตอนนี้ตัวมันมีไอดีแท้ (ActiveUnitData) ผูกมาจากคลังหรือยัง?
            // ถ้ามีอยู่แล้ว (เช่น เกิดจากการจัดทัพของ ArmyController) -> ให้ข้ามไปเลย ห้ามเจดไอดีใหม่ทับเด็ดขาด!
            if (statsManager.ActiveUnitData != null && !string.IsNullOrEmpty(statsManager.ActiveUnitData.uniqueId))
            {
                Debug.Log($"[TameableAnimal] 🔒 ปลอดภัย: ยูนิตตัวนี้มีรหัสแท้อยู่แล้ว ({statsManager.ActiveUnitData.uniqueId}) ข้ามการสร้างไอดีซ้ำรอบสอง!");
            }
            else
            {
                // 💡 โค้ดสร้างไอเทม/สุ่มไอดีเก่าของเพื่อน (ปล่อยทำงานเฉพาะตอนที่เป็นสัตว์ป่าเกิดตามธรรมชาติในฉาก)
                // ตัวอย่างเช่น:
                // UnitInstance wildInstance = new UnitInstance(myTemplateSO);
                // wildInstance.uniqueId = System.Guid.NewGuid().ToString();
                // statsManager.SetupFromInstance(wildInstance);
            }
        }
    }

    private void Update()
    {
        if (isTamed) return;

        if (Input.GetKeyDown(tameKey))
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                int maxHP = MyRuntimeStats.maxHP;
                if (distance <= tameRange && characterStats.currentHP <= 20)
                {
                    Debug.Log("Try to tame");
                    Tame();
                } 
            }
            else
            {
                PlayerActionMovement playerMovement = FindAnyObjectByType<PlayerActionMovement>();
                if (playerMovement != null)
                {
                    float distance = Vector3.Distance(transform.position, playerMovement.transform.position);
                    int maxHP = MyRuntimeStats.maxHP;
                    if (distance <= tameRange && characterStats.currentHP <= ((hpPercentToTame) * maxHP))
                    {
                        Tame();
                    } 
                }
            }
        }
    }

    public void Tame()
    {
        if (isTamed) return;
        isTamed = true;

        if (AnimalInventory.Instance == null)
        {
            Debug.LogError("TamedUnitsManager not found! Cannot add tamed unit.");
            return;
        }

        // ✅ ดึงค่า MyRuntimeStats ที่สุ่มคาไว้ตั้งแต่เกิด ส่งเข้าคลังกองทัพโดยตรง (ไม่มีการสุ่มใหม่)
        AnimalInventory.Instance.AddUnitToInventory(MyRuntimeStats);

        Debug.Log($"Successfully tamed {MyRuntimeStats.customName}! Stats grabbed from wild -> HP: {MyRuntimeStats.maxHP}, ATK: {MyRuntimeStats.attackDamage}");

        // 🟢 บังคับให้ SaveSystem เซฟทันที เพราะเราเปลี่ยนข้อมูลสัตว์เลี้ยงแล้ว
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGame(forceSave: true);
            Debug.Log("[Save] 💾 บันทึกข้อมูลการจับสัตว์ลงเครื่องสำเร็จ!");
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, tameRange);
    }
}