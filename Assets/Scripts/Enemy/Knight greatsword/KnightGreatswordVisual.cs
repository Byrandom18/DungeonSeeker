using UnityEngine;


public class KnightGreatswordVisual : MonoBehaviour
{
    [SerializeField] private EnemyAI _enemyAI;
    [SerializeField] private EnemyDamage _enemyDamage;

    private Animator _animator;

    private const string IS_MOVING = "IsMoving";
    private const string CHASING_SPEED_MULTIPLIER = "ChasingSpeedMultiplier";
    private const string ATTACK = "Attack";

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        _enemyAI.OnEnemyAttack += enemyAI_OnEnemyAttack;
    }

    private void Update()
    {
        _animator.SetBool(IS_MOVING, _enemyAI.IsRunning());
        _animator.SetFloat(CHASING_SPEED_MULTIPLIER, _enemyAI.GetRoamingAnimationSpeed());
    }

    private void OnDestroy()
    {
        _enemyAI.OnEnemyAttack -= enemyAI_OnEnemyAttack;
    }

    private void enemyAI_OnEnemyAttack(object sender, System.EventArgs e)
    {
        _animator.SetTrigger(ATTACK);
    }
}
