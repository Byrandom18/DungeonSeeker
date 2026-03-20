using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private InventoryPageUI _inventoryPage;

    [SerializeField] private int _inventorySize = 20;

    private void Start()
    {
        GameInput.Instance.OnInventoryButton += GameInput_OnInventoryButton;
        _inventoryPage.InitializeInventoryUI(_inventorySize);
    }

    private void GameInput_OnInventoryButton(object sender, System.EventArgs e)
    {
        InventorySwitch();
    }

    private void InventorySwitch()
    {
        if (!_inventoryPage.isActiveAndEnabled)
        {
            _inventoryPage.ShowInventoryUI();
        }
        else
        {
            _inventoryPage.HideInventoryUI();
        }
    }
}
