using UnityEngine;
using UnityEngine.InputSystem;


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

        public static string GetEnglishKeyName(Key keyCode)
        {
            return keyCode switch
            {
                Key.Q => "Q",
                Key.W => "W",
                Key.E => "E",
                Key.R => "R",
                Key.T => "T",
                Key.Y => "Y",
                Key.U => "U",
                Key.I => "I",
                Key.O => "O",
                Key.P => "P",
                Key.A => "A",
                Key.S => "S",
                Key.D => "D",
                Key.F => "F",
                Key.G => "G",
                Key.H => "H",
                Key.J => "J",
                Key.K => "K",
                Key.L => "L",
                Key.Z => "Z",
                Key.X => "X",
                Key.C => "C",
                Key.V => "V",
                Key.B => "B",
                Key.N => "N",
                Key.M => "M",
                Key.Digit1 => "1",
                Key.Digit2 => "2",
                Key.Digit3 => "3",
                Key.Digit4 => "4",
                Key.Digit5 => "5",
                Key.Digit6 => "6",
                Key.Digit7 => "7",
                Key.Digit8 => "8",
                Key.Digit9 => "9",
                Key.Digit0 => "0",
                Key.Space => "Space",
                Key.LeftShift => "LShift",
                Key.RightShift => "RShift",
                Key.LeftCtrl => "LCtrl",
                Key.RightCtrl => "RCtrl",
                Key.LeftAlt => "LAlt",
                Key.RightAlt => "RAlt",
                Key.LeftCommand => "LCmd",
                Key.RightCommand => "RCmd",
                Key.UpArrow => "↑",
                Key.DownArrow => "↓",
                Key.LeftArrow => "←",
                Key.RightArrow => "→",
                Key.Enter => "Enter",
                Key.Tab => "Tab",
                Key.Escape => "Esc",
                Key.Backspace => "Backspace",
                Key.Delete => "Del",
                Key.Home => "Home",
                Key.End => "End",
                Key.PageUp => "PgUp",
                Key.PageDown => "PgDn",
                Key.F1 => "F1",
                Key.F2 => "F2",
                Key.F3 => "F3",
                Key.F4 => "F4",
                Key.F5 => "F5",
                Key.F6 => "F6",
                Key.F7 => "F7",
                Key.F8 => "F8",
                Key.F9 => "F9",
                Key.F10 => "F10",
                Key.F11 => "F11",
                Key.F12 => "F12",
                _ => keyCode.ToString()
            };
        }
    }
}