// using UnityEngine;
// using TMPro;
// using UnityEngine.EventSystems;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine.UI;

// public class FormationSetupUI : MonoBehaviour
// {
//     [Header("References")]
//     public TamedUnitsManager tamedManager;
//     public CustomFormationData formationData;

//     [Header("UI Prefabs")]
//     public GameObject inventoryUnitPrefab;      // สำหรับแสดง Unit ในคลัง
//     public GameObject formationSlotPrefab;      // สำหรับแต่ละช่องในกริด

//     [Header("UI Containers")]
//     public Transform inventoryContainer;         // ที่ใส่ Unit ทั้งหมดที่ยังเข้า Formation ไม่ได้
//     public Transform formationGridContainer;     // ที่ใส่ Grid ของทัพ

//     [Header("UI Elements")]
//     public TMP_Text formationStatusText;             // แสดงสถานะทัพ
//     public Button confirmFormationButton;
//     public Button clearFormationButton;

//     // เก็บ UI elements ของแต่ละ slot
//     private Dictionary<string, FormationSlotUI> slotUIMap = new Dictionary<string, FormationSlotUI>();
//     private Dictionary<string, InventoryUnitUI> inventoryUIMap = new Dictionary<string, InventoryUnitUI>();

//     private void OnEnable()
//     {
//         if (confirmFormationButton != null) confirmFormationButton.onClick.AddListener(ConfirmFormation);
//         if (clearFormationButton != null) clearFormationButton.onClick.AddListener(ClearFormation);
//         RefreshUI();
//     }

//     private void OnDisable()
//     {
//         if (confirmFormationButton != null) confirmFormationButton.onClick.RemoveListener(ConfirmFormation);
//         if (clearFormationButton != null) clearFormationButton.onClick.RemoveListener(ClearFormation);
//     }

//     [ContextMenu("RefreshUI")]
//     public void RefreshUI()
//     {
//         if (tamedManager == null || formationData == null)
//         {
//             Debug.LogWarning("[FormationSetupUI] ยังไม่ได้ set TamedUnitsManager หรือ FormationData!");
//             return;
//         }
        

//         // ล้าง UI เก่า
//         ClearAllUIElements();

//         // สร้าง Grid Formation
//         CreateFormationGrid();

//         // สร้าง Inventory Units
//         CreateInventoryUnits();

//         // อัปเดตสถานะ
//         UpdateFormationStatus();
//     }

//     private void CreateFormationGrid()
//     {
//         // ล้าง container เดิม
//         foreach (Transform child in formationGridContainer)
//         {
//             Destroy(child.gameObject);
//         }

//         slotUIMap.Clear();

//         // สร้างกริด
//         for (int y = 0; y < formationData.gridHeight; y++)
//         {
//             for (int x = 0; x < formationData.gridWidth; x++)
//             {
//                 // ตรวจสอบว่าช่องนี้เปิดใช้งานหรือไม่
//                 bool isActive = false;
//                 if (y < formationData.rows.Length && x < formationData.rows[y].cols.Length)
//                 {
//                     isActive = formationData.rows[y].cols[x];
//                 }

//                 if (!isActive) continue; // ข้ามช่องปิด

//                 // สร้าง Slot UI
//                 GameObject slotObj = Instantiate(formationSlotPrefab, formationGridContainer);
//                 slotObj.name = $"Slot_{y}_{x}";

//                 FormationSlotUI slotUI = slotObj.GetComponent<FormationSlotUI>();
//                 if (slotUI == null) slotUI = slotObj.AddComponent<FormationSlotUI>();

//                 slotUI.Initialize(y, x, this);

//                 // ตรวจสอบว่ามี Unit อยู่ในช่องนี้หรือไม่
//                 var assignment = tamedManager.activeFormation.FirstOrDefault(a => a.row == y && a.col == x);
//                 if (assignment != null && assignment.unit != null)
//                 {
//                     slotUI.SetUnit(assignment.unit);
//                 }

//                 string key = $"{y}:{x}";
//                 slotUIMap[key] = slotUI;
//             }
//         }

//         Debug.Log($"[FormationSetupUI] สร้าง Formation Grid เสร็จแล้ว: {slotUIMap.Count} ช่อง");
//     }

//     private void CreateInventoryUnits()
//     {
//         // ล้าง container เดิม
//         foreach (Transform child in inventoryContainer)
//         {
//             Destroy(child.gameObject);
//         }

//         inventoryUIMap.Clear();

//         // หา Unit ที่ยังไม่ได้เข้า Formation
//         var unassignedUnits = tamedManager.tamedUnits.Where(u => 
//             u != null && !tamedManager.activeFormation.Exists(a => a.unitUniqueId == u.uniqueId)
//         ).ToList();

