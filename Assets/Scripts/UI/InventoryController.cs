using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private InventoryPage _inventoryPage;


    private void Start()
    {
        GameInput.Instance.OnInventoryButton += GameInput_OnInventoryButton;
    }

    private void GameInput_OnInventoryButton(object sender, System.EventArgs e)
    {
        ToggleInventory();
    }

    private void ToggleInventory()
    {
        if (!_inventoryPage.isActiveAndEnabled)
        {
            _inventoryPage.ShowInventory();
        }
        else
        {
            _inventoryPage.HideInventory();
        }
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.OnInventoryButton -= GameInput_OnInventoryButton;
    }
}
