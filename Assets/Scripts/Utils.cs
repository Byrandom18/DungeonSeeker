using UnityEngine;


namespace GameUtils
{
    public static class Utils
    {
        public static Vector3 GetRandomDir()
        {
            return new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }

        public static ItemRarity RollRarity(float luck)
        {
            // Пример весов (можно настраивать)
            float[] weights = new float[7];
            weights[1] = Mathf.Max(40f - luck * 0.7f, 10f);   // Common
            weights[2] = 28f;
            weights[3] = 15f;
            weights[4] = 8f;
            weights[5] = 4f + luck * 0.25f;                   // Legendary
            weights[6] = 1f + luck * 0.35f;                   // Unique

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
    }
}