//         foreach (var unit in unassignedUnits)
//         {
//             GameObject unitObj = Instantiate(inventoryUnitPrefab, inventoryContainer);
//             unitObj.name = $"Unit_{unit.uniqueId}";

//             InventoryUnitUI unitUI = unitObj.GetComponent<InventoryUnitUI>();
//             if (unitUI == null) unitUI = unitObj.AddComponent<InventoryUnitUI>();

//             unitUI.Initialize(unit, this);
//             inventoryUIMap[unit.uniqueId] = unitUI;
//             Debug.Log("unit in inventory name: " + unit.customName);
//         }

//         Debug.Log($"[FormationSetupUI] แสดง Inventory Units: {unassignedUnits.Count} ตัว");
//     }

//     public void OnUnitDraggedToSlot(UnitInstance unit, int targetRow, int targetCol)
//     {
//         if (unit == null)
//         {
//             Debug.LogError("[FormationSetupUI] พยายามลากสัตว์ null");
//             return;
//         }

//         if (!tamedManager.IsValidFormationSlot(targetRow, targetCol))
//         {
//             Debug.LogError($"[FormationSetupUI] ช่อง ({targetRow}, {targetCol}) ไม่ถูกต้อง");
//             return;
//         }

//         // ลบ Unit ออกจากตำแหน่งเก่า (ถ้ามี)
//         tamedManager.RemoveUnitFromFormationById(unit.uniqueId);

//         // จัดให้เข้า Formation ใหม่
//         tamedManager.AssignUnitToSlotById(unit.uniqueId, targetRow, targetCol);

//         // รีเฟรช UI
//         RefreshUI();
//     }

//     public void OnUnitRemovedFromSlot(string unitId)
//     {
//         if (string.IsNullOrEmpty(unitId))
//         {
//             Debug.LogError("[FormationSetupUI] พยายามลบ Unit ด้วย ID ว่าง");
//             return;
//         }

//         tamedManager.RemoveUnitFromFormationById(unitId);
//         RefreshUI();
//     }

//     public void OnTamedUnitsChanged()
//     {
//         if (isActiveAndEnabled)
//         {
//             RefreshUI();
//         }
//     }

//     private void UpdateFormationStatus()
//     {
//         int totalSlots = slotUIMap.Count;
//         int filledSlots = tamedManager.activeFormation.Count;
//         int totalUnits = tamedManager.tamedUnits.Count;

//         var unitNames = tamedManager.tamedUnits.Where(u => u != null).Select(u => u.customName).ToList();
//         string unitSummary = unitNames.Count > 0 ? string.Join(", ", unitNames) : "ยังไม่มีสัตว์";
//         string statusMsg = $"ทัพ: {filledSlots}/{totalSlots} ช่อง | สัตว์ทั้งหมด: {totalUnits} | รายชื่อ: {unitSummary}";
//         if (formationStatusText != null)
//         {
//             formationStatusText.text = statusMsg;
//         }

//         Debug.Log($"[FormationSetupUI] {statusMsg}");
//     }

//     private void ConfirmFormation()
//     {
//         Debug.Log("[FormationSetupUI] ยืนยันการจัดทัพ!");
//         // ตรวจสอบว่าทัพเสร็จครบหรือไม่
//         // อาจส่งไปยัง ArmyController เพื่อเริ่มเล่น
//         gameObject.SetActive(false); // ปิด FormationSetupUI แล้วเข้าเล่นเกม
//     }

//     private void ClearFormation()
//     {
//         Debug.Log("[FormationSetupUI] ล้างการจัดทัพทั้งหมด!");
//         tamedManager.ClearEntireFormation();
//         RefreshUI();
//     }

//     private void ClearAllUIElements()
//     {
//         foreach (Transform child in formationGridContainer)
//         {
//             Destroy(child.gameObject);
//         }
//         foreach (Transform child in inventoryContainer)
//         {
//             Destroy(child.gameObject);
//         }
//         slotUIMap.Clear();
//         inventoryUIMap.Clear();
//     }

//     public bool IsValidFormationSlot(int row, int col)
//     {
//         return tamedManager.IsValidFormationSlot(row, col);
//     }
// }

// /// <summary>
// /// UI สำหรับแต่ละช่อง (Slot) ในกริด Formation
// /// </summary>
// public class FormationSlotUI : MonoBehaviour, IDropHandler
// {
//     private int slotRow;
//     private int slotCol;
//     private FormationSetupUI setupUI;
//     private UnitInstance assignedUnit;

//     private Image slotImage;
//     private Text slotText;
//     private Button removeButton;

//     public void Initialize(int row, int col, FormationSetupUI setup)
//     {
//         slotRow = row;
//         slotCol = col;
//         setupUI = setup;

