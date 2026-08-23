using System.Collections;
using UnityEngine;

public class TreeResourceNode : ResourceNodeBase
{
    public GameObject treelog;
    public Transform treeStump;
    public Transform tree;
    public float newSpawnDuration = 2f;

    void Start()
    {
        if (tree != null) tree.gameObject.SetActive(true);
        if (treeStump != null) treeStump.gameObject.SetActive(false);
        canGathering = true;
    }

    protected override void StartLogisticForUnit(UnitBase unit)
    {
        if (unit == null) return;

        Transform nearestVault = FindNearestVault();
        if (nearestVault == null) return;

        // หากต้นไม้อยู่ในสถานะตัดได้ ให้สั่งเปลี่ยนสถานะเป็นตอไม้และเริ่มคูลดาวน์เกิดใหม่
        if (canGathering)
        {
            StartCoroutine(ReviveTreeCoroutine());
        }

        unit.gatBase = this;
        unit.carriedItem = resourceToProduce;
        unit.carriedAmount = amountResource;

        GameObject currentLog = null;
        if (treelog != null)
        {
            currentLog = Instantiate(treelog, unit.transform);
            currentLog.transform.localPosition = new Vector3(0f, 0f, -2.5f);
            currentLog.transform.localRotation = Quaternion.identity;
        }

        float draggingSpeed = 2.5f;
        unit.StartDragging(currentLog != null ? currentLog.transform : null, nearestVault, draggingSpeed);
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