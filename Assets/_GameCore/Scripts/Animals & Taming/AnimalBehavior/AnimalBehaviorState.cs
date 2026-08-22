using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public abstract class AnimalBehaviorState
{
    // ตัวแปรอ้างอิงของสัตว์ตัวนั้นๆ เพื่อให้สเตตัสย่อยดึงไปสั่งงานได้
    protected GameObject animalGo;
    protected NavMeshAgent agent;
    protected Transform playerTransform;
    protected Transform thisCharacterTransform;


    public virtual void InitState(GameObject animal, NavMeshAgent navAgent)
    {
        this.animalGo = animal;
        this.agent = navAgent;

        // หาตัวผู้เล่นในฉากเก็บไว้ล่วงหน้า
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    // 🟢 ฟังก์ชันหลักที่จะโดนเรียกใช้งานทุกๆ เฟรมใน Update()
    public abstract void UpdateState(float distanceToPlayer);

    // 🟢 ฟังก์ชันที่ทำงานเฉพาะจังหวะที่ "พึ่งสลับมาใช้พฤติกรรมนี้เฟรมแรก" (เอาไว้รีเซ็ตค่า)
    public virtual void OnEnterState() { }

    public void SetFlukeTransform(Transform flukeTransformer)
    {
        thisCharacterTransform = flukeTransformer;
    }
}