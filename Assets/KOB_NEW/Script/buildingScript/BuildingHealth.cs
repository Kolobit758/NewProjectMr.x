using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterStats))]
public class BuildingHealth : MonoBehaviour, ITaskable
{
    private CharacterStats characterStats;

    [Header("UI Settings")]
    public Slider healthBarSlider;       // ลาก UI Slider มาใส่ตรงนี้ (สามารถทำเป็น World Space Canvas บนตึกได้)
    public GameObject canvasUIParent;    // ตัวปิด-เปิด UI (ถ้าอยากให้เลือดแสดงเฉพาะตอนโดนตีหรือเมาส์ชี้)

    [Header("Building Settings")]
    public string buildingName = "สิ่งก่อสร้าง";
    public bool isDestroyedOnDeath = true;

    void Awake()
    {
        characterStats = GetComponent<CharacterStats>();
    }

    void Start()
    {
        // กำหนดเลือดเริ่มต้นของตึก (สามารถปรับแก้ตามประเภทตึกได้ที่นี่)
        int defaultMaxHP = 200;
        characterStats.InitializeStats(defaultMaxHP, defaultMaxHP);

        UpdateHealthUI();
    }

    void Update()
    {
        // อัปเดต UI หลอดเลือดทุกเฟรม (หรือจะผูกกับ Event ของ CharacterStats ก็ได้)
        UpdateHealthUI();

        // เช็คว่าเลือดหมดหรือยัง
        if (characterStats.currentHP <= 0)
        {
            DestroyBuilding();
        }
    }

    void UpdateHealthUI()
    {
        if (healthBarSlider != null)
        {
            int[] stats = characterStats.GetPrivateData();
            int maxHP = stats[0]; // สมมติช่องแรกคือ maxHP (หรือปรับตามโครงสร้าง CharacterStats ของคุณ)
            if (maxHP > 0)
            {
                healthBarSlider.value = (float)characterStats.currentHP / maxHP;
            }
        }
    }

    // 💥 ฟังก์ชันทำลายตึกเมื่อเลือดหมด
    void DestroyBuilding()
    {
        Debug.LogError($"💥 [Building]: ตึก {buildingName} ถูกทำลายพังทลายลงแล้ว!");

        if (isDestroyedOnDeath)
        {
            // ทำลาย GameObject ของตึกทิ้ง
            Destroy(gameObject, 0.1f);
        }
    }

    // 🟢 รองรับอินเทอร์เฟซ ITaskable (เพื่อให้ยูนิตฝ่ายเราเข้ามาซ่อมแซมหรือทำปฏิสัมพันธ์กับตึกได้ในอนาคต)
    public void OnUnitInteract(UnitBase unit)
    {
        Debug.Log($"🔨 ยูนิตกำลังซ่อมแซม/ใช้งานตึก {buildingName}");
    }

    public void OnUnitExit(UnitBase unit) { }
    public Vector3 GetInteractionPoint() => transform.position + transform.forward * 2f;
}