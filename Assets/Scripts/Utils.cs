using UnityEngine;


namespace GameUtils
{
    public static class Utils
    {
        // rarity settings
        // in future it will be possible to give buffs by reducing the rarity modifiers or cutting the maximum scale of everything except the unique ones.
        private static readonly float[] _baseWeights = { 3125f, 625f, 125f, 25f, 5f, 1f };
        private static readonly float[] _baseMods = { 0.1f, 0.2f, 0.35f, 0.5f, 0.75f, 1f };
        private static readonly float[] _baseMaxMulti = { 1f, 5f, 25f, 125f, 625f, Mathf.Infinity };

        public static Vector3 GetRandomDir()
        {
            return new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }

        public static ItemRarity RollRarity(float rarity)
        {
            float bonusRarity = Mathf.Max(0, rarity - 1);
            float[] weights = new float[7];
            for (int i = 0; i <= 5; i++)
                weights[i+1] = _baseWeights[i] * GetValue(bonusRarity, _baseMods[i], _baseMaxMulti[i]);

            float total = 0f;
            for (int i = 1; i <= 6; i++) total += weights[i];

            float r = Random.Range(0f, total);
            float c = 0f;

            for (int i = 1; i <= 6; i++)
            {
                c += weights[i];
                if (r <= c) return (ItemRarity)i;
            }

            return ItemRarity.Common;
        }
        private static float GetValue(float rarity, float baseMod, float maxMultiplier)
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
}