using System.Collections;
using UnityEngine;

public class TreeResourceNode : ResourceNodeBase
{
    public GameObject treelog;
    public GameObject currentTreeLog;
    public Transform treeStump;
    public Transform tree;
    public float newSpawnDuration = 2f;
    private bool isDelivered = false;

    void Start()
    {
        if (tree != null) tree.gameObject.SetActive(true);
        if (treeStump != null) treeStump.gameObject.SetActive(false);
        canGathering = true;
    }

    protected override void StartLogistic()
    {
        Transform nearestVault = FindNearestVault();
        if (nearestVault == null || !canGathering) return;

        StartCoroutine(ReviveTreeCoroutine());

        // สร้างซุงและให้ยูนิตตัวแรกถือ
        if (treelog != null && units.Count > 0 && units[0] != null)
        {
            currentTreeLog = Instantiate(treelog, units[0].transform);
            currentTreeLog.transform.localPosition = new Vector3(0f, 0f, -2.5f);
            currentTreeLog.transform.localRotation = Quaternion.identity;
        }

        float draggingSpeed = 2.5f;

        foreach (var unit in units)
        {
            if (unit == null) continue;
            unit.gatBase = this;
            unit.carriedItem = resourceToProduce;
            unit.carriedAmount = amountResource;
            // หมายเหตุ: ใน UnitBase อาจจะต้องปรับให้รองรับ GatheringBase เป็น ResourceNodeBase แทน
            // unit.gatBase = this; 

            if (currentTreeLog != null)
            {
                unit.StartDragging(currentTreeLog.transform, nearestVault, draggingSpeed);
            }
        }
    }

    public override void OnUnitSentResourced()
    {
        if (isDelivered) return;
        isDelivered = true;

        if (currentTreeLog != null)
        {
            Destroy(currentTreeLog.gameObject);
            currentTreeLog = null;
        }

        // 🔒 เรียก ResetUnitState ให้ยูนิตกลับเข้าสู่สภาพ Idle (ซึ่งจะไปต่อใน Auto-Repeat Loop ได้)
        foreach (UnitBase unit in units)
        {
            if (unit != null)
            {
                unit.ResetUnitState();
            }
        }

        units.Clear();
        isLogisticStarted = false;
        isDelivered = false; // reset กลับเมื่อ cleanup เสร็จสมบูรณ์
    }

    IEnumerator ReviveTreeCoroutine()
    {
        canGathering = false;
        if (tree != null) tree.gameObject.SetActive(false);
        if (treeStump != null) treeStump.gameObject.SetActive(true);

        yield return new WaitForSeconds(newSpawnDuration);

        canGathering = true;
        if (tree != null) tree.gameObject.SetActive(true);
        if (treeStump != null) treeStump.gameObject.SetActive(false);
    }
}