//         // ตั้งค่า UI component
//         slotImage = GetComponent<Image>();
//         if (slotImage == null) slotImage = gameObject.AddComponent<Image>();
//         slotImage.color = new Color(0.2f, 0.8f, 0.2f, 0.3f); // สีเขียวโปร่ง

//         slotText = GetComponentInChildren<Text>();
//         if (slotText == null)
//         {
//             GameObject textObj = new GameObject("Text");
//             textObj.transform.SetParent(transform);
//             slotText = textObj.AddComponent<Text>();
//             slotText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
//             slotText.fontSize = 14;
//             slotText.alignment = TextAnchor.MiddleCenter;
//         }

//         removeButton = GetComponentInChildren<Button>();
//         if (removeButton == null)
//         {
//             GameObject btnObj = new GameObject("RemoveButton");
//             btnObj.transform.SetParent(transform);
//             removeButton = btnObj.AddComponent<Button>();
//         }
//         removeButton.onClick.AddListener(RemoveUnit);

//         UpdateDisplay();
//     }

//     public void SetUnit(UnitInstance unit)
//     {
//         assignedUnit = unit;
//         UpdateDisplay();
//     }

//     private void UpdateDisplay()
//     {
//         if (assignedUnit != null)
//         {
//             slotImage.color = new Color(0.0f, 0.5f, 1.0f, 0.5f); // สีน้ำเงิน = มี Unit
//             slotText.text = $"{assignedUnit.customName}\nHP: {assignedUnit.maxHP}";
//             removeButton.gameObject.SetActive(true);
//         }
//         else
//         {
//             slotImage.color = new Color(0.2f, 0.8f, 0.2f, 0.3f); // สีเขียว = ว่าง
//             slotText.text = $"({slotRow}, {slotCol})\nว่าง";
//             removeButton.gameObject.SetActive(false);
//         }
//     }

//     public void OnDrop(PointerEventData eventData)
//     {
//         // หา InventoryUnitUI ที่ถูกลาก
//         InventoryUnitUI draggedUnit = eventData.pointerDrag?.GetComponent<InventoryUnitUI>();
//         if (draggedUnit == null)
//         {
//             Debug.Log("[FormationSlotUI] พยายามลาก non-unit object");
//             return;
//         }

//         setupUI.OnUnitDraggedToSlot(draggedUnit.GetUnit(), slotRow, slotCol);
//     }

//     private void RemoveUnit()
//     {
//         if (assignedUnit != null)
//         {
//             setupUI.OnUnitRemovedFromSlot(assignedUnit.uniqueId);
//         }
//     }
// }

// /// <summary>
// /// UI สำหรับสัตว์ในคลัง (Inventory)
// /// </summary>
// public class InventoryUnitUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
// {
//     private UnitInstance unit;
//     private FormationSetupUI setupUI;

//     private CanvasGroup canvasGroup;
//     private Image unitImage;
//     private Text unitText;

//     public void Initialize(UnitInstance unitData, FormationSetupUI setup)
//     {
//         unit = unitData;
//         setupUI = setup;

//         // ตั้งค่า CanvasGroup เพื่อ Drag & Drop
//         canvasGroup = GetComponent<CanvasGroup>();
//         if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

//         // ตั้งค่า Image
//         unitImage = GetComponent<Image>();
//         if (unitImage == null) unitImage = gameObject.AddComponent<Image>();
//         unitImage.color = new Color(0.3f, 0.7f, 1.0f, 1.0f); // สีน้ำเงินอ่อน

//         // ตั้งค่า Text แสดงข้อมูล
//         unitText = GetComponentInChildren<Text>();
//         if (unitText == null)
//         {
//             GameObject textObj = new GameObject("Text");
//             textObj.transform.SetParent(transform);
//             unitText = textObj.AddComponent<Text>();
//             unitText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
//             unitText.fontSize = 14;
//             unitText.alignment = TextAnchor.MiddleCenter;
//         }

//         unitText.text = $"{unit.customName}\nATK: {unit.attackDamage:F1}\nHP: {unit.maxHP}";
//     }

//     public UnitInstance GetUnit() => unit;

//     public void OnBeginDrag(PointerEventData eventData)
//     {
//         canvasGroup.blocksRaycasts = false;
//         canvasGroup.alpha = 0.6f;
//     }

//     public void OnDrag(PointerEventData eventData)
//     {
//         // ไม่ต้องทำอะไร การลาก Canvas Group จะอัปเดต UI เองแล้ว
//     }

//     public void OnEndDrag(PointerEventData eventData)
//     {
//         canvasGroup.blocksRaycasts = true;
//         canvasGroup.alpha = 1.0f;
//     }
// }