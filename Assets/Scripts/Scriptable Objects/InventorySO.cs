using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InventorySO", menuName = "Scriptable Objects/InventorySO")]
public class InventorySO : ScriptableObject
{
    [SerializeField] private List<InventoryItemStruct> inventoryItems;


}

[Serializable]
public struct InventoryItemStruct
{
    public int Quantity;
    public ItemSO Item;


    public InventoryItemStruct ChangeQuantity(int newQuantity)
    {
        return new InventoryItemStruct
        {
            Item = this.Item,
            Quantity = newQuantity
        };
    }
}