using TMPro;
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;

public class InventoryUI : MonoBehaviour
{
    [Header("Inventory Text")]
    public TextMeshProUGUI pepperText;
    public TextMeshProUGUI flamePepperText;
    public TextMeshProUGUI fogDustText;
    public TextMeshProUGUI woodText;

    [Header("Mastery Text")]
    public TextMeshProUGUI pepperMasteryText;

    [Header("Hint Text")]
    public TextMeshProUGUI pepperHintText;

    [UnitHeaderInspectable("Discovery Text")]
    public TextMeshProUGUI discoveryText;
    private Coroutine discoveryRoutine;

    private void Start()
    {
        if (discoveryText != null)
        {
            discoveryText.text = "";
        }
    }
    public void UpdatePepperCount(int count)
    {
        if (pepperText == null)
        {
            Debug.LogWarning("Pepper Text is missing");
            return;
        }

        pepperText.text = "Pepper: " + count;
    }

    public void UpdateFlamePepperCount(int count)
    {
        if (flamePepperText == null)
        {
            Debug.LogWarning("Flame Pepper Text is missing");
            return;
        }

        flamePepperText.text = "Flame Pepper: " + count;
    }

    public void UpdateFogDustCount(int count)
    {
        if (fogDustText == null)
        {
            return;
        }

        fogDustText.text = "Fog Dust: " + count;
    }

    public void UpdateWoodCount(int count)
    {
        if (woodText == null)
        {
            return;
        }

        woodText.text = "Wood: " + count;
    }

    public void UpdatePepperMastery(int level, int xp, int xpToNextLevel)
    {
        if (pepperMasteryText == null)
        {
            Debug.LogWarning("Pepper Mastery Text i s msiing");
            return;
        }

        if (xpToNextLevel <= 0)
        {
            pepperMasteryText.text = "Pepper Mastery Lv." + level + " | MAX";
            return;
        }

        pepperMasteryText.text = "Pepper Mastery Lv." + level + " | XP " + xp + "/" + xpToNextLevel;
    }

    public void UpdatePepperHint(string hint)
    {
        if (pepperHintText == null)
        {
            Debug.LogWarning("Pepper Hint Text is missing");
            return;
        }
        pepperHintText.text = "Hint: " + hint;
    }

    public void ShowDiscoveryMessage(string message)
    {
        if (discoveryText == null)
        {
            Debug.LogWarning("Discovery Text is missing.");
            return;
        }

        if (discoveryRoutine != null)
        {
            StopCoroutine(discoveryRoutine);
        }
        
        discoveryRoutine = StartCoroutine(ShowDiscoveryRoutine(message));
    }

    private IEnumerator ShowDiscoveryRoutine(string message)
    {
        discoveryText.text = message;
        yield return new WaitForSeconds(2.5f);
        discoveryText.text = "";
        discoveryRoutine = null;
    }

}
