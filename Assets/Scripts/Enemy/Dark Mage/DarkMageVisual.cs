using System;
using UnityEngine;

public class DarkMageVisual : MonoBehaviour
{
    [SerializeField] private EnemyAI _enemyAI;
    [SerializeField] private EnemyDamage _enemyDamage;
    [SerializeField] private DarkMageCombat _enemyCombat;
    
    private SpriteRenderer _sprite;
    private Vector2 _originScale;
    private Animator _animator;
    

    private static readonly int MOVE = Animator.StringToHash(IS_MOVING);
    private static readonly int SPEED_MULTI = Animator.StringToHash(CHASING_SPEED_MULTIPLIER);
    private static readonly int ATTACK_HASH = Animator.StringToHash(ATTACK);
    private static readonly int HIT = Animator.StringToHash(TAKE_HIT);
    private static readonly int DIE = Animator.StringToHash(DEATH);

    private const string IS_MOVING = "IsMoving";
    private const string CHASING_SPEED_MULTIPLIER = "ChasingSpeedMultiplier";
    private const string ATTACK = "Attack";
    private const string TAKE_HIT = "TakeHit";
    private const string DEATH = "Death";

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _sprite = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        _enemyAI.OnEnemyAttack += EnemyAI_OnEnemyAttack;
        _enemyDamage.OnTakeHit += EnemyDamage_OnTakeHit;
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

    private void EnemyAI_OnEnemyAttack(object sender, EventArgs e)
    {
        _animator.SetTrigger(ATTACK_HASH);
        _enemyAI.IsAttacking = true;

        Vector2 direction = (PlayerMovement.Instance.transform.position - transform.position);
        if (direction.x > 0) _enemyAI.IsFacingRight = true;
        else _enemyAI.IsFacingRight = false;
        UpdateSpriteDirection();
    }

    private void UpdateSpriteDirection()
    {
        if (_enemyAI.IsFacingRight)
            _sprite.transform.localScale = new Vector3(_originScale.x, _originScale.y, 1);
        else if (!_enemyAI.IsFacingRight)
            _sprite.transform.localScale = new Vector3(-_originScale.x, _originScale.y, 1);
    }

    public void AttackStart()
    {
        _enemyCombat.Shoot(_enemyDamage.CurrentAttack);
    }

    public void AttackCancelled()
    {
        _enemyAI.IsAttacking = false;
    }

    public void AttackEnd()
    {
        _enemyAI.IsAttacking = false;
    }

    private void EnemyDamage_OnTakeHit(object sender, EventArgs e)
    {
        if (_enemyDamage.InStagger)
        {
            _animator.SetTrigger(HIT);
            AttackCancelled();
        }
    }

    private void EnemyDamage_OnDeath(object sender, EventArgs e)
    {
        _animator.SetBool(DIE, true);
        AttackCancelled();
    }

    private void OnDestroy()
    {
        _enemyAI.OnEnemyAttack -= EnemyAI_OnEnemyAttack;
        _enemyDamage.OnTakeHit -= EnemyDamage_OnTakeHit;
        _enemyDamage.OnDeath -= EnemyDamage_OnDeath;
        _enemyAI.OnEnemyUpdateSpriteDirection -= EnemyAI_OnEnemyUpdateSpriteDirection;
    }
}
