using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(UIAnimationController))] // 🟢 บังคับให้ต้องมีสคริปต์อนิเมชันร่วมด้วย
public class ShopUIController : MonoBehaviour
{
    public static ShopUIController Instance { get; private set; }

    [Header("UI References")]
    public GameObject shopPanel;             // หน้าต่างหลักของ Shop
    public Transform orderButtonContainer;   // Layout Group (Grid Layout) สำหรับวางปุ่มออเดอร์
    public GameObject orderButtonPrefab;     // Prefab ปุ่มออเดอร์ในร้านค้า

    [Header("Popup Reward UI")]
    public TMP_Text rewardPopupText;         // Text เด้งบอกจำนวนเงินที่ได้รับ

    private UIAnimationController animController; // 🟢 ตัวควบคุมแอนิเมชันและฝุ่น

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // ดึงคอมโพเนนต์แอนิเมชันจากหน้าต่าง Shop Panel
        if (shopPanel != null)
        {
            animController = shopPanel.GetComponent<UIAnimationController>();
        }
    }

    void Start()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (rewardPopupText != null) rewardPopupText.gameObject.SetActive(false);
    }

    // 🟢 1. ฟังก์ชันเปิดหน้าต่าง Shop ผ่าน UIAnimationController (สไลด์ + ฝุ่น)
    public void OpenShop()
    {
        if (shopPanel != null)
        {
            if (animController != null)
            {
                animController.OpenUI(); // เปิดพร้อมแอนิเมชันและฝุ่น
            }
            else
            {
                shopPanel.SetActive(true);
            }
            RefreshShopUI(); // รีเฟรชข้อมูลออเดอร์ใหม่ทุกครั้งที่เปิด
        }
    }

    // 🔴 ปิดหน้าต่าง Shop ผ่าน UIAnimationController (สไลด์ออก + ฝุ่น แล้วปิดตัวเอง)
    public void CloseShop()
    {
        if (shopPanel != null)
        {
            if (animController != null)
            {
                animController.CloseUI(); // ปิดพร้อมแอนิเมชันสไลด์เก็บและฝุ่น
            }
            else
            {
                shopPanel.SetActive(false);
            }
        }
    }

    // 🟢 2. สร้างและรีเฟรชปุ่มออเดอร์ทั้งหมดใน Grid Layout
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

            TMP_Text titleText = null;
            TMP_Text reqText = null;

            Transform contentContainer = btnObj.transform.Find("ContentContainer");
            if (contentContainer != null)
            {
                titleText = contentContainer.Find("TitleText")?.GetComponent<TMP_Text>();
                reqText = contentContainer.Find("RequirementText")?.GetComponent<TMP_Text>();
            }

            if (titleText == null) titleText = btnObj.transform.Find("TitleText")?.GetComponent<TMP_Text>();
            if (reqText == null) reqText = btnObj.transform.Find("RequirementText")?.GetComponent<TMP_Text>();

            bool canComplete = CheckIfOrderCanBeCompleted(order);

            if (btnImage != null)
            {
                btnImage.color = canComplete ? new Color(1f, 0.85f, 0.2f) : new Color(0.6f, 0.6f, 0.6f);
            }

            if (titleText != null)
            {
                titleText.text = $"<b>{order.orderTitle}</b> : Reward: {order.rewardGold} G";
            }

            if (reqText != null)
            {
                reqText.text = BuildRequirementText(order);
            }

            btn.interactable = canComplete;

            ProceduralOrder targetOrderRef = order;
            btn.onClick.AddListener(() =>
            {
                OnClickDeliverOrder(targetOrderRef);
            });

        }
    }

    // 🟢 3. สร้างข้อความรายการ requirement พร้อมบอกว่า "มีเท่าไหร่ / ต้องการเท่าไหร่" และ "ขาดเท่าไหร่"
    string BuildRequirementText(ProceduralOrder order)
    {
        string reqInfo = "Requires:\n";

        foreach (var req in order.requirements)
        {
            int currentAmount = ResourceInventory.Instance.GetResourceAmount(req.requiredItem.itemName);
            bool hasEnough = currentAmount >= req.amount;
            string colorTag = hasEnough ? "green" : "red";

            // มี/ต้องการ เช่น "5/10"
            reqInfo += $"- {req.requiredItem.itemName}: <color={colorTag}>{currentAmount}/{req.amount}</color>";

            // ถ้ายังไม่พอ บอกจำนวนที่ขาดต่อท้ายไปเลย

            reqInfo += $" <color=red>(you have {currentAmount})</color>";


            reqInfo += "\n";
        }

        return reqInfo;
    }

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

    void OnClickDeliverOrder(ProceduralOrder order)
    {
        if (ShopOrderManager.Instance != null)
        {
            int earnedGold = order.rewardGold;
            bool success = ShopOrderManager.Instance.CompleteOrder(order);

            if (success)
            {
                Debug.Log($"💰 [Shop]: Sold order successfully! Earned {earnedGold} Gold.");
                RefreshShopUI();
                StartCoroutine(ShowRewardPopupRoutine(earnedGold));
            }
        }
    }

    private IEnumerator ShowRewardPopupRoutine(int goldAmount)
    {
        if (rewardPopupText != null)
        {
            rewardPopupText.text = $"+{goldAmount} Gold";
            rewardPopupText.gameObject.SetActive(true);

            RectTransform rectTrans = rewardPopupText.GetComponent<RectTransform>();
            Vector3 startPos = rectTrans.localPosition;
            Vector3 targetPos = startPos + new Vector3(0f, 60f, 0f);

            Color startColor = rewardPopupText.color;
            float duration = 1.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                rectTrans.localPosition = Vector3.Lerp(startPos, targetPos, t * t);

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