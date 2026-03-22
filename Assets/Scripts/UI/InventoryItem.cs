using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _itemImage;
    [SerializeField] private TMP_Text _quantityText;
    [SerializeField] private Image _borderImage;

    public Sprite Sprite;
    public int Quantity = 1;
    public string Name = "Item name";
    public string Description = "Item Description";

    public event Action<InventoryItem> OnItemClick, OnItemHovered, OnItemUnhovered;

    private void Awake()
    {
        ResetData();
        Decelect();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnItemClick?.Invoke(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnItemHovered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnItemUnhovered?.Invoke(this);
    }

    private void ResetData()
    {

    }

    private void Decelect()
    {
        _borderImage.enabled = false;
    }

    public void SetData(Sprite sprite, int quantity)
    {
        _itemImage.sprite = sprite;
        _quantityText.text = quantity.ToString();
    }

    private void Select()
    {
        _borderImage.enabled = true;
    }
}
