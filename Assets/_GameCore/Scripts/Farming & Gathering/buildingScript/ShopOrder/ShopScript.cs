using UnityEngine;
using UnityEngine.UI;

public class ShopScript : MonoBehaviour
{
    public Canvas canvas;
    public Button button;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvas.worldCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        button.onClick.AddListener(OnButtonClick);
    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnButtonClick()
    {
        // ค้นหา ShopUIController แล้วสั่งเปิดหน้าต่างร้านค้าทันที
        if (ShopUIController.Instance != null)
        {
            ShopUIController.Instance.OpenShop();
            Debug.Log("🏪 [Shop]: Opening Shop UI!");
        }
        else
        {
            Debug.LogWarning("⚠️ [Shop]: ไม่พบ ShopUIController.Instance ในฉาก!");
        }
    }
}
