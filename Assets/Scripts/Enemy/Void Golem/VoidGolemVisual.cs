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
        _enemyAI.OnEnemyAttack += _enemyAI_OnEnemyAttack;
        _enemyDamage.OnTakeHit += _enemyDamage_OnTakeHit;
        _enemyDamage.OnDeath += _enemyDamage_OnDeath;
    }

    

    private void Update()
    {
        _animator.SetBool(IS_MOVING, _enemyAI.IsRunning());
        _animator.SetFloat(CHASING_SPEED_MULTIPLIER, _enemyAI.GetRoamingAnimationSpeed());
    }

    private void OnDestroy()
    {
        _enemyAI.OnEnemyAttack -= _enemyAI_OnEnemyAttack;
        _enemyDamage.OnTakeHit -= _enemyDamage_OnTakeHit;
    }

    private void _enemyAI_OnEnemyAttack(object sender, System.EventArgs e)
    {
        _animator.SetTrigger(ATTACK);
        _enemyAI.IsAttacking = true;
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        float animLength = stateInfo.length;  // Длительность в секундах
        Invoke("AttackFinished", animLength);
    }

    private void AttackFinished()
    {
        _enemyAI.IsAttacking = false;
    }

    private void _enemyDamage_OnTakeHit(object sender, System.EventArgs e)
    {
        _animator.SetTrigger(TAKE_HIT);
    }

    private void _enemyDamage_OnDeath(object sender, EventArgs e)
    {
        _animator.SetTrigger(DEATH);
        _enemyAI.SetDeathState();
    }

    
}
