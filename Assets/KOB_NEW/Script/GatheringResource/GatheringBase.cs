using UnityEngine;

public class GatheringBase : MonoBehaviour, ITaskable
{
    public SO_ItemData resourceToProduce;
    public int health = 50;
    [HideInInspector] public bool isBeingGathered = false;

    public Vector3 GetInteractionPoint() => transform.position;

    public void OnUnitInteract(UnitBase unit)
    {
        // เริ่มกระบวนการลากของไปส่งโกดัง
        Transform vault = FindNearestVault();
        if (vault != null)
        {
            unit.StartDragging(transform, vault);
        }
    }

    public void OnUnitExit(UnitBase unit) { }

    public void OnUnitSentResourced()
    {
        health -= 10;
        if (health <= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            // วนลูป Gather ต่อหากยังไม่หมด
            Transform vault = FindNearestVault();
            // ยูนิตจะทำ Job ต่อผ่านระบบ GatherJob
        }
    }

    public bool IsDepleted() => health <= 0;

    private Transform FindNearestVault()
    {
        GameObject[] vaults = GameObject.FindGameObjectsWithTag("Vault");
        Transform nearest = null;
        float minDst = Mathf.Infinity;
        foreach (var v in vaults)
        {
            float dst = Vector3.Distance(transform.position, v.transform.position);
            if (dst < minDst) { minDst = dst; nearest = v.transform; }
        }
        return nearest;
    }
}