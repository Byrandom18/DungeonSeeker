using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryDescription : MonoBehaviour
{
    [SerializeField] private Image _itemImage;
    //[SerializeField] private TMP_Text _quantity;
    [SerializeField] private TMP_Text _tilte;
    [SerializeField] private TMP_Text _description;

    private void Awake()
    {
        //ResetDescription();
    }

    public void ResetDescription()
    {
        _itemImage.gameObject.SetActive(false);
        _tilte.text = "";
        _description.text = "";
    }

    public void SetDescription(Sprite sprite, int quantity, string itemName, string description)
    {
        _itemImage.gameObject.SetActive(true);
        _itemImage.sprite = sprite;
        //_quantity.text = quantity.ToString();
        _tilte.text = itemName;
        _description.text = description;
    }
}
