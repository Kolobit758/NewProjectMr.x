using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class AnimalStatsManager : MonoBehaviour
{
    // ตัวแปรข้อมูลยูนิตหลักที่ตัวละครตัวนี้ถืออยู่บนสนาม
    public UnitInstance ActiveUnitData { get; private set; }

    // ตัวแปรเก็บค่าสถิติดั้งเดิมของตัวละคร
    public CharacterStats Stats { get; private set; }
    [Header("original stats")]
    private float originSpeed;

    [SerializeField] private Slider hpBar;
    [SerializeField] private Slider staminaBar;

    void OnEnable()
    {
        Stats.OnStaminaChanged += OnOutOfEnergy;
        Stats.OnStaminaChanged += OnFullFillEnergy;
        
        // [เปลี่ยน UI]
        Stats.OnHPChanged += ChangeHpStaminaUI;
        Stats.OnStaminaChanged += ChangeHpStaminaUI;
    }
    void OnDisable()
    {
        Stats.OnStaminaChanged -= OnOutOfEnergy;   // <--- ลบออก ถูกต้อง
        Stats.OnStaminaChanged -= OnFullFillEnergy;  // <--- อ้าว! ดันไปบวกเพิ่มซ้ำตอนปิดซะงั้น!

                // [เปลี่ยน UI]
        Stats.OnHPChanged -= ChangeHpStaminaUI;
        Stats.OnStaminaChanged -= ChangeHpStaminaUI;
    }

    private void Awake()
    {
        originSpeed = gameObject.GetComponent<NavMeshAgent>().acceleration;
        Stats = GetComponent<CharacterStats>();
        if (Stats == null) Stats = gameObject.AddComponent<CharacterStats>();


    }

    /// <summary>
    /// 💥 ฟังก์ชันสวมข้อมูลแท้: บังคับใช้ Reference ก้อนเดียวกับคลังสำรอง ห้ามแตกหน่อ ID!
    /// </summary>
    public void SetupFromInstance(UnitInstance instance)
    {
        if (instance == null) return;

        // 🚨 ล็อก Reference เข้าหากันดื้อๆ ห้ามใช้ new UnitInstance() ตรงนี้เด็ดขาด
        this.ActiveUnitData = instance;

        // 🟢 ดึงค่า Stat จากอินสแตนซ์ในกระเป๋ามาพ่นใส่ตัวละครบนสนามรบจริง
        if (Stats != null)
        {
            // Stats.maxHP = instance.maxHP;
            Stats.currentHP = instance.currentHP;
            // สั่งอัปเดตค่าอื่นๆ ตามโครงสร้าง CharacterStats ของเพื่อนได้เลยจ้า
        }

        Debug.Log($"[Stats] 🔒 ซิงค์ยูนิตสำเร็จ: {ActiveUnitData.customName} บนสนามล็อกรหัส ID แท้เดียวกับคลังแล้ว -> {ActiveUnitData.uniqueId}");
    }

    /// <summary>
    /// 💡 ฟังก์ชันสำหรับทหารหลัก (Local Unit): สร้างตัวแปรพร้อมส่งพารามิเตอร์ดับบั๊ก CS7036
    /// </summary>
    public void SetupFromDataSO(UnitDataSO data)
    {
        if (data == null) return;

        // 🟢 FIX SUCCESS: ยัดพารามิเตอร์ 'data' เข้าไปในวงเล็บตามกฎ Constructor ของคลาสเพื่อน!
        ActiveUnitData = new UnitInstance(data);

        // บังคับสุ่มรหัสชั่วคราวขึ้นมาใช้ เพื่อให้ทหารหลักมี ID ประจำตัวตอนรันคำสั่งบนสนามรบ
        ActiveUnitData.uniqueId = System.Guid.NewGuid().ToString();
        ActiveUnitData.customName = data.speciesName;
        ActiveUnitData.maxHP = data.baseMaxHP;
        ActiveUnitData.currentHP = data.baseMaxHP;

        if (Stats != null)
        {
            // Stats.maxHP = data.baseMaxHP;
            Stats.currentHP = data.baseMaxHP;
        }
    }


    public void OnOutOfEnergy()
    {
        if (Stats.currentStamina > 0)
        {
            Debug.Log("I have stamina");
            return;
        }
        ;
        if (Stats.currentStamina <= 0)
        {
            NavMeshAgent agent = gameObject.GetComponent<NavMeshAgent>();
            agent.speed = 0;
            Debug.Log("brooo I'm tried");
        }

    }
    public void OnFullFillEnergy()
    {
        int[] privateData = Stats.GetPrivateData();
        if (Stats.currentStamina >= privateData[2])
        {
            NavMeshAgent agent = gameObject.GetComponent<NavMeshAgent>();
            agent.speed = originSpeed;
            Debug.Log("I'm full fill");
        }
    }

    public void ChangeHpStaminaUI()
    {
        int[] privateStats = Stats.GetPrivateData();
        hpBar.value = (float)Stats.currentHP / privateStats[0];
        staminaBar.value = (float)Stats.currentStamina / privateStats[2];
    }
}