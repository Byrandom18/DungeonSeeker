using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[SelectionBase]
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D _rb;
    [SerializeField] private float _speed = 3f;
    private float _minMovingSpeed = 0.1f;
    private PlayerStats _stats;
    private PlayerVisual _visual;
    private Vector2 _inputVector;
    private Camera _mainCamera;
    private Knockback _knockback;
    public static PlayerMovement Instance { get; private set; }

    [Header("Dodge Settings")]
    [SerializeField] private float _dodgePower = 10f; // Сила рывка
    [SerializeField] private float _dodgeDuration = 0.2f;
    [SerializeField] private float _cooldown = 5f;

    private bool _isDodging = false;
    private bool _canDodge = true;
    private Vector2 _lastMovementDirection = Vector2.right;
    private bool _isRunning = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        
        Instance = this;
        _stats = GetComponent<PlayerStats>();
        _visual = GetComponentInChildren<PlayerVisual>();
        _mainCamera = Camera.main;
        _knockback = GetComponent<Knockback>();
        if (_rb == null)
            Debug.LogError("Rigidbody2D not found on " + gameObject.name);
        if (_visual == null)
            Debug.LogError("VisualComponent not found on " + gameObject.name);
        if (_stats == null)
            Debug.LogError("PlayerStats not found on " + gameObject.name);
        if (Instance == null)
            Debug.LogError("Instance can not be assigned on" + gameObject.name);
        if (_knockback == null)
            Debug.LogError("Knockback can not be assigned on" + gameObject.name);
    }

    private void Start()
    {
        
        GameInput.Instance.OnPlayerDodge += GameInput_OnPlayerDodge;
    }

    private void Update()
    {
        _inputVector = GameInput.Instance.GetMovementVector();
    }

    private void FixedUpdate()
    {
        if (!_stats.IsAlive) return;
        Move();
        UpdateSpriteDirection();
    }


    public Vector3 GetPlayerScreenPosition()
    {
        Vector3 playerScreenPosition = _mainCamera.WorldToScreenPoint(transform.position);
        return playerScreenPosition;
    }

    public bool IsRunning()
    {
        return _isRunning;
    }

    private void GameInput_OnPlayerDodge(object sender, System.EventArgs e)
    {
        if (_canDodge && !_isDodging && _stats.IsAlive)
        {
            StartCoroutine(PerformDodge());
        }
    }

    private void Move()
    {
        

        if (_inputVector != Vector2.zero)
        {
            _lastMovementDirection = _inputVector;
        }

        _rb.transform.position = (_rb.position + _inputVector * (_speed * Time.fixedDeltaTime));
        if (Mathf.Abs(_inputVector.x) > _minMovingSpeed || Mathf.Abs(_inputVector.y) > _minMovingSpeed)
        {
            _isRunning = true;
        }
        else
        {
            _isRunning = false;
        }
        //rb.linearVelocity = inputVector * speed;
    }

    private void UpdateSpriteDirection()
    {
        if (_lastMovementDirection.x < -0.1f)
        {
            _visual.UpdateSpriteDirection(false);
        }
        else if (_lastMovementDirection.x > 0.1f)
        {
            _visual.UpdateSpriteDirection(true);
        }
    }

    
    private IEnumerator PerformDodge()
    {
        // Подготовка
        _canDodge = false;
        _isDodging = true;
        _stats.Invulnerability = true;

        // Определяем направление
        Vector2 dodgeDirection = GameInput.Instance.GetMovementVector();
        if (dodgeDirection == Vector2.zero)
        {
            dodgeDirection = _lastMovementDirection;
        }


        // Применяем рывок через velocity
        //_rb.linearVelocity = dodgeDirection * _dodgePower;
        _rb.AddForce(dodgeDirection * _dodgePower, ForceMode2D.Impulse);
        // Ждем duration
        yield return new WaitForSeconds(_dodgeDuration);

        // Возвращаем обычную скорость (если игрок держит кнопку движения)
        if (!_isDodging) // Дополнительная проверка на случай прерывания
        {
            //_rb.linearVelocity = GameInput.Instance.GetMovementVector() * _speed;
            _rb.AddForce(dodgeDirection * _dodgePower, ForceMode2D.Impulse);
        }

        // Завершение
        _isDodging = false;
        _stats.Invulnerability = false;
        // Перезарядка
        yield return new WaitForSeconds(_cooldown);
        _canDodge = true;

    }

    private void OnDestroy()
    {
        GameInput.Instance.OnPlayerDodge -= GameInput_OnPlayerDodge;
    }
}
