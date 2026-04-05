using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public struct UpgradeIngredient
{
    public ItemSO Resource;
    public int Quantity;
}

[Serializable]
public struct UpgradeStep
{
    [Tooltip("Требования для одного уровня улучшения. С какого уровня улучшения применяется этот шаг (1 = первое улучшение)")]
    public int Level;
    public List<UpgradeIngredient> Ingredients;
}

[CreateAssetMenu(fileName = "UpgradeRecipeSO", menuName = "Scriptable Objects/UpgradeRecipeSO")]
public class UpgradeRecipeSO : ScriptableObject
{
    [SerializeField] private List<UpgradeStep> _steps = new List<UpgradeStep>();

    /// <summary>
    /// Возвращает список ингредиентов для улучшения до указанного уровня.
    /// Если рецепт для этого уровня не задан — возвращает null.
    /// </summary>
    public List<UpgradeIngredient> GetIngredientsForLevel(int targetLevel)
    {
        foreach (var step in _steps)
        {
            if (step.Level == targetLevel)
                return step.Ingredients;
        }
        return null;
    }

    /// <summary>
    /// Проверяет, можно ли улучшить предмет до targetLevel при наличии items в инвентаре.
    /// </summary>
    public bool CanUpgrade(int targetLevel, IReadOnlyList<InventoryItemData> items)
    {
        var ingredients = GetIngredientsForLevel(targetLevel);
        if (ingredients == null) return false;

        foreach (var ingredient in ingredients)
        {
            if (ingredient.Resource == null) continue;
            int available = GetAvailableQuantity(ingredient.Resource, items);
            if (available < ingredient.Quantity) return false;
        }
        return true;
    }

    private static int GetAvailableQuantity(ItemSO resource, IReadOnlyList<InventoryItemData> items)
    {
        foreach (var data in items)
        {
            if (data.Item == resource) return data.Quantity;
        }
        return 0;
    }
}
