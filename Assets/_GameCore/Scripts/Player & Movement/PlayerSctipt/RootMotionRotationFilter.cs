using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RootMotionRotationFilter : MonoBehaviour
{
    private Animator anim;
    private Rigidbody rb;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    // ฟังก์ชันพิเศษของ Unity ที่จะทำงานทุกครั้งที่ Animator คำนวณ Root Motion เสร็จในแต่ละเฟรม
    void OnAnimatorMove()
    {
        if (anim == null) return;

        // 1. นำระยะเคลื่อนที่ (Position) จาก Root Motion มาสั่งให้ Rigidbody ขยับตามปกติ
        if (rb != null)
        {
            Vector3 newPosition = rb.position + anim.deltaPosition;
            rb.MovePosition(newPosition);
        }
        else
        {
            transform.position += anim.deltaPosition;
        }

        // 2. 💡 ส่วนของการหมุน (Rotation): เราปล่อยว่างไว้ ไม่เอา anim.deltaRotation มาใส่
        // ทำให้อำนาจการหมุนถูกคืนกลับมาให้โค้ด RotatePlayerModel() ในสคริปต์หลักของคุณทันที!
    }
}