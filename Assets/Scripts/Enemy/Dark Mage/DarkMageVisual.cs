using System;
using System.Collections;
using UnityEngine;

public class DarkMageVisual : MonoBehaviour
{
    [SerializeField] private EnemyAI _enemyAI;
    [SerializeField] private EnemyDamage _enemyDamage;
    [SerializeField] private DarkMageCombat _enemyCombat;

    private SpriteRenderer _sprite;
    private Vector2 _originScale;
    private Animator _animator;


    private static readonly int MOVE = Animator.StringToHash("IsMoving");
    private static readonly int SPEED_MULTI = Animator.StringToHash("ChasingSpeedMultiplier");
    private static readonly int ATTACK_HASH = Animator.StringToHash("Attack");
    private static readonly int HIT = Animator.StringToHash("TakeHit");
    private static readonly int DIE = Animator.StringToHash("Death");


    [SerializeField] private AttackChargeVisual _chargeVisual;


    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _sprite = GetComponent<SpriteRenderer>();
        if (_chargeVisual == null) Debug.LogError($"[VoidGolemVisual] chargeVisual not assigned on {gameObject.name}");
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

    // ============= Attack ============================================================
    private IEnumerator StartChargeNextFrame()
    {
        yield return null; // ��� ���� ���� ����� �������� ������� StateInfo
        float remainingTime = GetRemainingAnimationTime();
        _chargeVisual?.StartCharge(remainingTime);
    }
    private void EnemyAI_OnEnemyAttack(object sender, EventArgs e)
    {
        _animator.SetTrigger(ATTACK_HASH);
        _enemyAI.IsAttacking = true;

        // Turning to the nearest target
        Vector3 targetPos = GetNearestTargetPosition();
        Vector3 dir = targetPos - transform.position;
        _enemyAI.IsFacingRight = dir.x > 0;
        UpdateSpriteDirection();

        StartCoroutine(StartChargeNextFrame());
    }

    public void AttackStart() => _enemyCombat.Shoot(_enemyDamage.CurrentAttack);
    public void AttackCancelled()
    {
        _enemyAI.IsAttacking = false;
        _chargeVisual?.StopCharge();
    }
    public void AttackEnd()
    {
        _enemyAI.IsAttacking = false;
        _chargeVisual?.StopCharge();
    }

    // =========== Hit / Death =====================================================
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

    // ========= Sprite direction =======================================================
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

    // ============ Helpers ===========================================================
    private float GetRemainingAnimationTime()
    {
        AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
        if (info.length <= 0 || (!info.loop && info.normalizedTime >= 1f)) return 0f;
        float current = (info.normalizedTime % 1f) * info.length;
        return info.length - current;
    }

    private Vector3 GetNearestTargetPosition()
    {
        if (PartyManager.Instance != null)
        {
            var t = PartyManager.Instance.GetNearestTarget(transform.position);
            if (t != null) return t.Transform.position;
        }
        if (PartyManager.Instance != null && PartyManager.Instance.LeaderTransform != null)
            return PartyManager.Instance.LeaderTransform.position;

        return transform.position;
    }

    private void OnDestroy()
    {
        _enemyAI.OnEnemyAttack -= EnemyAI_OnEnemyAttack;
        _enemyDamage.OnTakeHit -= EnemyDamage_OnTakeHit;
        _enemyDamage.OnDeath -= EnemyDamage_OnDeath;
        _enemyAI.OnEnemyUpdateSpriteDirection -= EnemyAI_OnEnemyUpdateSpriteDirection;
    }
}
