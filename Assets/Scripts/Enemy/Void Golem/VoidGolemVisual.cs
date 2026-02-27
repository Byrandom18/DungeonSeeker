using System;
using UnityEngine;


public class VoidGolemVisual : MonoBehaviour
{
    [SerializeField] private EnemyAI _enemyAI;
    [SerializeField] private EnemyDamage _enemyDamage;

    private Animator _animator;

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
        _animator.SetBool(IS_MOVING, _enemyAI.IsRunning());
        _animator.SetFloat(CHASING_SPEED_MULTIPLIER, _enemyAI.GetRoamingAnimationSpeed());
    }

    private void OnDestroy()
    {
        _enemyAI.OnEnemyAttack -= EnemyAI_OnEnemyAttack;
        _enemyDamage.OnTakeHit -= EnemyDamage_OnTakeHit;
    }

    private void EnemyAI_OnEnemyAttack(object sender, System.EventArgs e)
    {
        _animator.SetTrigger(ATTACK); //stagger cancel logic in invoke in EnemyAI
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
        if (_enemyDamage.InStagger) _animator.SetTrigger(TAKE_HIT);
    }

    private void EnemyDamage_OnDeath(object sender, EventArgs e)
    {
        _animator.SetTrigger(DEATH);
        //_enemyAI.SetDeathState();
    }

    
}
