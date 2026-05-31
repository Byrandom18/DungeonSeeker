using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton-like tooltip panel. Place it on a root Canvas child
/// so it always renders on top. Assign via Inspector or find via FindAnyObjectByType.
/// </summary>
public class UpgradeTooltip : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private GameObject _root;          // панель целиком
    [SerializeField] private Transform _ingredientsContainer; // HorizontalLayoutGroup

    [Header("Prefab")]
    [SerializeField] private UpgradeTooltipIngredient _ingredientPrefab; // см. скрипт ниже

    [Header("Follow settings")]
    [SerializeField] private Vector2 _offset = new Vector2(0f, 60f);
    [SerializeField] private RectTransform _canvasRect; // корневой Canvas RectTransform

    private readonly List<UpgradeTooltipIngredient> _pool = new();

    public static UpgradeTooltip Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    /// <summary>
    /// Показывает тултип у позиции кнопки.
    /// ingredients: список (спрайт, имеющееся кол-во, необходимое кол-во)
    /// </summary>
    public void Show(
        RectTransform anchor,
        List<(Sprite icon, int have, int need)> ingredients)
    {
        // Заполняем пул
        while (_pool.Count < ingredients.Count)
        {
            var cell = Instantiate(_ingredientPrefab, _ingredientsContainer);
            _pool.Add(cell);
        }

        for (int i = 0; i < _pool.Count; i++)
        {
            if (i < ingredients.Count)
            {
                _pool[i].gameObject.SetActive(true);
                var (icon, have, need) = ingredients[i];
                _pool[i].SetData(icon, have, need);
            }
            else
            {
                _pool[i].gameObject.SetActive(false);
            }
        }

        _root.SetActive(true);

        // Позиционирование рядом с кнопкой
        PositionNear(anchor);
    }

    public void Hide()
    {
        _root.SetActive(false);
    }

    private void PositionNear(RectTransform anchor)
    {
        var rt = _root.GetComponent<RectTransform>();

        // Конвертируем мировую позицию якоря в локальную позицию канваса
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, anchor.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPoint, null, out Vector2 localPoint);

        rt.anchoredPosition = localPoint + _offset;
    }
}