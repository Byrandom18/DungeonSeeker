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

    private static readonly int MOVE = Animator.StringToHash("IsMoving");
    private static readonly int SPEED_MULTI = Animator.StringToHash("ChasingSpeedMultiplier");
    private static readonly int ATTACK_HASH = Animator.StringToHash("Attack");
    private static readonly int HIT = Animator.StringToHash("TakeHit");
    private static readonly int DIE = Animator.StringToHash("Death");



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

    
    // ========== Attack ===========================================================
    private void EnemyAI_OnEnemyAttack(object sender, EventArgs e)
    {
        _animator.SetTrigger(ATTACK_HASH);
        _enemyAI.IsAttacking = true;

        Vector3 targetPos = GetNearestTargetPosition();
        Vector3 direction = (targetPos - transform.position).normalized;
        _attackPosition = transform.position + direction * 1f;
        _enemyAI.IsFacingRight = direction.x > 0;
        UpdateSpriteDirection();
    }

    public void AttackStart()
    {
        GameObject go = Instantiate(_explosionPrefab, _attackPosition, Quaternion.identity, transform);
        _explosionScript = go.GetComponent<Explosion>();
        if (_explosionScript != null)
        {
            float remainingTime = GetRemainingAnimationTime();
            _explosionScript.MaxRadius = 2f;
            _explosionScript.ExplosionTime = remainingTime;
            _explosionScript.Damage = _enemyDamage.CurrentAttack;
            _explosionScript.KnockbackMultiplier = _enemyDamage.KnockbackMultiplier;
        }
    }
    public void AttackCancelled()
    {
        if (_explosionScript != null) _explosionScript.StopExplosion();
        _enemyAI.IsAttacking = false;
    }

    public void AttackEnd() => _enemyAI.IsAttacking = false;

    public IEnumerator StaggerReturnRoutine()
    {
        _enemyDamage.CanReceiveStagger = false;
        yield return new WaitForSeconds(GetRemainingAnimationTime());
        _enemyDamage.CanReceiveStagger = true;
    }

    // ============= Hit / Death =====================================================
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

    // ========== Sprite direction ======================================================
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

    // =========== Helpers =============================================================
    private Vector3 GetNearestTargetPosition()
    {
        if (PartyManager.Instance != null)
        {
            var t = PartyManager.Instance.GetNearestTarget(transform.position);
            if (t != null) return t.Transform.position;
        }
        return PlayerMovement.Instance != null
            ? PlayerMovement.Instance.transform.position
            : transform.position;
    }

    private float GetRemainingAnimationTime()
    {
        AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
        if (info.length <= 0 || (!info.loop && info.normalizedTime >= 1f)) return 0f;
        float current = (info.normalizedTime % 1f) * info.length;
        return info.length - current;
    }

    private void OnDestroy()
    {
        _enemyAI.OnEnemyAttack -= EnemyAI_OnEnemyAttack;
        _enemyDamage.OnTakeHit -= EnemyDamage_OnTakeHit;
        _enemyDamage.OnDeath -= EnemyDamage_OnDeath;
        _enemyAI.OnEnemyUpdateSpriteDirection -= EnemyAI_OnEnemyUpdateSpriteDirection;
    }
}
