using System.Collections;
using UnityEngine;

/// <summary>
/// Аддитивный тинт во время замаха атаки.
///
/// Требует кастомный шейдер (Shader Graph) с аддитивным смешением RGB:
///   finalColor.rgb = spriteTexture.rgb + _TintColor.rgb
///
/// Это осветляет спрайт не замещая его цвет, сохраняя все детали.
/// При _TintColor = (0,0,0) — спрайт без изменений.
/// При _TintColor = (1,0.3,0) — оранжевое осветление.
///
/// Как создать шейдер (Shader Graph, URP, Unity 6):
///   1. ПКМ → Create → Shader Graph → URP → Sprite Unlit Shader Graph
///   2. Texture2D Property: Name="_MainTex",   Reference="_MainTex"
///   3. Color Property:     Name="_TintColor",  Reference="_TintColor", default=(0,0,0,0)
///   4. Sample Texture 2D ← _MainTex
///      → Split → Combine(R,G,B) → Add(+_TintColor.rgb) → Base Color
///      → A → Alpha
///   5. Сохранить. Создать материал. Назначить на SpriteRenderer врага.
/// </summary>
public class AttackChargeVisual : MonoBehaviour
{
    [Header("SpriteRenderer с кастомным шейдером")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Цвет аддитивного тинта (RGB прибавляется к спрайту)")]
    [SerializeField] private Color _tintColor = new Color(1f, 0.3f, 0f, 0f);

    [Header("Максимальная сила тинта (0–1)")]
    [SerializeField, Range(0f, 1f)] private float _maxIntensity = 0.6f;

    [Tooltip("Кривая нарастания. Пустая = медленно в начале, резко в конце")]
    [SerializeField] private AnimationCurve _chargeCurve;

    // Свойство в Shader Graph должно иметь Reference = _TintColor
    private static readonly int TintColorProp = Shader.PropertyToID("_TintColor");

    private MaterialPropertyBlock _mpb;
    private Coroutine _chargeRoutine;
    private bool _initialized;

    private void Awake()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (_spriteRenderer == null)
        {
            Debug.LogError($"[AttackChargeVisual] SpriteRenderer не найден на {gameObject.name}");
            return;
        }

        _mpb = new MaterialPropertyBlock();

        if (_chargeCurve == null || _chargeCurve.length == 0)
        {
            _chargeCurve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0.3f),
                new Keyframe(0.5f, 0.1f),
                new Keyframe(1f, 1f, 3f, 0f)
            );
        }

        _initialized = true;
        ResetTint();
    }

    // ── Публичный API ─────────────────────────────────────────────────────────

    public void StartCharge(float duration)
    {
        if (!_initialized) return;
        StopCharge();
        if (duration <= 0f) return;
        _chargeRoutine = StartCoroutine(ChargeRoutine(duration));
    }

    public void StopCharge()
    {
        if (_chargeRoutine != null)
        {
            StopCoroutine(_chargeRoutine);
            _chargeRoutine = null;
        }
        if (_initialized) ResetTint();
    }

    // ── Корутина ──────────────────────────────────────────────────────────────

    private IEnumerator ChargeRoutine(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float intensity = _chargeCurve.Evaluate(t) * _maxIntensity;
            ApplyTint(intensity);
            yield return null;
        }

        ApplyTint(_maxIntensity);
    }

    // ── Внутренние методы ─────────────────────────────────────────────────────

    /// <summary>
    /// Записывает аддитивный тинт в MaterialPropertyBlock.
    /// intensity = 0 → tintColor * 0 = чёрный (нет добавки)
    /// intensity = 1 → tintColor * 1 = полная добавка
    /// </summary>
    private void ApplyTint(float intensity)
    {
        if (_spriteRenderer == null) return;

        _spriteRenderer.GetPropertyBlock(_mpb);

        Color tint = new Color(
            _tintColor.r * intensity,
            _tintColor.g * intensity,
            _tintColor.b * intensity,
            0f // alpha не используется в аддитивном режиме
        );

        _mpb.SetColor(TintColorProp, tint);
        _spriteRenderer.SetPropertyBlock(_mpb);
    }

    private void ResetTint()
    {
        if (_spriteRenderer == null) return;

        _spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(TintColorProp, Color.clear);
        _spriteRenderer.SetPropertyBlock(_mpb);
    }
}
