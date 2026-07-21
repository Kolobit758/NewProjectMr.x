using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GatheringBase : MonoBehaviour, ITaskable
{
    public List<UnitBase> units = new List<UnitBase>();
    public int minPowerToGathering = 0;
    public SO_ItemData resourceToProduce;
    public int amountResource; // แก้พิมพ์ผิดนิดนึงจาก amoutResource

    private Transform targetVault;
    private bool isDelivered = false; 

    void Start() { }
    void Update() { }

    public void OnUnitInteract(UnitBase unit)
    {
        units.Add(unit);
        units.RemoveAll(u => u == null);

        if (CalculateGatheringPower() >= minPowerToGathering)
        {
            Debug.Log("Gat Start");
            StartLogistic();
        }
    }

    public void OnUnitExit(UnitBase unit) { }

    private Transform FindNearestVault()
    {
        GameObject[] vaults = GameObject.FindGameObjectsWithTag("Vault");
        Transform nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject v in vaults)
        {
            float dist = Vector3.Distance(transform.position, v.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = v.transform;
            }
        }
        return nearest;
    }

    public void StartLogistic()
    {
        Transform nearestVault = FindNearestVault();
        if (nearestVault == null) return;

        this.transform.SetParent(units[0].transform);
        this.transform.localPosition = Vector3.back * 2f;

            foreach (var unit in units)
        {
            unit.carriedItem = resourceToProduce;
            unit.carriedAmount = amountResource;
            unit.StartDragging(this.transform, nearestVault);
        }
        StartCoroutine(DraggingProcess(nearestVault));
    }

    IEnumerator DraggingProcess(Transform targetVault)
    {
        while (true)
        {
            bool allReached = true;
            foreach (var unit in units)
            {
                if (unit != null && unit.agent.pathPending == false && unit.agent.remainingDistance > unit.agent.stoppingDistance)
                {
                    allReached = false; break;
                }
            }
            if (allReached) break;
            yield return null;
        }
    }

    public void OnUnitSentResourced()
    {
        if (isDelivered) return;
        isDelivered = true;

        Debug.Log("กำลังลบข้อมูลและคืนอิสระให้ยูนิตทุกตัว");

        this.transform.SetParent(null);

        // 🟢 วนลูปสั่งให้ยูนิตทุกตัวล้างค่าของตัวเองให้สะอาด
        foreach (UnitBase unit in units)
        {
            if (unit != null)
            {
                unit.ResetUnitState(); 
            }
        }

        units.Clear();
        Destroy(this.gameObject); 
    }

    int CalculateGatheringPower()
    {
        int gatPower = 0;
        foreach (UnitBase unit in units)
        {
            gatPower += unit.gatheringPower;
        }
        return gatPower;
    }

    public Vector3 GetInteractionPoint() => transform.position;
}