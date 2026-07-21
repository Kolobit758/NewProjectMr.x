using UnityEngine;

public class ThrownFood : MonoBehaviour
{
    public SO_ItemData foodData;
    private Rigidbody rb;

    public void Init(Vector3 force, SO_ItemData data)
    {
        foodData = data;
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(force, ForceMode.Impulse);
        }

        // ทำลายตัวเองอัตโนมัติใน 10 วินาทีถ้าไม่มีใครกิน
        Destroy(gameObject, 10f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // เช็คว่าชนกับสัตว์ป่าที่มีสคริปต์ TameableAnimal ไหม
        TameableAnimal animal = collision.gameObject.GetComponent<TameableAnimal>();
        if (animal == null)
        {
            animal = collision.gameObject.GetComponentInParent<TameableAnimal>();
        }

        if (animal != null && foodData != null)
        {
            animal.TryTameWithFood(foodData);
            Destroy(gameObject); // กินเสร็จ อาหารหายไป
        }
    }
}