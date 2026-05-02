using System.Collections;
using UnityEngine;

/// <summary>
/// Накладывает красно-оранжевый тинт во время замаха атаки.
///
/// Использует MaterialPropertyBlock на основном SpriteRenderer —
/// не требует второго рендерера, не создаёт новых материалов,
/// автоматически следует анимации (спрайт не нужно синхронизировать вручную).
///
/// Настройка:
///   1. Назначить _spriteRenderer — основной SpriteRenderer врага.
///      Если не назначен — ищется автоматически через GetComponentInChildren.
///   2. Вызвать StartCharge(duration) в начале анимации атаки.
///   3. Вызвать StopCharge() при отмене или завершении.
///
/// Совместимость: стандартные шейдеры URP Sprite-Lit-Default и
/// Sprite-Unlit-Default поддерживают _Color через MaterialPropertyBlock.
/// </summary>
public class AttackChargeVisual : MonoBehaviour
{
    [Header("Основной SpriteRenderer врага")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Цвет тинта")]
    [SerializeField] private Color _chargeColor = new Color(1f, 0.3f, 0f, 1f);

    [Header("Максимальная сила тинта (0–1)")]
    [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.55f;

    [Tooltip("Кривая нарастания. Если пустая — используется встроенная Pow 1.5")]
    [SerializeField] private AnimationCurve _chargeCurve;

    private static readonly int ColorProp = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock _mpb;
    private Coroutine _chargeRoutine;
    private Color _originalColor;
    private bool _initialized;

    private void Awake()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer == null)
        {
            Debug.LogError($"[AttackChargeVisual] SpriteRenderer не найден на {gameObject.name}");
            return;
        }

        _mpb = new MaterialPropertyBlock();
        _spriteRenderer.GetPropertyBlock(_mpb);

        // Запоминаем оригинальный цвет
        // У нового объекта блок пустой — берём из SpriteRenderer.color
        _originalColor = _spriteRenderer.color;

        // Кривая нарастания по умолчанию: медленно => резко в конце
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

    // == Публичный API =========================================================

    /// <summary>
    /// Начать нарастание тинта.
    /// duration — время до момента нанесения урона (берётся из аниматора).
    /// </summary>
    public void StartCharge(float duration)
    {
        if (!_initialized) return;
        StopCharge();
        if (duration <= 0f) return;
        _chargeRoutine = StartCoroutine(ChargeRoutine(duration));
    }

    /// <summary>Немедленно сбросить тинт.</summary>
    public void StopCharge()
    {
        if (_chargeRoutine != null)
        {
            StopCoroutine(_chargeRoutine);
            _chargeRoutine = null;
        }
        if (_initialized) ResetTint();
    }

    // == Корутина ==============================================================

    private IEnumerator ChargeRoutine(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float weight = _chargeCurve.Evaluate(t) * _maxAlpha;
            ApplyTint(weight);
            yield return null;
        }

        ApplyTint(_maxAlpha);
    }

    // == Внутренние методы =====================================================

    /// <summary>
    /// Смешивает оригинальный цвет спрайта с _chargeColor через Lerp.
    /// weight = 0 => оригинал, weight = 1 => полный тинт.
    /// Alpha оригинала сохраняется.
    /// </summary>
    private void ApplyTint(float weight)
    {
        if (_spriteRenderer == null) return;

        _spriteRenderer.GetPropertyBlock(_mpb);

        Color blended = Color.Lerp(_originalColor, _chargeColor, weight);
        blended.a = _originalColor.a; // не трогаем прозрачность

        _mpb.SetColor(ColorProp, blended);
        _spriteRenderer.SetPropertyBlock(_mpb);
    }

    private void ResetTint()
    {
        if (_spriteRenderer == null) return;

        _spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(ColorProp, _originalColor);
        _spriteRenderer.SetPropertyBlock(_mpb);
    }
}
