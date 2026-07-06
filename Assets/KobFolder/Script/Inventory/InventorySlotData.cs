[System.Serializable]
public class InventorySlotData
{
    public SO_ItemData itemData;
    public int amount;

    public bool IsEmpty => itemData == null || amount <= 0;

    public void Clear()
    {
        itemData = null;
        amount = 0;
    }

    public void SetItem(SO_ItemData item, int count)
    {
        itemData = item;
        amount = count;
    }
}