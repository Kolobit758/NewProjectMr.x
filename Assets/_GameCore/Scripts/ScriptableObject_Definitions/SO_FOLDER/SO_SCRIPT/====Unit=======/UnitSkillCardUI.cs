using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UnitSkillCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Visual Components")]
    public Image unitIconImage;
    public Image skillIconImage;
    public Image cooldownOverlay;
    public TextMeshProUGUI cooldownText;
    public TextMeshProUGUI unitNameText;

    // 🟢 เก็บพิกัดพัดโค้งดั้งเดิมที่คำนวณมาจาก HUD Manager
    [HideInInspector] public Vector3 initialLocalPosition;
    [HideInInspector] public Quaternion initialLocalRotation;
    [HideInInspector] public int siblingIndexBeforeDrag;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private UnitInstance targetInstance;
    private GameObject spawnedUnitGo;
    private bool isDragging = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Update()
    {
        if (targetInstance == null || targetInstance.template.uniqueSkill == null) return;

        // อัปเดตหลอดคูลดาวน์หมุนติ้ว
        if (targetInstance.currentCooldownTimer > 0f)
        {
            cooldownOverlay.fillAmount = targetInstance.currentCooldownTimer / targetInstance.template.uniqueSkill.cooldown;
            cooldownText.text = targetInstance.currentCooldownTimer.ToString("F1");
            cooldownText.gameObject.SetActive(true);
        }
        else
        {
            cooldownOverlay.fillAmount = 0f;
            cooldownText.gameObject.SetActive(false);
        }
    }

    public void SetupCard(UnitInstance instance, GameObject unitGameObject)
    {
        targetInstance = instance;
        spawnedUnitGo = unitGameObject;

        if (instance.template.uniqueSkill != null)
        {
            unitIconImage.sprite = instance.template.unitIcon;
            skillIconImage.sprite = instance.template.uniqueSkill.skillIcon;
            unitNameText.text = instance.customName;
            cooldownOverlay.fillAmount = 0f;
            cooldownText.gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // 🎴 [Drag System] เริ่มต้นลากการ์ดสกิลออกจากกองพัดโค้ง
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (targetInstance != null && targetInstance.currentCooldownTimer > 0f) return; // ติดคูลดาวน์ห้ามลาก

        isDragging = true;
        siblingIndexBeforeDrag = rectTransform.GetSiblingIndex();
        rectTransform.SetAsLastSibling(); // ดึงการ์ดใบที่ลากขึ้นมาเลเยอร์หน้าสุดของจอทันที จะได้ไม่วิ่งมุดใต้การ์ดใบอื่น

        canvasGroup.blocksRaycasts = false; // ปล่อย Raycast ทะลุลงไปตรวจจับโลก 3D ด้านหลัง
        canvasGroup.alpha = 0.8f; // ปรับให้ตัวการ์ดจางลงเล็กน้อยตอนลาก เพิ่มความรู้สึกว่ากำลังลอยอยู่
    }

    // 🎴 ระหว่างลากการ์ด: ให้พิกัดเคลื่อนที่วิ่งตามปลายเมาส์อย่างอิสระ
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        rectTransform.position = eventData.position;
        rectTransform.localRotation = Quaternion.identity; // ปลดมุมบิดเอียงออกชั่วคราวขณะลากเพื่อให้ดูตรงสวยงาม
    }

    // 🎴 ปล่อยการ์ด: เช็คตำแหน่งเมาส์ว่าปล่อยร่ายสกิล หรือยกเลิกแล้วดึงการ์ดกลับเข้าพัดโค้งเดิม
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        rectTransform.SetSiblingIndex(siblingIndexBeforeDrag); // คืนลำดับเลเยอร์ในแถวพัด

        // เงื่อนไข: ถ้าลากขึ้นสูงเกิน 35% ของความสูงหน้าจอ (ลากออกห่างจากแถวด้านล่าง) = สั่งใช้สกิล!
        if (eventData.position.y > Screen.height * 0.35f)
        {
            if (targetInstance.IsSkillReady())
            {
                targetInstance.template.uniqueSkill.ExecuteSkill(spawnedUnitGo);
                targetInstance.currentCooldownTimer = targetInstance.template.uniqueSkill.cooldown;
                Debug.Log($"[Skill Cast] ⚡ เปิดใช้งานสกิลของ {targetInstance.customName} เรียบร้อย!");
            }
        }

        // คืนตำแหน่งและมุมเอียงกลับเข้าสล็อตพัดโค้งเดิมแบบเป๊ะ ๆ ทันที
        rectTransform.localPosition = initialLocalPosition;
        rectTransform.localRotation = initialLocalRotation; 
    }
}