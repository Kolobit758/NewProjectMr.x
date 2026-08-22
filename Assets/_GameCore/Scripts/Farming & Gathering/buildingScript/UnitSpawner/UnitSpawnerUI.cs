using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitSpawnerUI : MonoBehaviour
{
    public static UnitSpawnerUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject spawnerPanel;      // หน้าต่าง UI สร้างยูนิต
    public TMP_Text titleText;           // ชื่อตึกปัจจุบัน
    public Button spawnButton;           // ปุ่มกดสร้างยูนิต
    public TMP_Text buttonLabelText;     // ข้อความบนปุ่ม (เช่น "Train Unit (Cost: 50G)")

    private UnitSpawnerBuilding activeSpawner;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (spawnerPanel != null) spawnerPanel.SetActive(false);
        if (spawnButton != null)
        {
            spawnButton.onClick.AddListener(OnClickSpawnButton);
        }
    }

    public void OpenSpawnerPanel(UnitSpawnerBuilding spawner)
    {
        activeSpawner = spawner;
        if (spawnerPanel != null)
        {
            spawnerPanel.SetActive(true);
            UpdateUIInfo();
        }
    }

    public void CloseSpawnerPanel()
    {
        activeSpawner = null;
        if (spawnerPanel != null) spawnerPanel.SetActive(false);
    }

    void UpdateUIInfo()
    {
        if (activeSpawner == null) return;

        if (titleText != null)
        {
            titleText.text = activeSpawner.spawnerType == UnitSpawnerBuilding.SpawnerType.AnimalShelter ? "Animal Shelter Spawner" : "Robot Factory Spawner";
        }

        if (buttonLabelText != null)
        {
            buttonLabelText.text = $"Train Unit\nCost: {activeSpawner.unitCostGold} G";
        }
    }

    void OnClickSpawnButton()
    {
        if (activeSpawner != null)
        {
            activeSpawner.RequestSpawnUnit();
            CloseSpawnerPanel();
        }
    }
}