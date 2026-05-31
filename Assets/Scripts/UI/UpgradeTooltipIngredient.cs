using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Single ingredient slot inside the upgrade tooltip.
/// Prefab layout: Image (icon) + TextMeshProUGUI (x/y label)
/// </summary>
public class UpgradeTooltipIngredient : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _label;

    // ÷вет текста когда ресурсов достаточно / не хватает
    [SerializeField] private Color _enoughColor = new Color(0.3f, 1f, 0.3f);
    [SerializeField] private Color _missingColor = new Color(1f, 0.3f, 0.3f);

    public void SetData(Sprite icon, int have, int need)
    {
        _icon.sprite = icon;
        _icon.enabled = icon != null;

        _label.text = $"{have}/{need}";
        _label.color = have >= need ? _enoughColor : _missingColor;
    }
}