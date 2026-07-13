using UnityEngine;

public class BranchSegment : MonoBehaviour
{
    public bool hasNode = false;
    public bool isHead = true;
    public GraftType currentGraft = GraftType.Normal;

    public IslandController currentIslandBelongTo;

    [Header("Connection Transforms")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Leaf & Shadow Indicator")]
    public GameObject leafVisual;
    public MeshRenderer leafRenderer;

    [Header("Node Visuals")]
    [SerializeField] private GameObject nodeVisual;
    [SerializeField] private MeshRenderer segmentRenderer;


    public GameObject leftSide;
    public GameObject RightSide;

    private void OnTriggerStay(Collider other)
    {
        IslandController island = other.GetComponent<IslandController>();
        if (island != null)
        {
            currentIslandBelongTo = island;
        }
    }

    public void SpawnNode()
    {
        hasNode = true;
        if (nodeVisual != null) nodeVisual.SetActive(true);
    }

    public void SetHeadState(bool IsHeadCurrent)
    {
        isHead = IsHeadCurrent;

        // 🛡️ ดัก Null: ถ้าลืมลาก leafVisual ใส่ช่อง มันจะไม่สั่งงานและไม่ทำเกมระเบิด
        if (leafVisual != null)
        {
            leafVisual.SetActive(IsHeadCurrent);
        }
    }

    public void UpdateShadowVisual(bool leftBlocked, bool rightBlocked)
    {
        if (!isHead) return;

        // 🛡️ ดัก Null: เช็คว่ามี leafVisual กับ leafRenderer ไหมก่อนเปลี่ยนสี
        if (leafVisual == null || leafRenderer == null) return;

        if (!leafVisual.activeSelf) leafVisual.SetActive(true);

        if (currentIslandBelongTo != null && currentIslandBelongTo.isReceivingEnergy)
        {
            leafRenderer.material.color = Color.green;
            return;
        }

        if (leftBlocked) leafRenderer.material.color = Color.blue;
        else if (rightBlocked) leafRenderer.material.color = Color.red;
        else leafRenderer.material.color = Color.white;
    }

    public void ApplyGraft(GraftType type)
    {
        if (!hasNode) return;
        currentGraft = type;
        hasNode = false;

        if (nodeVisual != null) nodeVisual.SetActive(false);

        if (segmentRenderer != null)
        {
            if (type == GraftType.GonBuri) segmentRenderer.material.color = Color.green;
            else if (type == GraftType.BatteryTree) segmentRenderer.material.color = Color.yellow;
        }
    }
    // เพิ่มฟังก์ชันนี้ไว้ใน BranchSegment.cs
    public bool IsLeftComponent(GameObject clickedObject)
    {
        // เช็คว่าวัตถุที่คลิกโดนคือ leftSide หรือมีชื่อว่า LeftSide
        return clickedObject == leftSide || clickedObject.name == "LeftSide";
    }

    public bool IsRightComponent(GameObject clickedObject)
    {
        // เช็คว่าวัตถุที่คลิกโดนคือ RightSide หรือมีชื่อว่า RightSide
        return clickedObject == RightSide || clickedObject.name == "RightSide";
    }
}