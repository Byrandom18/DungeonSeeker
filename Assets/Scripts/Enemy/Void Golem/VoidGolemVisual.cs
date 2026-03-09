using System;
using System.Collections;
using UnityEngine;


public class VoidGolemVisual : MonoBehaviour
{
    [SerializeField] private EnemyAI _enemyAI;
    [SerializeField] private EnemyDamage _enemyDamage;
    [SerializeField] private GameObject _explosionPrefab;
    private Explosion _explosionScript = null;
    private SpriteRenderer _sprite;
    private Vector2 _originScale;
    private Animator _animator;
    private Vector3 _attackPosition;

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

    private void EnemyAI_OnEnemyUpdateSpriteDirection(object sender, EventArgs e)
    {
        UpdateSpriteDirection();
    }

    private void Update()
    {
        _animator.SetBool(MOVE, _enemyAI.IsRunning());
        _animator.SetFloat(SPEED_MULTI, _enemyAI.GetRoamingAnimationSpeed());
    }

    

    private void EnemyAI_OnEnemyAttack(object sender, EventArgs e)
    {
        _animator.SetTrigger(ATTACK_HASH);
        _enemyAI.IsAttacking = true;
        Vector3 direction = (PlayerMovement.Instance.transform.position - transform.position).normalized;
        _attackPosition = transform.position + direction * 1f;
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
        
        GameObject explosion = Instantiate(_explosionPrefab, _attackPosition, Quaternion.identity, transform);
        _explosionScript = explosion.GetComponent<Explosion>();
        if (_explosionScript)
        {
            float explosionTime = GetRemainingAnimationTime();
            _explosionScript.MaxRadius = 2;
            _explosionScript.ExplosionTime = explosionTime;
            _explosionScript.Damage = _enemyDamage.CurrentAttack;
            _explosionScript.KnockbackMultiplier = _enemyDamage.KnockbackMultiplier;
        }
    }
    private float GetRemainingAnimationTime()
    {
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.length <= 0) return 0f;
        if (!stateInfo.loop && stateInfo.normalizedTime >= 1f) return 0f;
        // Текущее время в секундах
        float currentTime = (stateInfo.normalizedTime % 1f) * stateInfo.length;
        // Оставшееся время
        return stateInfo.length - currentTime;
    }

    public IEnumerator StaggerReturnRoutine()
    {
        _enemyDamage.CanReceiveStagger = false;
        yield return new WaitForSeconds(GetRemainingAnimationTime());
        _enemyDamage.CanReceiveStagger = true;
    } 

    public void AttackCancelled()
    {
        if (_explosionScript != null) _explosionScript.StopExplosion();
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
        _animator.SetTrigger(DIE);
        AttackCancelled();
        //_enemyAI.SetDeathState();
    }

    private void OnDestroy()
    {
        _enemyAI.OnEnemyAttack -= EnemyAI_OnEnemyAttack;
        _enemyDamage.OnTakeHit -= EnemyDamage_OnTakeHit;
        _enemyDamage.OnDeath -= EnemyDamage_OnDeath;
        _enemyAI.OnEnemyUpdateSpriteDirection -= EnemyAI_OnEnemyUpdateSpriteDirection;
    }
}
