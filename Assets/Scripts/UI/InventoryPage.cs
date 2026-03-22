using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryPage : MonoBehaviour
{
    [SerializeField] private InventoryItem _itemPrefab;
    
    [SerializeField] private RectTransform _contentPanel;
    [SerializeField] private InventoryDescription _itemDescription;

    private List<InventoryItem> _listOfItems = new List<InventoryItem>();

    public event Action<int> OnDescriptionRequested;

    private Sprite _sprite;
    private int _quantity;
    private string _title;
    private string _description;

    private void Awake()
    {
        HideInventory();
        _itemDescription.ResetDescription();
    }


    public void InitializeInventoryUI(int inventorySize)
    {
        for (int i = 0; i < inventorySize; i++)
        {
            InventoryItem uiItem = Instantiate(_itemPrefab, Vector3.zero, Quaternion.identity);
            uiItem.transform.SetParent(_contentPanel, false);
            _listOfItems.Add(uiItem);
            uiItem.OnItemClick += UiItem_OnItemClick;
            uiItem.OnItemHovered += UiItem_OnItemHovered;
            uiItem.OnItemUnhovered += UiItem_OnItemUnhovered;
        }
    }

    private void UiItem_OnItemUnhovered(InventoryItem item)
    {
        
    }

    private void UiItem_OnItemHovered(InventoryItem item)
    {
        
    }

    private void UiItem_OnItemClick(InventoryItem item)
    {
        SetTempFields(item);
        _itemDescription.SetDescription(_sprite, _quantity, _title, _description);
    }

    public void ShowInventory()
    {
        gameObject.SetActive(true);
        _itemDescription.ResetDescription();
        if (_listOfItems.Count <= 0 ) return;

        SetTempFields(_listOfItems[0]);
        _itemDescription.SetDescription(_sprite, _quantity, _title, _description);
    }

    public void HideInventory()
    {
        gameObject.SetActive(false);    
    }

    private void SetTempFields(InventoryItem item)
    {
        _sprite = item.Sprite;
        _title = item.Name; 
        _description = item.Description;
        _quantity = item.Quantity;
    }

    private void OnDestroy()
    {
        foreach (var uiItem in _listOfItems)
        {
            uiItem.OnItemClick -= UiItem_OnItemClick;
            uiItem.OnItemHovered -= UiItem_OnItemHovered;
            uiItem.OnItemUnhovered -= UiItem_OnItemUnhovered;
        }
    }
}
