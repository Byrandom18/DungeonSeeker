using UnityEngine;

public class Explosive : MonoBehaviour
{
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private LineRenderer _staticLineRenderer;
    [SerializeField] private float _maxRadius = 5f;
    [SerializeField] private int _segments = 64;
    [SerializeField] private float _expandTime = 5;
    private float _currentRadius = 0f;
    private bool _isExpanding = false;

    [SerializeField] private bool _start;

    private void Start()
    {
        DrawCircle(_staticLineRenderer, _maxRadius);
    }
    

    private void Update()
    {
        if (_start && !_isExpanding) StartExplosion();
        if (!_isExpanding) return;
        float expandSpeed = _maxRadius / _expandTime;
        _currentRadius += expandSpeed * Time.deltaTime;

        if (_currentRadius >= _maxRadius)
        {
            _currentRadius = _maxRadius;
            _isExpanding = false;
            _lineRenderer.enabled = false;
        }

        
        DrawCircle(_lineRenderer, _currentRadius);
    }

    public void StartExplosion()
    {
        _currentRadius = 0f;
        _isExpanding = true;
        _lineRenderer.enabled = true;
    }
    
    private void DrawCircle(LineRenderer lineRenderer ,float radius)
    {
        lineRenderer.positionCount = _segments + 1;
        
        for (int i = 0; i <= _segments; i++)
        {
            float angle = 2 * Mathf.PI * i / _segments;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            lineRenderer.SetPosition(i, transform.position + new Vector3(x, y, 0f));
        }
    }
}
