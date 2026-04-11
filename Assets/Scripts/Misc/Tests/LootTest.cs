using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LootTest : MonoBehaviour
{
    [SerializeField] private float Rarity = 1.0f;

    [SerializeField] private TMP_Text text1;
    [SerializeField] private TMP_Text text2;
    private static readonly float[] _baseWeights = { 3125f, 625f, 125f, 25f, 5f, 1f };
    private static readonly float[] _baseMods = { 0.1f, 0.2f, 0.35f, 0.5f, 0.75f, 1f };
    private static readonly float[] _baseMaxMulti = { 1f, 5f, 25f, 125f, 625f, Mathf.Infinity };
    [Header("Base Weights")]
    [SerializeField] private float baseWeight1 = 10000f;
    [SerializeField] private float baseWeight2 = 10000f;
    [SerializeField] private float baseWeight3 = 10000f;
    [SerializeField] private float baseWeight4 = 10000f;
    [SerializeField] private float baseWeight5 = 10000f;
    [SerializeField] private float baseWeight6 = 10000f;

    [Header("Modifier Settings")]
    [SerializeField] private float mod1 = 10000f;
    [SerializeField] private float mod2 = 10000f;
    [SerializeField] private float mod3 = 10000f;
    [SerializeField] private float mod4 = 10000f;
    [SerializeField] private float mod5 = 10000f;
    [SerializeField] private float mod6 = 10000f;

    [Header("Power Settings")]
    [SerializeField] private float power1 = 10000f;
    [SerializeField] private float power2 = 10000f;
    [SerializeField] private float power3 = 10000f;
    [SerializeField] private float power4 = 10000f;
    [SerializeField] private float power5 = 10000f;
    //[SerializeField] private float power6 = 10000f;


    public void Roll()
    {
        RollRarity(Rarity, false);
        PrintRarity();
    }

    private void PrintRarity()
    {
        Debug.Log(GameUtils.Utils.RollRarity(Rarity));
    }

    private void RollRarity(float rarity, bool debug = false)
    {
        float bonus = Mathf.Max(0, rarity - 1);
        text1.text = text2.text;
        // Расчет весов с кривыми роста
        float[] weights = new float[7];
        //weights[1] = baseWeight1;
        //weights[2] = baseWeight2 * Mathf.Pow(1 + bonus * mod2, power2);
        //weights[3] = baseWeight3 * Mathf.Pow(1 + bonus * mod3, power3);
        //weights[4] = baseWeight4 * Mathf.Pow(1 + bonus * mod4, power4);
        //weights[5] = baseWeight5 * Mathf.Pow(1 + bonus * mod5, power5);
        //weights[6] = baseWeight6 * Mathf.Pow(1 + bonus * mod6, power6);
        //weights[1] = baseWeight1;
        //weights[2] = baseWeight2 * GetMultiplier(bonus, mod2, power2);
        //weights[3] = baseWeight3 * GetMultiplier(bonus, mod3, power3);
        //weights[4] = baseWeight4 * GetMultiplier(bonus, mod4, power4);
        //weights[5] = baseWeight5 * GetMultiplier(bonus, mod5, power5);
        //weights[6] = baseWeight6 * GetMultiplier(bonus, mod6, power6);
        weights[1] = baseWeight1 * GetValue(bonus, mod1, power1);
        weights[2] = baseWeight2 * GetValue(bonus, mod2, power2);
        weights[3] = baseWeight3 * GetValue(bonus, mod3, power3);
        weights[4] = baseWeight4 * GetValue(bonus, mod4, power4);
        weights[5] = baseWeight5 * GetValue(bonus, mod5, power5);
        weights[6] = baseWeight6 * GetValue(bonus, mod6, Mathf.Infinity);
        //weights[1] = baseWeight1;
        //weights[2] = baseWeight2 * Mathf.Pow(1 + mod2, Mathf.Min(bonus, power2));
        //weights[3] = baseWeight3 * Mathf.Pow(1 + mod3, Mathf.Min(bonus, power3));
        //weights[4] = baseWeight4 * Mathf.Pow(1 + mod4, Mathf.Min(bonus, power4));
        //weights[5] = baseWeight5 * Mathf.Pow(1 + mod5, Mathf.Min(bonus, power5));
        //weights[6] = baseWeight6 * Mathf.Pow(1 + mod6, Mathf.Min(bonus, power6));
        //for (int i = 0; i <= 5; i++)
        //    weights[i + 1] = _baseWeights[i] * GetValue(bonus, _baseMods[i], _baseMaxMulti[i]);

        float total = weights.Sum();
        text2.text = ($"Rarity bonus: {bonus * 100}%\n" +
            $"Common:        {weights[1] / total:P1}\n" +
            $"Uncommon:     {weights[2] / total:P1}\n" +
            $"Rare:                {weights[3] / total:P1}\n" +
            $"Epic:                 {weights[4] / total:P1}\n" +
            $"Legendary:       {weights[5] / total:P1}\n" +
            $"Unique:            {weights[6] / total:P1}");
        if (debug)
        {
            Debug.Log($"Rarity bonus: {bonus * 100}%\n" +
                $"Common: {weights[1] / total:P1} " +
                $"Uncommon: {weights[2] / total:P1} " +
                $"Rare: {weights[3] / total:P1} " +
                $"Epic: {weights[4] / total:P1} " +
                $"Legendary: {weights[5] / total:P1} " +
                $"Unique: {weights[6] / total:P1} ");
        }

        
    }

    private float GetMultiplier(float bonus, float baseRate, float maxBonus)
    {
        float multiplier = 1 + Mathf.Min(maxBonus, bonus * baseRate);
        return multiplier;
    }

    private float GetValue(float rarity, float baseMod, float maxMultiplier)
    {
        float multiplier = 1 + rarity;
        if (multiplier <= maxMultiplier)
            return multiplier * baseMod;

        float excess = multiplier - maxMultiplier;
        float ratio = excess / maxMultiplier;
        multiplier *= (1f / (1f + ratio));
        return multiplier * baseMod;
    }

   
}
