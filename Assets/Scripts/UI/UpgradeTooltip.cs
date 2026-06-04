using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeTooltip : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private GameObject _root;
    [SerializeField] private Transform _ingredientsContainer;
    [SerializeField] private TextMeshProUGUI _messageLabel;

    [Header("Prefab")]
    [SerializeField] private UpgradeTooltipIngredient _ingredientPrefab;

    [Header("Messages")]
    [SerializeField] private string _maxLevelMessage = "Max Level";

    [Header("Follow settings")]
    [SerializeField] private Vector2 _offset = new Vector2(0f, 60f);
    [SerializeField] private RectTransform _canvasRect;

    public string MaxLevelMessage => _maxLevelMessage;

    private readonly List<UpgradeTooltipIngredient> _pool = new();

    public static UpgradeTooltip Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void ShowIngredients(RectTransform anchor, List<(Sprite icon, int have, int need)> ingredients)
    {
        _ingredientsContainer.gameObject.SetActive(true);
        _messageLabel.gameObject.SetActive(false);

        foreach (var cell in _pool)
            cell.gameObject.SetActive(false);

        while (_pool.Count < ingredients.Count)
        {
            var cell = Instantiate(_ingredientPrefab, _ingredientsContainer);
            _pool.Add(cell);
        }

        for (int i = 0; i < ingredients.Count; i++)
        {
            _pool[i].gameObject.SetActive(true);
            var (icon, have, need) = ingredients[i];
            _pool[i].SetData(icon, have, need);
        }

        _root.SetActive(true);
        PositionNear(anchor);
    }

    public void ShowMessage(RectTransform anchor, string message)
    {
        foreach (var cell in _pool)
            cell.gameObject.SetActive(false);

        _ingredientsContainer.gameObject.SetActive(false);
        _messageLabel.gameObject.SetActive(true);
        _messageLabel.text = message;

        _root.SetActive(true);
        PositionNear(anchor);
    }

    public void Hide()
    {
        _root.SetActive(false);
    }

    private void PositionNear(RectTransform anchor)
    {
        var rt = _root.GetComponent<RectTransform>();
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, anchor.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPoint, null, out Vector2 localPoint);
        rt.anchoredPosition = localPoint + _offset;
    }
}