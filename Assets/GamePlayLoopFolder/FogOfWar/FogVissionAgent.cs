using UnityEngine;

public class FogVisionAgent : MonoBehaviour
{
    [Tooltip("ระยะการมองเห็นของยูนิตตัวนี้ (หน่วยเป็นเมตรในเกม)")]
    public float visionRadius = 8f;

    void Start()
    {
        if (FogOfWarManager.Instance != null)
        {
            FogOfWarManager.Instance.RegisterVisionSource(this);
        }
    }

    void OnDestroy()
    {
        if (FogOfWarManager.Instance != null)
        {
            FogOfWarManager.Instance.UnregisterVisionSource(this);
        }
    }

    // แสดงวงกลมระยะการมองเห็นในหน้า Scene View เพื่อให้ปรับง่ายขึ้น
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRadius);
    }
}