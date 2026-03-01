using UnityEngine;

public class FlashBlink : MonoBehaviour
{
    [SerializeField] private MonoBehaviour _damagableObject;
    [SerializeField] private Material _blinkMaterial;
    [SerializeField] private float _blinkDuration = 0.1f;

    private float _blinkTimer;
    private Material _defaultMaterial;
    private SpriteRenderer _spriteRenderer;
    private bool _canBlink = true;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _defaultMaterial = _spriteRenderer.material;

        _canBlink = true;
    }

    private void Start()
    {
        if (_damagableObject is PlayerStats)
        {
            (_damagableObject as PlayerStats).OnFlashBlink += DamagableObject_OnFlashBlink;
        }
        if (_damagableObject is EnemyDamage)
        {
            (_damagableObject as EnemyDamage).OnTakeHit += DamagableObject_OnFlashBlink;
        }
    }

    
    private void Update()
    {
        if (_canBlink)
        {
            if (_blinkTimer >= 0)
            {
                _blinkTimer -= Time.deltaTime;
                if (_blinkTimer <= 0) SetDefaultMaterial();
            }
        }
    }
    private void DamagableObject_OnFlashBlink(object sender, System.EventArgs e)
    {
        SetBlinkMaterial();
    }

    private void SetBlinkMaterial()
    {
        _blinkTimer = _blinkDuration;
        _spriteRenderer.material = _blinkMaterial;
    }

    private void SetDefaultMaterial()
    {
        _spriteRenderer.material = _defaultMaterial;
    }
}
