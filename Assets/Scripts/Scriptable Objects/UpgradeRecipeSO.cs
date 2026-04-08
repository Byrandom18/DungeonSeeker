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
    public int Level;
    public List<UpgradeIngredient> Ingredients;
}

[CreateAssetMenu(fileName = "UpgradeRecipeSO", menuName = "Scriptable Objects/UpgradeRecipeSO")]
public class UpgradeRecipeSO : ScriptableObject
{
    [SerializeField] private List<UpgradeStep> _steps = new List<UpgradeStep>();

    public List<UpgradeIngredient> GetIngredientsForLevel(int targetLevel)
    {
        foreach (var step in _steps)
        {
            if (step.Level == targetLevel)
                return step.Ingredients;
        }
        return null;
    }

    public bool CanUpgrade(int targetLevel, IReadOnlyList<InventoryItemData> items)
    {
        var ingredients = GetIngredientsForLevel(targetLevel);
        if (ingredients == null) return true;

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
