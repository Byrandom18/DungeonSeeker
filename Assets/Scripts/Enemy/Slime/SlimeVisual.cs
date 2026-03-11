using System;
using UnityEngine;

public class SlimeVisual : MonoBehaviour
{
    [SerializeField] private EnemyAI _enemyAI;
    [SerializeField] private EnemyDamage _enemyDamage;
    private SpriteRenderer _sprite;
    private Vector2 _originScale;
    private Animator _animator;

    private static readonly int MOVE = Animator.StringToHash(IS_MOVING);
    private static readonly int SPEED_MULTI = Animator.StringToHash(CHASING_SPEED_MULTIPLIER);
    private static readonly int DIE = Animator.StringToHash(DEATH);
    //private static readonly int ATTACK_HASH = Animator.StringToHash(ATTACK);
    //private static readonly int HIT = Animator.StringToHash(TAKE_HIT);

    private const string IS_MOVING = "IsMoving";
    private const string CHASING_SPEED_MULTIPLIER = "ChasingSpeedMultiplier";
    private const string DEATH = "Death";
    //private const string ATTACK = "Attack";
    //private const string TAKE_HIT = "TakeHit";

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _sprite = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        _enemyDamage.OnDeath += EnemyDamage_OnDeath;
        _enemyAI.OnEnemyUpdateSpriteDirection += EnemyAI_OnEnemyUpdateSpriteDirection;
        _originScale = _sprite.transform.localScale;
    }

    private void Update()
    {
        _animator.SetBool(MOVE, _enemyAI.IsRunning());
        _animator.SetFloat(SPEED_MULTI, _enemyAI.GetRoamingAnimationSpeed());
    }

    private void EnemyAI_OnEnemyUpdateSpriteDirection(object sender, EventArgs e)
    {
        UpdateSpriteDirection();
    }

    private void UpdateSpriteDirection()
    {
        if (_enemyAI.IsFacingRight)
            _sprite.transform.localScale = new Vector3(_originScale.x, _originScale.y, 1);
        else if (!_enemyAI.IsFacingRight)
            _sprite.transform.localScale = new Vector3(-_originScale.x, _originScale.y, 1);
    }

    private void EnemyDamage_OnDeath(object sender, EventArgs e)
    {
        _animator.SetBool(DIE, true);
    }

    private void OnDestroy()
    {
        _enemyDamage.OnDeath -= EnemyDamage_OnDeath;
        _enemyAI.OnEnemyUpdateSpriteDirection -= EnemyAI_OnEnemyUpdateSpriteDirection;
    }
}
