using System;
using UnityEngine;


public class VoidGolemVisual : MonoBehaviour
{
    [SerializeField] private EnemyAI _enemyAI;
    [SerializeField] private EnemyDamage _enemyDamage;

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

    private void EnemyDamage_OnTakeHit(object sender, System.EventArgs e)
    {
        if (_enemyDamage.InStagger) _animator.SetTrigger(HIT);
    }

    private void EnemyDamage_OnDeath(object sender, EventArgs e)
    {
        _animator.SetTrigger(DIE);
        //_enemyAI.SetDeathState();
    }

    private void OnDestroy()
    {
        _enemyAI.OnEnemyAttack -= EnemyAI_OnEnemyAttack;
        _enemyDamage.OnTakeHit -= EnemyDamage_OnTakeHit;
        _enemyDamage.OnDeath -= EnemyDamage_OnDeath;
    }
}
