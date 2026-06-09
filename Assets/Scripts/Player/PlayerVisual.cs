using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    private const string IsMovingParam = "IsMoving";
    private const string DeathParam = "Death";
    private const string IdleState = "Idle";

    private static readonly int DeathHash = Animator.StringToHash(DeathParam);
    private static readonly int IsMovingHash = Animator.StringToHash(IsMovingParam);
    private static readonly int IdleHash = Animator.StringToHash(IdleState);

    private SpriteRenderer _sprite;
    private Vector2 _originScale;
    private Animator _animator;

    [SerializeField] private PlayerStats _ownerStats;
    private PlayerMovement _movement;

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        _originScale = transform.localScale;
        _animator = GetComponent<Animator>();

        if (_ownerStats == null)
            _ownerStats = GetComponentInParent<PlayerStats>();

        _movement = GetComponentInParent<PlayerMovement>();
    }

    private void OnEnable()
    {
        if (_ownerStats != null)
            _ownerStats.OnPlayerDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        if (_ownerStats != null)
            _ownerStats.OnPlayerDeath -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath(object sender, System.EventArgs e)
    {
        if (_animator != null)
            _animator.SetTrigger(DeathHash);
    }

    public void ResetToIdle()
    {
        if (_animator == null)
            return;

        _animator.ResetTrigger(DeathHash);
        _animator.SetBool(IsMovingHash, false);
        _animator.Rebind();
        _animator.Update(0f);
        _animator.Play(IdleHash, 0, 0f);
        _animator.Update(0f);
    }

    private void Update()
    {
        if (_movement != null && _animator != null)
            _animator.SetBool(IsMovingHash, _movement.IsRunning());
    }

    public void UpdateSpriteDirection(bool flipRight)
    {
        if (!flipRight)
            _sprite.transform.localScale = new Vector2(-_originScale.x, _originScale.y);
        else
            _sprite.transform.localScale = new Vector2(_originScale.x, _originScale.y);
    }
}
