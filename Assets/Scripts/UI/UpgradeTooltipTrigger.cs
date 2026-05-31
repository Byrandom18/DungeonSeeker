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
    // InventoryPage выставляет этот делегат после инициализации
    public Func<List<(Sprite icon, int have, int need)>> DataProvider;

    private RectTransform _rt;

    private void Awake() => _rt = GetComponent<RectTransform>();

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UpgradeTooltip.Instance == null || DataProvider == null) return;

        var data = DataProvider.Invoke();
        if (data == null || data.Count == 0) return;

        UpgradeTooltip.Instance.Show(_rt, data);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UpgradeTooltip.Instance?.Hide();
    }

    private void OnDisable()
    {
        UpgradeTooltip.Instance?.Hide();
    }
}