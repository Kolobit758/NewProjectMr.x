using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ShopUIController : MonoBehaviour
{
    public static ShopUIController Instance { get; private set; }

    [Header("UI References")]
    public GameObject shopPanel;             // หน้าต่างหลักของ Shop
    public Transform orderButtonContainer;   // Layout Group (Grid Layout) สำหรับวางปุ่มออเดอร์
    public GameObject orderButtonPrefab;     // Prefab ปุ่มออเดอร์ในร้านค้า

    [Header("Popup Reward UI")]
    public TMP_Text rewardPopupText;         // Text เด้งบอกจำนวนเงินที่ได้รับ (วางไว้ตำแหน่งเด่นๆ ใน ShopPanel หรือหน้าจอหลัก)

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (rewardPopupText != null) rewardPopupText.gameObject.SetActive(false);
    }

    // 🟢 1. ฟังก์ชันเปิดหน้าต่าง Shop (เรียกใช้จากปุ่มตึกร้านค้า หรือคีย์ลัด)
    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            RefreshShopUI(); // รีเฟรชข้อมูลออเดอร์ใหม่ทุกครั้งที่เปิด
        }
    }

    // ปิดหน้าต่าง Shop
    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    // 🟢 2. สร้างและอัปเดตหน้าจอ Grid Layout ของออเดอร์ทั้งหมด
    // 🟢 สร้างและรีเฟรชปุ่มออเดอร์ทั้งหมดใน Grid Layout
    public void RefreshShopUI()
    {
        if (orderButtonContainer == null || orderButtonPrefab == null) return;

        // ลบปุ่มเก่าทิ้งก่อนสร้างใหม่
        foreach (Transform child in orderButtonContainer)
        {
            Destroy(child.gameObject);
        }

        if (ShopOrderManager.Instance == null) return;

        // วนลูปสร้างปุ่มตามออเดอร์ที่สุ่มมา 5 แบบ
        foreach (ProceduralOrder order in ShopOrderManager.Instance.activeDailyOrders)
        {
            GameObject btnObj = Instantiate(orderButtonPrefab, orderButtonContainer);
            Button btn = btnObj.GetComponent<Button>();
            Image btnImage = btnObj.GetComponent<Image>();

            // ค้นหา Text ทั้งสองตัวจาก Prefab (รองรับทั้งหาผ่าน ContentContainer หรือหาจากตัวลูกตรงๆ)
            TMP_Text titleText = null;
            TMP_Text reqText = null;

            Transform contentContainer = btnObj.transform.Find("ContentContainer");
            if (contentContainer != null)
            {
                titleText = contentContainer.Find("TitleText")?.GetComponent<TMP_Text>();
                reqText = contentContainer.Find("RequirementText")?.GetComponent<TMP_Text>();
            }

            // ถ้าหาใน ContentContainer ไม่เจอ ลองหาจากตัวปุ่มตรงๆ อีกรอบ
            if (titleText == null) titleText = btnObj.transform.Find("TitleText")?.GetComponent<TMP_Text>();
            if (reqText == null) reqText = btnObj.transform.Find("RequirementText")?.GetComponent<TMP_Text>();

            // เช็คว่าไอเทมทุกตัวในออเดอร์นี้พอไหม
            bool canComplete = CheckIfOrderCanBeCompleted(order);

            // เปลี่ยนสีพื้นหลังปุ่ม: ของครบ = สีเหลือง, ของไม่ครบ = สีเทา
            if (btnImage != null)
            {
                btnImage.color = canComplete ? new Color(1f, 0.85f, 0.2f) : new Color(0.6f, 0.6f, 0.6f);
            }

            // 1. เซ็ตข้อความที่ TitleText (ชื่อออเดอร์ + รางวัล)
            if (titleText != null)
            {
                titleText.text = $"<b>{order.orderTitle}</b> : Reward: {order.rewardGold} G";
            }
            else
            {
                Debug.LogWarning("⚠️ [ShopUI]: หา GameObject ที่ชื่อ 'TitleText' ไม่เจอใน Prefab ปุ่ม!");
            }

            // 2. เซ็ตข้อความที่ RequirementText (รายการวัตถุดิบ 1-3 อย่าง)
            if (reqText != null)
            {
                string reqInfo = "Requires:\n";
                foreach (var req in order.requirements)
                {
                    bool hasItem = ResourceInventory.Instance.HasResource(req.requiredItem.itemName, req.amount);
                    string colorTag = hasItem ? "green" : "red";

                    reqInfo += $"- {req.requiredItem.itemName}: <color={colorTag}>{req.amount}</color>\n";
                }
                reqText.text = reqInfo;
            }
            else
            {
                Debug.LogWarning("⚠️ [ShopUI]: หา GameObject ที่ชื่อ 'RequirementText' ไม่เจอใน Prefab ปุ่ม! กรุณาตรวจสอบชื่อใน Hierarchy");
            }

            // เปิด/ปิดการคลิกปุ่มตามความพร้อมของวัตถุดิบ
            btn.interactable = canComplete;

            // ผูก Event ปุ่มกดส่งออเดอร์
            ProceduralOrder targetOrderRef = order;
            btn.onClick.AddListener(() =>
            {
                OnClickDeliverOrder(targetOrderRef);
            });
        }
    }

    // ฟังก์ชันเช็คว่าออเดอร์นี้ของพอไหม
    bool CheckIfOrderCanBeCompleted(ProceduralOrder order)
    {
        foreach (var req in order.requirements)
        {
            if (!ResourceInventory.Instance.HasResource(req.requiredItem.itemName, req.amount))
            {
                return false;
            }
        }
        return true;
    }

    // 🟢 3. เมื่อผู้เล่นกดปุ่มส่งขายออเดอร์
    void OnClickDeliverOrder(ProceduralOrder order)
    {
        if (ShopOrderManager.Instance != null)
        {
            int earnedGold = order.rewardGold;
            bool success = ShopOrderManager.Instance.CompleteOrder(order);

            if (success)
            {
                Debug.Log($"💰 [Shop]: Sold order successfully! Earned {earnedGold} Gold.");

                // อัปเดตหน้าจอ UI ร้านค้าใหม่ (ปุ่มจะหายไปหรือเปลี่ยนสถานะ)
                RefreshShopUI();

                // แสดง Popup เด้งรับเงินรางวัลแบบ Smooth Ease-In
                StartCoroutine(ShowRewardPopupRoutine(earnedGold));
            }
        }
    }

    // 🟢 4. แอนิเมชัน Popup เด้งรับเงินรางวัล
    private IEnumerator ShowRewardPopupRoutine(int goldAmount)
    {
        if (rewardPopupText != null)
        {
            rewardPopupText.text = $"+{goldAmount} Gold";
            rewardPopupText.gameObject.SetActive(true);

            RectTransform rectTrans = rewardPopupText.GetComponent<RectTransform>();
            Vector3 startPos = rectTrans.localPosition;
            Vector3 targetPos = startPos + new Vector3(0f, 60f, 0f); // ลอยขึ้นด้านบน 60 หน่วย

            Color startColor = rewardPopupText.color;
            float duration = 1.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // ใช้ Ease-In (t * t) ขยับสมูทขึ้น
                rectTrans.localPosition = Vector3.Lerp(startPos, targetPos, t * t);

                // เฟดจางลงช่วงท้าย
                if (t > 0.6f)
                {
                    float alpha = Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
                    rewardPopupText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                }

                yield return null;
            }

            rewardPopupText.gameObject.SetActive(false);
            rectTrans.localPosition = startPos;
            rewardPopupText.color = startColor;
        }
    }
}