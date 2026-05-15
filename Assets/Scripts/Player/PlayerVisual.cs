using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    private SpriteRenderer _sprite;
    private Vector2 _originScale;
    private Animator _animator;

    [SerializeField] private PlayerStats _ownerStats;
    private PlayerMovement _movement;

    private static readonly int DIE = Animator.StringToHash(DEATH);
    private static readonly int MOVE = Animator.StringToHash(IS_MOVING);

    private const string IS_MOVING = "IsMoving";
    private const string DEATH = "Death";

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        _originScale = transform.localScale;
        _animator = GetComponent<Animator>();
        if (_ownerStats == null)
            _ownerStats = GetComponentInParent<PlayerStats>();
        _movement = GetComponentInParent<PlayerMovement>();
    }

    private void Start()
    {
        _ownerStats.OnPlayerDeath += PlayerStats_OnPlayerDeath;
    }

    private void PlayerStats_OnPlayerDeath(object sender, System.EventArgs e)
    {
        _animator.SetTrigger(DIE);
    }

    private void Update()
    {
        if (_movement != null)
            _animator.SetBool(MOVE, _movement.IsRunning());
    }

    public void UpdateSpriteDirection(bool flipRight)
    {
        if (!flipRight)
        {
            _sprite.transform.localScale = new Vector3(-_originScale.x, _originScale.y, 1);
        }
        else if (flipRight)
        {
            _sprite.transform.localScale = new Vector3(_originScale.x, _originScale.y, 1);
        }
    }

    private void OnDestroy()
    {
        _ownerStats.OnPlayerDeath -= PlayerStats_OnPlayerDeath;
    }
}
