using UnityEngine;
using System.Collections.Generic;

public enum GodTool { None, Fertilizer, PlantGraft, ClothShieldLeft, ClothShieldRight, NoCloth, ToggleEnergyValve }

public class GodModeUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MotherTreeController treeController;
    [SerializeField] private Camera mainCamera;

    [Header("Current State")]
    public GodTool currentTool = GodTool.None;

    public void SelectTool(int toolIndex)
    {
        currentTool = (GodTool)toolIndex;
    }

    void Update()
    {
        if (treeController != null && treeController.IsGodMode)
        {
            HandleMouseClick();
        }
    }

    void HandleMouseClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                BranchSegment segment = hit.transform.GetComponentInParent<BranchSegment>();

                if (segment == null)
                {
                    BranchSegment[] allSegmentsInScene = FindObjectsByType<BranchSegment>(FindObjectsSortMode.None);
                    foreach (var seg in allSegmentsInScene)
                    {
                        if (hit.transform.gameObject == seg.leftSide || hit.transform.gameObject == seg.RightSide)
                        {
                            segment = seg;
                            break;
                        }
                    }
                }

                if (segment != null)
                {
                    if (currentTool == GodTool.ToggleEnergyValve)
                    {
                        if (segment.currentIslandBelongTo != null)
                        {
                            segment.currentIslandBelongTo.ToggleEnergySupply();
                            segment.UpdateShadowVisual(false, false);
                            return;
                        }
                    }

                    ExecuteToolAction(segment, hit.transform.gameObject);
                }
            }
        }
    }

    void ExecuteToolAction(BranchSegment segment, GameObject clickedObject)
    {
        bool isLeftClicked = (clickedObject == segment.leftSide);
        bool isRightClicked = (clickedObject == segment.RightSide);

        switch (currentTool)
        {
            // 🟢 🔥 [แก้ไขจุดหัวใจหลัก]: ผูกคลังกระเป๋าเข้ากับการกดคลิกปุ๋ยบนหน้าจอ God View จริงๆ แล้วมึงกอบ!
            case GodTool.Fertilizer:
                if (isLeftClicked || isRightClicked) break;

                if (ResourceInventory.Instance != null)
                {
                    // 1. ดักเช็คของในกระเป๋าจริงก่อนว่ามีปุ๋ยให้ใช้งานไหม
                    if (ResourceInventory.Instance.HasResource("Fertilizer", 1) || ResourceInventory.Instance.HasResource("Poop", 1))
                    {
                        if (segment.isHead)
                        {
                            // 2. หักปุ๋ยจากช่องคลังจริงของผู้เล่น 1 ชิ้น!
                            ResourceInventory.Instance.ConsumeResourceByName("Fertilizer", 1);
                            
                            // 3. สั่งงอกกิ่งแบบสุ่มท่อน (1-3 ท่อนย่อย) พรวดเดียวสะใจ
                            treeController.GrowBranchFromHead(segment);
                            Debug.Log("🌱 [God View]: จ่ายปุ๋ยจากกระเป๋าสำเร็จ! เร่งกิ่งไม้สุ่มงอกพุ่งกระฉูด!");
                        }
                        else if (segment.hasNode)
                        {
                            // ดักไว้เผื่อจิ้มแตกกิ่งย่อยออกจากตาไม้ ก็ใช้ปุ๋ยหักของในคลังเหมือนกันมึงกอบ
                            ResourceInventory.Instance.ConsumeResourceByName("Fertilizer", 1);
                            treeController.SpawnSubBranch(segment);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("❌ ปุ๋ยหมดกระเป๋าแล้วมึงกอบ! กดเร่งงอกไม่ได้ ไปเก็บแต้มปุ๋ยที่แปลงผักก่อน!");
                    }
                }
                else
                {
                    // Failsafe เผื่อกดรันเทสโปรโตไทป์เปล่าๆ แบบไม่มีกระเป๋า ให้งอกปกติได้เลยมึง
                    if (segment.isHead) treeController.GrowBranchFromHead(segment);
                    else if (segment.hasNode) treeController.SpawnSubBranch(segment);
                }
                break;

            case GodTool.PlantGraft:
                if (segment.hasNode) segment.ApplyGraft(GraftType.GonBuri);
                break;

            case GodTool.ClothShieldLeft:
            case GodTool.ClothShieldRight:
                if (segment.isHead)
                {
                    treeController.SetShadowDirection(isLeftClicked, isRightClicked, segment);
                }
                break;

            case GodTool.NoCloth:
                if (segment.isHead)
                    treeController.SetShadowDirection(false, false, segment);
                break;
        }
    }

    // 🟢 เอาฟังก์ชัน OnClickFertiliseButton ตัวเก่าที่ลอจิกตีกันเองและคอมไพล์เลอร์ขีดแดงเตือนออกไปแล้วมึงกอบ!
    // ฟังก์ชันปุ่มกดสลับโหมดไอเทมในมือของมึงยังคงทำงานได้ถูกต้องสมบูรณ์แบบร้อยเปอร์เซ็นต์ตามนี้เลย:

    public void ChangeTooltoFertilizer()
    {
        ChangeTool(GodTool.Fertilizer);
        Debug.Log("🎒 ถือไอเทม [ปุ๋ยเร่งงอก] เตรียมจิ้มหัวกิ่งไม้บนฉากเกมแล้วมึง!");
    }
    
    public void ChangeTooltoClothShield()
    {
        ChangeTool(GodTool.ClothShieldLeft);
    }
    
    public void ChangeTooltoToggleEnergyValve()
    {
        ChangeTool(GodTool.ToggleEnergyValve);
    }
    
    public void ChangeTool(GodTool tool)
    {
        currentTool = tool;
    }
}