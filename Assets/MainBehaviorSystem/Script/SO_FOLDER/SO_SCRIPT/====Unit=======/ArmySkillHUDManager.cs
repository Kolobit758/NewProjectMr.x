using UnityEngine;
using System.Collections.Generic;

public class ArmySkillHUDManager : MonoBehaviour
{
    public static ArmySkillHUDManager Instance { get; private set; }

    public GameObject skillCardPrefab;
    public Transform cardContainer; 

    [Header("Dynamic Fan-Out Settings")]
    [Tooltip("ความกว้างรวมของแผงการ์ดสูงสุด (เช่น 500-600) ยิ่งเยอะการ์ดยิ่งแผ่กว้าง")]
    public float totalFanWidth = 600f;     
    
    [Tooltip("ระยะห่างสูงสุดระหว่างการ์ดสองใบ (ป้องกันตอนการ์ดมีแค่ 2 ใบแล้วห่างกันเกินไป)")]
    public float maxSpacing = 160f;        
    
    [Tooltip("ความลึกรวมของส่วนโค้ง ยิ่งเยอะยิ่งนูนตรงกลาง")]
    public float arcIntensity = 20f;       
    
    [Tooltip("มุมเอียงสูงสุดของการ์ดใบขอบ")]
    public float maxRotationAngle = 10f;   

    private List<GameObject> activeCards = new List<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    public void RefreshSkillCards(List<UnitInstance> spawnedInstances, List<GameObject> unitGameObjects)
    {
        foreach (var card in activeCards) { if (card != null) Destroy(card); }
        activeCards.Clear();

        List<UnitSkillCardUI> createdCardUIs = new List<UnitSkillCardUI>();

        for (int i = 0; i < spawnedInstances.Count; i++)
        {
            if (i >= unitGameObjects.Count) break;
            if (spawnedInstances[i].template.uniqueSkill == null) continue;

            GameObject newCard = Instantiate(skillCardPrefab, cardContainer);
            UnitSkillCardUI cardUI = newCard.GetComponent<UnitSkillCardUI>();
            
            cardUI.SetupCard(spawnedInstances[i], unitGameObjects[i]);
            activeCards.Add(newCard);
            createdCardUIs.Add(cardUI);
        }

        // 🟢 จัดพัดโค้งแบบคำนวณจำนวนการ์ด
        PositionCardsInArc(createdCardUIs);
    }

    private void PositionCardsInArc(List<UnitSkillCardUI> cardUIs)
    {
        int cardCount = cardUIs.Count;
        if (cardCount == 0) return;

        // 1. 🟢 [Dynamic Spacing] คำนวณระยะห่างอัตโนมัติตามจำนวนการ์ด
        // สูตร: เอาความกว้างแผงตั้ง หารด้วยจำนวนช่องว่าง ถ้าการ์ดน้อยมันจะถ่างออกเท่ากับ maxSpacing
        float dynamicSpacing = maxSpacing;
        if (cardCount > 1)
        {
            dynamicSpacing = totalFanWidth / (cardCount - 1);
            dynamicSpacing = Mathf.Min(dynamicSpacing, maxSpacing); // ล็อกไว้ไม่ให้ห่างเกินเพดานที่ตั้งไว้
        }

        // หาจุดกึ่งกลางอ้างอิง
        float midIndex = (cardCount - 1) / 2f;

        for (int i = 0; i < cardCount; i++)
        {
            RectTransform rect = cardUIs[i].GetComponent<RectTransform>();
            
            // คำนวณระยะห่างจากจุดกึ่งกลาง (เช่น -1.5, -0.5, 0.5, 1.5)
            float offsetFromMiddle = i - midIndex;
            
            // 2. พิกัดแกน X (แผ่กระจายตามค่าระยะห่างแบบไดนามิกที่คำนวณได้)
            float xPos = offsetFromMiddle * dynamicSpacing;

            // 3. พิกัดแกน Y (ความโค้งแบบทรงพาราโบลาคว่ำ ให้สัมพันธ์กับสัดส่วน offset)
            float yPos = 0f;
            if (cardCount > 1)
            {
                // หารด้วย midIndex เพื่อกระจายสัดส่วนความลาดเอียงให้เรียบเนียนขึ้น
                float normalizedOffset = offsetFromMiddle / midIndex;
                yPos = -Mathf.Pow(normalizedOffset, 2) * arcIntensity;
            }

            // 4. มุมหมุนแกน Z (ใบซ้ายหันซ้าย ใบขวาหันขวา)
            float zRot = 0f;
            if (cardCount > 1)
            {
                zRot = -offsetFromMiddle * (maxRotationAngle / midIndex);
            }

            // 🟢 บันทึกข้อมูลพิกัดล็อกตำแหน่งไว้ให้ตัวการ์ดใช้ดึงกลับตอนปล่อยเมาส์
            cardUIs[i].initialLocalPosition = new Vector3(xPos, yPos, 0f);
            cardUIs[i].initialLocalRotation = Quaternion.Euler(0f, 0f, zRot);

            // ย้ายพิกัด UI จริง
            rect.localPosition = cardUIs[i].initialLocalPosition;
            rect.localRotation = cardUIs[i].initialLocalRotation;
        }
    }
}