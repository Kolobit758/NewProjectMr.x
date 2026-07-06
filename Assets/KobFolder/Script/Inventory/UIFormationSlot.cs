using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class UIFormationSlot : MonoBehaviour
{
    public int Row { get; private set; }
    public int Col { get; private set; }

    [Header("UI Components")]
    public TextMeshProUGUI slotInfoText; 
    public Image slotBackgroundImage;    

    [Header("Color States")]
    public Color disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f); 
    public Color emptyColor = new Color(0.4f, 0.8f, 0.4f, 0.3f);    
    public Color defaultUnitColor = new Color(0.2f, 0.5f, 0.8f, 0.8f); 
    public Color tamedUnitColor = new Color(0.9f, 0.6f, 0.2f, 0.8f);   

    private UITeamManager managerRef;
    private Button myButton;
    private bool isValidSlot = false;

    public void SetupSlot(int row, int col, UITeamManager manager)
    {
        Row = row;
        Col = col;
        managerRef = manager;

        myButton = GetComponent<Button>();
        myButton.onClick.RemoveAllListeners();
        myButton.onClick.AddListener(HandleSlotClick);

        if (TeamFormationManager.Instance != null)
        {
            isValidSlot = TeamFormationManager.Instance.IsValidFormationSlot(row, col);
        }
        else
        {
            // ถ้าดึงไม่ได้ ให้เปิดเป็น true ไว้ก่อนเพื่อความปลอดภัย
            isValidSlot = true; 
        }
    }

    public void RefreshSlotState(UnitInstance tamedUnit, bool isDefaultUnit, string defaultUnitName)
    {
        if (myButton == null) myButton = GetComponent<Button>();

        // 🚨 บังคับเปิดปุ่มให้ Interactable = true เสมอ เพื่อแก้ปัญหาปุ่มกดไม่ติด
        myButton.interactable = true; 

        if (isDefaultUnit)
        {
            if (slotBackgroundImage != null) slotBackgroundImage.color = defaultUnitColor;
            if (slotInfoText != null) slotInfoText.text = $"[ทหาร]\n{defaultUnitName}";
        }
        else if (tamedUnit != null)
        {
            if (slotBackgroundImage != null) slotBackgroundImage.color = tamedUnitColor;
            if (slotInfoText != null) slotInfoText.text = $"[Tame]\n{tamedUnit.customName}";
        }
        else
        {
            // ถ้าเป็นช่องปกติ หรือช่องที่โดนล็อกแต่เราอยากเทส ให้เปิดเป็นสีเขียวว่างเปล่า
            if (slotBackgroundImage != null) 
            {
                slotBackgroundImage.color = isValidSlot ? emptyColor : disabledColor;
            }
            if (slotInfoText != null) slotInfoText.text = $"{Row}:{Col}";
        }
    }

    private void HandleSlotClick()
    {
        // 🌟 บรรทัดนี้จะบังคับพ่น Log ลง Console ทันทีเมื่อนิ้วจิ้มโดนปุ่ม!
        Debug.Log($"[CRITICAL CHECK] ปุ่มช่องโดนเมาส์คลิกจริง! พิกัด Row:{Row}, Col:{Col} | isValidSlot = {isValidSlot}");

        if (managerRef != null)
        {
            // 🚨 บังคับส่งข้อมูลข้ามไปฝั่ง Manager ทันทีโดยไม่สนว่าช่องจะโดนล็อกใน SO ไหม
            managerRef.OnGridSlotClicked(Row, Col);
        }
    }
}