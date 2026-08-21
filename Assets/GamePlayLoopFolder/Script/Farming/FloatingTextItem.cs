using UnityEngine;
using TMPro;

public class FloatingTextItem : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float lifeTime = 1.2f;     

    private float timer = 0f;
    private Vector3 startPos;
    private Vector3 targetPos;
    public float yOffset = 2f;

    void Awake()
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshProUGUI>();
    }

    public void Initialize(string text, Color color)
    {
        if (textMesh != null)
        {
            textMesh.text = text;
            textMesh.color = color;
        }

        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // 🟢 แก้ให้ใช้ตำแหน่งจริงของ Object ที่ถูก Instantiate (บวก yOffset ขึ้นไปบนหัว)
        startPos = transform.position + new Vector3(0f, yOffset, 0f);
        transform.position = startPos; // เซ็ตตำแหน่งเริ่มต้นให้ตรงเป๊ะทันที

        // 🟢 ปรับระยะทางให้พอดีๆ ไม่ไกลเกินไป (พุ่งไปข้างหน้า Z เล็กน้อย และลอยขึ้น Y นิดหน่อยให้ดูมีมิติ)
        targetPos = startPos + new Vector3(0f, 1.0f, 2.5f);

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float percent = timer / lifeTime;

        // 🟢 ปรับความชันของ Ease ให้สมูทขึ้น (ใช้พาวเวอร์ 3 หรือ 4 พอ ไม่แข็งโป๊กแบบ 10)
        float easePercent = 1f - Mathf.Pow(1f - percent, 4f);

        // คำนวณตำแหน่งเคลื่อนที่
        transform.position = Vector3.Lerp(startPos, targetPos, easePercent);

        // ค่อยๆ จางหาย (Fade Out) ช่วงท้าย
        if (textMesh != null)
        {
            Color c = textMesh.color;
            if (percent > 0.6f)
            {
                float fadePercent = (percent - 0.6f) / 0.4f;
                c.a = Mathf.Lerp(1f, 0f, fadePercent);
            }
            textMesh.color = c;
        }
    }
}