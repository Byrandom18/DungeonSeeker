using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Chest : MonoBehaviour
{
    [SerializeField] private DropTableSO _dropTable;
    [SerializeField] private float _rarity = 1f;
    [SerializeField] private int _totalDropCount = 5;
    [SerializeField] private float _chancePerDropRoll = 1f;
    [SerializeField] private float _resourceChance = 0.5f;
    private InventorySO _inventorySO;

    private void Start()
    {
        _inventorySO = InventoryController.Instance.GetInventorySO();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            GainItems();
            Destroy(gameObject);
        }
    }

    private void GainItems()
    {
        _dropTable.RollMultipleDrops(_rarity, _totalDropCount, _chancePerDropRoll, _resourceChance, droppedData => { _inventorySO.AddDroppedItem(droppedData); });
    }
}
