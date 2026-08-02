using UnityEngine;
using TMPro; // อย่าลืมใช้ TextMeshPro เพื่อความคมชัด

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMesh;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float disappearSpeed = 3f;
    [SerializeField] private float lifetime = 0.6f;

    private Color textColor;

    public void Setup(int damageAmount)
    {
        if (textMesh == null) textMesh = GetComponentInChildren<TextMeshProUGUI>();
        
        textMesh.text = damageAmount.ToString();
        textColor = textMesh.color;

        // สุ่มเอียงและเยื้องตำแหน่งเล็กน้อยแบบ Ragnarok ไม่ให้มันซ้อนกันทื่อๆ
        transform.localPosition += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.2f, 0.2f), 0);
        transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));

        Destroy(gameObject, lifetime); // ทำลายตัวเองเมื่อหมดอายุขัย
    }

    private void Update()
    {
        // ลอยขึ้นข้างบนเรื่อยๆ
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // หันหน้าเข้าหาการ์เมร่าหลักตลอดเวลา (Billboard Effect)
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
        }

        // ค่อยๆ จางหายไป (Fade Out)
        textColor.a -= disappearSpeed * Time.deltaTime;
        textMesh.color = textColor;
    }
}