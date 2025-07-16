using System;

[Serializable]
public class ItemData
{
    public float PositionX;
    public float PositionY;
    public string ItemName;
    public int Quantity;

    public ItemData() { }

    public ItemData(float posX, float posY, string itemName, int quantity)
    {
        PositionX = posX;
        PositionY = posY;
        ItemName = itemName;
        Quantity = quantity;
    }
}
