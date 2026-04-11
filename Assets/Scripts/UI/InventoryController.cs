using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private InventoryPage _inventoryPage;
    [SerializeField] private InventorySO _inventorySO;
    public static InventoryController Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GameInput.Instance.OnInventoryButton += GameInput_OnInventoryButton;
    }

    public InventorySO GetInventorySO()
    {
        return _inventorySO;
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
