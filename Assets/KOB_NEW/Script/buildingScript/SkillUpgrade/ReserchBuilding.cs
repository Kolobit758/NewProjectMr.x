using UnityEngine;

public class ResearchBuilding : MonoBehaviour
{
    [Header("Unlock Flags")]
    public bool unlocksAutoGather = false;
    public bool unlocksAutoFarm = false;

    void Start()
    {
        // แจ้ง Manager ว่าตึกนี้ถูกสร้างขึ้นมาแล้ว
        if (BuildingUnlockManager.Instance != null)
        {
            BuildingUnlockManager.Instance.RegisterBuilding(this);
        }
    }

    void OnDestroy()
    {
        // แจ้ง Manager ว่าตึกนี้กำลังจะถูกทำลาย
        if (BuildingUnlockManager.Instance != null)
        {
            BuildingUnlockManager.Instance.UnregisterBuilding(this);
        }
    }
}