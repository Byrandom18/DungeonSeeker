using System.Collections;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    [Header("Circles config")]
    public Color CircleColor = new Color(1f, 0f, 0f, 0.25f);
    public Color OuterCircleColor = new Color(1f, 0f, 0f, 0.5f);
    public Color InnerCircleColor = new Color(1f, 0f, 0f, 0.5f); 
    public float MaxRadius = 5f;
    public float ExplosionTime = 3f;
    public float LineWidth = 0.1f;

    [Header("Combat settings")]
    public float Damage = 1f;
    public float KnockbackMultiplier = 1f;

    private bool _isExpanding;
    private CircleCollider2D _circleCollider;
    private SpriteRenderer _spriteRenderer;
    
    [SerializeField] private LineRenderer _innerLineRenderer;
    [SerializeField] private LineRenderer _outerLineRenderer;

    //[SerializeField] private bool _start;
    private float _currentRadius = 0f;
    private int _segments = 50;

    private void Awake()
    {
        _circleCollider = GetComponent<CircleCollider2D>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        CreateLineRenderers();
        _circleCollider.enabled = false;
        _circleCollider.radius = MaxRadius;
        _spriteRenderer.transform.localScale = Vector2.zero;
        _spriteRenderer.color = CircleColor;
        StartExplosion();
    }

    private void Update()
    {
        //if (_start && !_isExpanding) StartExplosion(); //manual launch
        if (!_isExpanding) return;
        float expansionSpeed = MaxRadius / ExplosionTime;
        _currentRadius += expansionSpeed * Time.deltaTime;
        _currentRadius = Mathf.Clamp(_currentRadius, 0f, MaxRadius);
        float circleDiameter = _currentRadius * 2 + LineWidth;
        _spriteRenderer.transform.localScale = new Vector2(circleDiameter, circleDiameter);
        CreateCircles();

        if (_currentRadius >= MaxRadius)
        {
            _circleCollider.enabled = true;
            _isExpanding = false;
            StartCoroutine(FadeOutAndDestroy());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision.transform.TryGetComponent(out PlayerStats player))
        {
            player.TakeDamage(Damage, transform, KnockbackMultiplier);
        }
    }

    public void StartExplosion()
    {
        _currentRadius = 0f;
        _isExpanding = true;
    }

    public void StopExplosion()
    {
        _isExpanding = false;
        StartCoroutine(FadeOutAndDestroy());
    }

    private void CreateLineRenderers()
    {
        // Создаем объекты для кругов
        SetCircleSettings(_outerLineRenderer, OuterCircleColor, false);
        SetCircleSettings(_innerLineRenderer, InnerCircleColor, true);

    }

    private void SetCircleSettings(LineRenderer lr, Color color, bool isFilled)
    {
        lr.positionCount = _segments + 1;
        lr.loop = true;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        lr.material = mat;

        if (isFilled)
        {
            lr.startColor = color;
            lr.endColor = color;
            lr.startWidth = LineWidth;
            lr.endWidth = LineWidth;
        }
        else
        {
            Color transparenColor = new(color.r, color.g, color.b, 0f);
            lr.startColor = transparenColor;
            lr.endColor = transparenColor;
            lr.startWidth = LineWidth;
            lr.endWidth = LineWidth;
            float outerAlpha = color.a;
            StartCoroutine(OuterLineFadeRoutine(0, outerAlpha));
        }

    }

    

    private void CreateCircles()
    {
        // Внешний круг всегда на максимальном радиусе
        DrawCircle(_outerLineRenderer, MaxRadius);

        // Внутренний круг растет
        DrawCircle(_innerLineRenderer, _currentRadius);
    }

    private void DrawCircle(LineRenderer lr, float radius)
    {
        if (lr == null) return;

        float angle = 0f;
        for (int i = 0; i <= _segments; i++)
        {
            float x = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            Vector3 worldPosition = transform.position + new Vector3(x, y, 0);
            lr.SetPosition(i, worldPosition);

            angle += 360f / _segments;
        }
    }
    //Fade alpha on destroy
    private IEnumerator FadeOutAndDestroy()
    {
        transform.SetParent(null);
        float duration = 0.2f;
        float elapsed = 0f;

        Color startCircleColor = CircleColor;
        Color startOuterColor = OuterCircleColor;
        Color startInnerColor = InnerCircleColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);

            CircleColor.a = startCircleColor.a * alpha;
            OuterCircleColor.a = startOuterColor.a * alpha;
            InnerCircleColor.a = startInnerColor.a * alpha;

            UpdateColors();

            yield return null;
        }

        Destroy(gameObject);
    }
    private void UpdateColors()
    {
        if (_outerLineRenderer != null)
        {
            _outerLineRenderer.startColor = OuterCircleColor;
            _outerLineRenderer.endColor = OuterCircleColor;
        }

        if (_innerLineRenderer != null)
        {
            _innerLineRenderer.startColor = InnerCircleColor;
            _innerLineRenderer.endColor = InnerCircleColor;
        }

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = CircleColor;
        }
    }

    private IEnumerator OuterLineFadeRoutine(float startTransparencyAmount, float targetTransparencyAmount)
    {
        float elapsedTime = 0f;
        float fadeTime = 0.1f;
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startTransparencyAmount, targetTransparencyAmount, elapsedTime / fadeTime);
            _outerLineRenderer.startColor = new Color(_spriteRenderer.color.r, _spriteRenderer.color.g, _spriteRenderer.color.b, newAlpha);
            _outerLineRenderer.endColor = new Color(_spriteRenderer.color.r, _spriteRenderer.color.g, _spriteRenderer.color.b, newAlpha);
            yield return null;
        }
    }
}
