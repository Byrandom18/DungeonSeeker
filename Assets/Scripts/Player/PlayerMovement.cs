using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField] private float speed = 3f;
    
    private PlayerStats stats;
    private PlayerVisual visual;
    private Vector2 inputVector;
    private Camera _mainCamera;
    public static PlayerMovement Instance { get; private set; }

    [Header("Dodge Settings")]
    [SerializeField] private float dodgePower = 10f; // Сила рывка
    [SerializeField] private float dodgeDuration = 0.2f;
    [SerializeField] private float cooldown = 5f;

    private bool isDodging = false;
    private bool canDodge = true;
    private Vector2 lastMovementDirection = Vector2.right;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        Instance = this;
        stats = GetComponent<PlayerStats>();
        visual = GetComponentInChildren<PlayerVisual>();
        _mainCamera = Camera.main;
        if (rb == null)
            Debug.LogError("Rigidbody2D not found on " + gameObject.name);
        if (visual == null)
            Debug.LogError("VisualComponent not found on " + gameObject.name);
        if (stats == null)
            Debug.LogError("PlayerStats not found on " + gameObject.name);
        if (Instance == null)
            Debug.LogError("Instance can not be assigned on" + gameObject.name);
    }

    private void Start()
    {
        
        GameInput.Instance.OnPlayerDodge += GameInput_OnPlayerDodge;
    }

    private void GameInput_OnPlayerDodge(object sender, System.EventArgs e)
    {
        if (canDodge && !isDodging)
        {
            StartCoroutine(PerformDodge());
        }
    }

    private void Update()
    {
        inputVector = GameInput.Instance.GetMovementVector();
    }

    private void FixedUpdate()
    {
        if (!isDodging)
        {
            Move();
            UpdateSpriteDirection();
        }
    }

    private void Move()
    {
        

        if (inputVector != Vector2.zero)
        {
            lastMovementDirection = inputVector;
        }

        rb.MovePosition(rb.position + inputVector * (speed * Time.fixedDeltaTime));
        //rb.linearVelocity = inputVector * speed;
    }

    private void UpdateSpriteDirection()
    {
        if (rb.linearVelocity.x < -0.1f)
        {
            visual.UpdateSpriteDirection(false);
        }
        else if (rb.linearVelocity.x > 0.1f)
        {
            visual.UpdateSpriteDirection(true);
        }
    }

    
    private IEnumerator PerformDodge()
    {
        // Подготовка
        canDodge = false;
        isDodging = true;
        stats.invulnerability = true;
        // Определяем направление
        Vector2 dodgeDirection = GameInput.Instance.GetMovementVector();
        if (dodgeDirection == Vector2.zero)
        {
            dodgeDirection = lastMovementDirection;
        }


        // Применяем рывок через velocity
        rb.linearVelocity = dodgeDirection * dodgePower;

        // Ждем duration
        yield return new WaitForSeconds(dodgeDuration);

        // Возвращаем обычную скорость (если игрок держит кнопку движения)
        if (!isDodging) // Дополнительная проверка на случай прерывания
        {
            rb.linearVelocity = GameInput.Instance.GetMovementVector() * speed;
        }

        // Завершение
        isDodging = false;
        stats.invulnerability = false;
        // Перезарядка
        yield return new WaitForSeconds(cooldown);
        canDodge = true;

    }

    public Vector3 GetPlayerScreenPosition()
    {
        Vector3 playerScreenPosition = _mainCamera.WorldToScreenPoint(transform.position);
        return playerScreenPosition;
    }
}
