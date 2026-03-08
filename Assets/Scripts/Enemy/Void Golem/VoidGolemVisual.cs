using System;
using System.Collections;
using UnityEngine;


public class VoidGolemVisual : MonoBehaviour
{
    [SerializeField] private EnemyAI _enemyAI;
    [SerializeField] private EnemyDamage _enemyDamage;
    [SerializeField] private GameObject _explosionPrefab;
    private Explosion _explosionScript = null;

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
    }

    private void Start()
    {
        _enemyAI.OnEnemyAttack += EnemyAI_OnEnemyAttack;
        _enemyDamage.OnTakeHit += EnemyDamage_OnTakeHit;
        _enemyDamage.OnDeath += EnemyDamage_OnDeath;
    }

    

    private void Update()
    {
        _animator.SetBool(MOVE, _enemyAI.IsRunning());
        _animator.SetFloat(SPEED_MULTI, _enemyAI.GetRoamingAnimationSpeed());
    }

    

    private void EnemyAI_OnEnemyAttack(object sender, System.EventArgs e)
    {
        _animator.SetTrigger(ATTACK_HASH); //stagger cancel logic in invoke in EnemyAI
        _enemyAI.IsAttacking = true;
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        float animLength = stateInfo.length;
        Invoke("AttackFinished", animLength);
    }

    private void AttackFinished()
    {
        _enemyAI.IsAttacking = false;
    }

    public void AttackStart()
    {
        Vector3 direction = (PlayerMovement.Instance.transform.position - transform.position).normalized;
        Vector3 targetPosition = transform.position + direction * 1f;
        GameObject explosion = Instantiate(_explosionPrefab, targetPosition, Quaternion.identity, transform);
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
    }

    private void EnemyDamage_OnTakeHit(object sender, System.EventArgs e)
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
    }
}
