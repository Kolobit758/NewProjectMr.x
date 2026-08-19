using UnityEngine;
using UnityEngine.UI;

public class BuildingClickOpenUI : MonoBehaviour
{
    public UnitProducerBuilding myBuilding; 

    void Start()
    {
        // ถ้าไม่ได้ลากตึกมาใส่ ให้มันพยายามหาตึกจากแม่ (Parent) ของตัวเองอัตโนมัติ
        if (myBuilding == null)
        {
            myBuilding = GetComponentInParent<UnitProducerBuilding>();
        }

        GetComponent<Button>()?.onClick.AddListener(() => {
            if (myBuilding != null)
            {
                // 🟢 เรียกผ่านฟังก์ชันของตึก เพื่อให้มันส่งข้อมูลต่อให้ BuildingProductionUI จัดการต่อ
                myBuilding.OnClickOpenUIButton();
            }
        });
    }
}   