using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// สร้างปุ่มเลือกไอเทม (เช่น ปุ๋ยแต่ละชนิด, ทรัพยากรแต่ละชนิด) จาก List&lt;SO_ItemData&gt; แบบไดนามิก
/// แทนที่จะต้องลาก reference ปุ่มทีละอันใน Inspector เอง — เหมาะมากตอนมีไอเทมเยอะ/เพิ่มทีหลังบ่อยๆ
///
/// วิธีติดตั้งใน Editor:
/// 1. สร้าง Prefab ปุ่ม 1 อัน (Button ที่มี Text เป็นลูกไว้โชว์ชื่อ) เก็บเป็น Prefab แยกไว้
/// 2. สร้าง GameObject ว่างๆ ใต้ Panel เป็น "container" แล้วใส่ Layout Group
///    (Horizontal/Vertical/Grid Layout Group) เพื่อให้ปุ่มที่ spawn มาเรียงกันสวยๆ อัตโนมัติ
/// 3. เอา component นี้ไปแปะที่ไหนก็ได้ (หรือแปะที่ container เอง) แล้วลาก buttonPrefab, container เข้าไป
/// 4. ใส่ SO_ItemData ทุกชนิดที่ต้องการให้มีปุ่มลงใน list "items" (ลากจาก Project window)
/// 5. เรียก Spawn(callback) จาก script อื่น (เช่น FarmPlotPanelUI) ตอน Start เพื่อสร้างปุ่มทั้งหมดทีเดียว
/// </summary>
public class DynamicItemButtonSpawner : MonoBehaviour
{
    [Header("ข้อมูลไอเทมทั้งหมดที่จะสร้างปุ่มให้ (ลาก SO_ItemData มาใส่ได้เลย)")]
    public List<SO_ItemData> items = new List<SO_ItemData>();

    [Header("Prefab ปุ่ม 1 อัน (ต้องมี Text เป็นลูกไว้โชว์ชื่อไอเทม)")]
    public Button buttonPrefab;

    [Header("Parent ที่จะเก็บปุ่มที่ spawn ออกมา (แนะนำให้มี Layout Group)")]
    public Transform container;

    private readonly List<Button> _spawnedButtons = new List<Button>();

    /// <summary>สร้างปุ่มทั้งหมดจาก items (ลบของเก่าทิ้งก่อนเสมอ กันเรียกซ้ำแล้วปุ่มซ้อนกัน)</summary>
    public void Spawn(System.Action<SO_ItemData> onItemClicked)
    {
        Clear();

        if (buttonPrefab == null || container == null) return;

        foreach (var item in items)
        {
            if (item == null) continue;

            Button newButton = Instantiate(buttonPrefab, container);
            newButton.gameObject.SetActive(true);

            // ใส่ชื่อไอเทมลงบน Text ลูกของปุ่ม (ถ้ามี)
            Text label = newButton.GetComponentInChildren<Text>();
            if (label != null) label.text = item.itemName;

            // 🟢 ถ้า SO_ItemData ของคุณมีฟิลด์รูปไอคอน (เช่น "public Sprite icon;")
            // และปุ่มมี Image ลูกชื่อ "Icon" ไว้ใส่รูป ให้ปลดคอมเมนต์ 2 บรรทัดนี้
            // แล้วแก้ "item.icon" ให้ตรงชื่อฟิลด์จริงในสคริปต์ SO_ItemData ของคุณ
            // Image iconImage = newButton.transform.Find("Icon")?.GetComponent<Image>();
            // if (iconImage != null) iconImage.sprite = item.icon;

            SO_ItemData captured = item; // 🔒 กัน closure bug: ต้องก๊อปตัวแปรออกมาก่อน ไม่งั้นทุกปุ่มจะอ้างถึงตัวสุดท้ายในลูป
            newButton.onClick.AddListener(() => onItemClicked?.Invoke(captured));

            _spawnedButtons.Add(newButton);
        }
    }

    /// <summary>ล็อก/ปลดล็อกปุ่มที่ spawn ไว้ทั้งหมดพร้อมกัน (เช่น ตอนยังไม่ปลดล็อกสกิลผ่านตึกวิจัย)</summary>
    public void SetInteractable(bool interactable)
    {
        foreach (var b in _spawnedButtons)
        {
            if (b != null) b.interactable = interactable;
        }
    }

    /// <summary>ลบปุ่มที่เคย spawn ไว้ทั้งหมดทิ้ง (เผื่อ items เปลี่ยนแล้วอยาก spawn ใหม่)</summary>
    public void Clear()
    {
        foreach (var b in _spawnedButtons)
        {
            if (b != null) Destroy(b.gameObject);
        }
        _spawnedButtons.Clear();
    }
}   