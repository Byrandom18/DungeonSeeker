using UnityEngine;
using UnityEngine.AI;

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

    private NavMeshAgent _agent;
    private bool _agentLookRight;

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        _originScale = transform.localScale;
        _animator = GetComponent<Animator>();
        if (_ownerStats == null)
            _ownerStats = GetComponentInParent<PlayerStats>();

        _movement = GetComponentInParent<PlayerMovement>();
        _agent = GetComponentInParent<NavMeshAgent>();
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
        else if (_agent != null && _animator != null)
        {

            _animator.SetBool(IsMovingHash, IsBotRunning());
        }
    }

    public void UpdateSpriteDirection(bool flipRight)
    {
        if (!flipRight)
            _sprite.transform.localScale = new Vector2(-_originScale.x, _originScale.y);
        else
            _sprite.transform.localScale = new Vector2(_originScale.x, _originScale.y);
    }

    private bool IsBotRunning()
    {
        if (_agent.velocity.sqrMagnitude > 0.1)
        {
            if (_agent.velocity.x > 0.1)
                _agentLookRight = true;
            else if (_agent.velocity.x < 0.1)
                _agentLookRight = false;
            FlipSprite(_agentLookRight);
            return true;
        }
        return false;
    }

    private void FlipSprite(bool isRight)
    {
        if (!isRight)
            _sprite.flipX = true;
        else
            _sprite.flipX = false;
    }
}
