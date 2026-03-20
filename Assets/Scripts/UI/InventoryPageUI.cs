using System.Collections.Generic;
using UnityEngine;

public class InventoryPageUI : MonoBehaviour
{
    [SerializeField] private InventoryItemUI _itemPrefab;

    [SerializeField] private RectTransform _contentPanel;

    private List<InventoryItemUI> _listOfItemsUI = new List<InventoryItemUI>();

    public void InitializeInventoryUI(int inventorySize)
    {
        for (int i = 0; i < inventorySize; i++)
        {
            InventoryItemUI uiItem = Instantiate(_itemPrefab, Vector3.zero, Quaternion.identity);
            uiItem.transform.SetParent(_contentPanel, false);
            _listOfItemsUI.Add(uiItem);
        }
    }

    public void ShowInventoryUI()
    {
        gameObject.SetActive(true);
    }

    public void HideInventoryUI()
    {
        gameObject.SetActive(false);    
    }
}
