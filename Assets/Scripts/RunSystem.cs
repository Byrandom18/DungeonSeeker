using System;
using UnityEngine;

/// <summary>
/// Manages hub/run inventory lifecycle.
/// Rules:
/// - StartRun: move equipped items Hub -> Run.
/// - CompleteRun: move all items Run -> Hub.
/// - FailRun: clear Run inventory (all run items are lost).
/// </summary>
public class RunSystem : MonoBehaviour
{
    public static RunSystem Instance { get; private set; }

    [SerializeField] private InventorySO _hubInventorySO;
    [SerializeField] private InventorySO _runInventorySO;
    [SerializeField] private InventoryController _inventoryController;

    public bool IsInRun { get; private set; }

    public event Action<bool> OnRunStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (_inventoryController == null)
            _inventoryController = InventoryController.Instance;

        SwitchToHubInventory();
    }

    public bool StartRun()
    {
        if (IsInRun) return false;
        if (_hubInventorySO == null || _runInventorySO == null) return false;

        _runInventorySO.ClearAll(notify: false);
        _hubInventorySO.TransferEquippedTo(_runInventorySO);

        IsInRun = true;
        SwitchToRunInventory();
        OnRunStateChanged?.Invoke(IsInRun);
        return true;
    }

    public bool CompleteRun()
    {
        if (!IsInRun) return false;
        if (_hubInventorySO == null || _runInventorySO == null) return false;

        _runInventorySO.TransferAllTo(_hubInventorySO);

        IsInRun = false;
        SwitchToHubInventory();
        OnRunStateChanged?.Invoke(IsInRun);
        return true;
    }

    public bool FailRun()
    {
        if (!IsInRun) return false;
        if (_runInventorySO == null) return false;

        _runInventorySO.ClearAll();

        IsInRun = false;
        SwitchToHubInventory();
        OnRunStateChanged?.Invoke(IsInRun);
        return true;
    }

    private void SwitchToHubInventory()
    {
        if (_inventoryController == null)
            _inventoryController = InventoryController.Instance;
        _inventoryController?.SetInventorySO(_hubInventorySO);
    }

    private void SwitchToRunInventory()
    {
        if (_inventoryController == null)
            _inventoryController = InventoryController.Instance;
        _inventoryController?.SetInventorySO(_runInventorySO);
    }
}
