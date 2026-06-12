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

[Serializable]
public struct RarityIngredient
{
    public ItemRarity Rarity;
    public ItemSO Resource;
    public int Quantity;
}


[CreateAssetMenu(fileName = "UpgradeRecipeSO", menuName = "Scriptable Objects/UpgradeRecipeSO")]
public class UpgradeRecipeSO : ScriptableObject
{
    [SerializeField] private List<UpgradeStep> _steps = new List<UpgradeStep>();

    [Header("Rarity-based extra ingredients")]
    [SerializeField] private List<RarityIngredient> _rarityIngredients = new List<RarityIngredient>();


    public List<UpgradeIngredient> GetIngredientsForLevel(int targetLevel, ItemRarity rarity)
    {
        var result = new List<UpgradeIngredient>();

        foreach (var step in _steps)
        {
            if (step.Level == targetLevel && step.Ingredients != null)
            {
                result.AddRange(step.Ingredients);
                break;
            }
        }

        foreach (var ri in _rarityIngredients)
        {
            if (ri.Rarity == rarity && ri.Resource != null)
            {
                result.Add(new UpgradeIngredient
                {
                    Resource = ri.Resource,
                    Quantity = ri.Quantity
                });
                break;
            }
        }

        return result;
    }

    public bool CanUpgrade(int targetLevel, ItemRarity rarity, IReadOnlyList<InventoryItemData> items)
    {
        var ingredients = GetIngredientsForLevel(targetLevel, rarity);

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
