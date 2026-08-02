using TMPro;
using UnityEngine;

public class HouseUpgradeManager : MonoBehaviour
{
    public static HouseUpgradeManager Instance { get; private set; }

    [Header("House State")]
    public int houseLevel = 1;
    public int HouseLevel => houseLevel;

    [Header("Visual References")]
    public GameObject houseLevel1Visual;
    public GameObject houseLevel2Visual;

    [Header("UI")]
    public TextMeshProUGUI houseStatusText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        houseLevel = Mathf.Max(1, houseLevel);
        RefreshVisuals();
        RefreshUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            TryUpgradeToLevel2();
        }
    }

    public void TryUpgradeToLevel2()
    {
        if (houseLevel >= 2)
        {
            Debug.Log("House is already Level 2.");
            if (HUDController.Instance != null)
            {
                HUDController.Instance.ShowPopup("House is already Level 2", "Prototype Complete is already unlocked.");
            }
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager not found. Cannot upgrade House.");
            return;
        }

        bool success = InventoryManager.Instance.SpendForHouseLevel2();
        if (!success)
        {
            return;
        }

        houseLevel = 2;
        RefreshVisuals();
        RefreshUI();

        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowPrototypeComplete();
        }

        InventoryManager.Instance.ShowDiscoveryMessage("House upgraded to Level 2!");
        Debug.Log("Prototype Complete: House upgraded to Level 2.");
    }

    private void RefreshVisuals()
    {
        if (houseLevel1Visual != null)
        {
            houseLevel1Visual.SetActive(houseLevel < 2);
        }

        if (houseLevel2Visual != null)
        {
            houseLevel2Visual.SetActive(houseLevel >= 2);
        }
    }

    private void RefreshUI()
    {
        if (houseStatusText == null)
        {
            return;
        }

        if (houseLevel >= 2)
        {
            houseStatusText.text = "House Level 2 | Prototype Complete";
        }
        else
        {
            houseStatusText.text = "House Level 1 | Need Wood x5 + Fog Dust x3 + Flame Pepper x1 | Press L";
        }
    }
}
