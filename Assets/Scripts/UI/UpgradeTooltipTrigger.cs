using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to the Upgrade button GameObject.
/// Calls DataProvider to get ingredient data when the pointer enters.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UpgradeTooltipTrigger : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    public Func<(List<(Sprite icon, int have, int need)> ingredients, string message)> DataProvider;

    private RectTransform _rt;

    private bool _isHovered;

    private void Awake() => _rt = GetComponent<RectTransform>();


    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        RefreshTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        UpgradeTooltip.Instance?.Hide();
    }

    private void OnDisable()
    {
        _isHovered = false;
        UpgradeTooltip.Instance?.Hide();
    }

    private void Update()
    {
        if (_isHovered)
            RefreshTooltip();
    }

    private void RefreshTooltip()
    {
        if (UpgradeTooltip.Instance == null || DataProvider == null) return;

        var (ingredients, message) = DataProvider.Invoke();

        if (!string.IsNullOrEmpty(message))
        {
            UpgradeTooltip.Instance.ShowMessage(_rt, message);
            return;
        }

        if (ingredients == null || ingredients.Count == 0)
        {
            UpgradeTooltip.Instance.Hide();
            return;
        }

        UpgradeTooltip.Instance.ShowIngredients(_rt, ingredients);
    }
}