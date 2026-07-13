using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// คอมโพเนนต์ติดบน Prefab ของ UI ที่ลอยเหนือเกาะ 1 เกาะ
/// โครงสร้าง Prefab แนะนำ:
///   - GameObject หลัก (อันเดียวกับที่แปะสคริปต์นี้) ต้องมี Component "Canvas" ตั้งค่า Render Mode = World Space
///     - ปรับ Rect Transform ให้มีขนาดพอสมควร (เช่น Width 300 x Height 100) แล้วปรับ Scale ของ Canvas ทั้งก้อนให้เล็กลง
///       (เช่น scale 0.03) เพราะ World Space Canvas ใช้หน่วยเป็นเมตรเหมือนโลกเกม ถ้าไม่ย่อจะดูใหญ่มาก
///     - ใส่ Component "Canvas Scaler" (ไม่จำเป็นต้องตั้งอะไรเพิ่มสำหรับ World Space)
///   - ลูกๆ ใต้ Canvas:
///     - Image    ชื่อ "BackgroundImage" (พื้นหลัง) → ลากใส่ช่อง Highlight Image เพื่อให้เรืองแสงตอนเป็นเป้าหมายเควส
///     - TMP_Text ชื่อ "IslandNameText"   (แสดงชื่อเกาะ)
///     - Slider   ชื่อ "EnergySlider"     (แสดงระดับพลังงาน 0-1)
///     - TMP_Text ชื่อ "EnergyValueText" (แสดงตัวเลข เช่น "45 / 100")
///     - Image (ถ้ามี) ชื่อ "ConnectedIcon" (โชว์/ซ่อนตามว่าต่อเน็ตเวิร์กหรือยัง)
///     - GameObject (ถ้ามี) ชื่อ "QuestTargetBadge" (เช่น ไอคอนดาว ★ โชว์เมื่อเกาะนี้เป็นเป้าหมายเควส)
///
/// หมายเหตุ: สคริปต์ MotherTreeUIManager จะ Instantiate Prefab นี้เป็นลูกของตัวเกาะเอง
/// แล้วยกขึ้นไปตามค่า Height Offset และหมุนให้หงายขึ้น (Euler 90,0,0) ให้กล้อง God Mode เห็นชัด
///
/// วิธีแปะสคริปต์นี้: ลากไฟล์ "IslandEnergyRow.cs" นี้ไปวางบน GameObject หลักของ Prefab ได้เลย
/// (ชื่อไฟล์ตรงกับชื่อคลาส ลากง่าย ไม่ต้องเสิร์ชหาผ่าน Add Component)
/// </summary>
public class IslandEnergyRow : MonoBehaviour
{
    [SerializeField] private TMP_Text islandNameText;
    [SerializeField] private Slider energySlider;
    [SerializeField] private TMP_Text energyValueText;
    [SerializeField] private GameObject connectedIcon; // optional, เอาออกได้ถ้าไม่ใช้

    [Header("Quest Target Highlight")]
    [Tooltip("Background/Border image ของแถว ที่จะเปลี่ยนสีเมื่อเกาะนี้เป็นเป้าหมายเควส")]
    [SerializeField] private Image highlightImage;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.05f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.2f, 0.9f);
    [Tooltip("ถ้ามี GameObject ไอคอน/ป้ายบอกว่าเป็นเป้าหมายเควส (เช่น เครื่องหมาย ★) ใส่ไว้ตรงนี้ จะโชว์/ซ่อนอัตโนมัติ")]
    [SerializeField] private GameObject questTargetBadge;

    private bool isCurrentlyHighlighted = false;

    public void SetName(string displayName)
    {
        if (islandNameText != null) islandNameText.text = displayName;
    }

    public void SetEnergy(float current, float max, bool isConnected)
    {
        if (energySlider != null)
            energySlider.value = current / Mathf.Max(max, 0.0001f);

        if (energyValueText != null)
            energyValueText.text = $"{current:0} / {max:0}";

        if (connectedIcon != null)
            connectedIcon.SetActive(isConnected);
    }

    public void SetHighlight(bool isTarget)
    {
        // กันไม่ให้เซ็ตค่าซ้ำทุกเฟรมโดยไม่จำเป็น (เผื่อมีอนิเมชันต่อยอดในอนาคต)
        if (isCurrentlyHighlighted == isTarget) return;
        isCurrentlyHighlighted = isTarget;

        if (highlightImage != null)
            highlightImage.color = isTarget ? highlightColor : normalColor;

        if (questTargetBadge != null)
            questTargetBadge.SetActive(isTarget);
    }
}