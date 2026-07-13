using UnityEngine;
using System;

public class AttackIndicator3D : MonoBehaviour
{
    private float duration;
    private float timer;
    private Action<Vector3> onTimeUp;
    private Renderer meshRenderer;
    private Material indicatorMat;

    void Awake()
    {
        meshRenderer = GetComponent<Renderer>();
        if (meshRenderer != null)
        {
            // ดึง Material ของวงแดงออกมาโมค่าความจาง (Transparency)
            indicatorMat = meshRenderer.material;
        }
    }

    // 🟢 ฟังก์ชัน Setup ใหม่: รับค่ารัศมี (radius) จากบอสมาปรับขนาดสเกลวัตถุ 3D โดยตรง!
    public void SetupIndicator3D(float time, float radius, Action<Vector3> callback)
    {
        duration = time;
        onTimeUp = callback;
        timer = 0f;

        // 📐 ปรับขนาดสเกล 3D ขยายพื้นที่ x และ z ตามรัศมีที่บอสส่งมา แกน y ปล่อยให้แบนติดพื้น
        transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);

        if (indicatorMat != null && indicatorMat.HasProperty("_Color"))
        {
            Color c = indicatorMat.color;
            c.a = 0.1f; // เริ่มต้นจาง ๆ
            indicatorMat.color = c;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / duration;

        // 🟥 ค่อย ๆ เร่งระเบิดสีแดงขู่ผู้เล่นในโลก 3D ให้เข้มขึ้นเรื่อย ๆ
        if (indicatorMat != null && indicatorMat.HasProperty("_Color"))
        {
            Color c = indicatorMat.color;
            c.a = Mathf.Lerp(0.1f, 0.7f, progress);
            indicatorMat.color = c;
        }

        if (timer >= duration)
        {
            onTimeUp?.Invoke(transform.position); // เวลาหมด! สั่งระเบิดหนวดฟาด
            Destroy(gameObject);
        }
    }
}