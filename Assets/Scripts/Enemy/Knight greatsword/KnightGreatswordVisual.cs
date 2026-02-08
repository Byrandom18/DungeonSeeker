using UnityEngine;


public class KnightGreatswordVisual : MonoBehaviour
{
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private EnemyDamage enemyDamage;

    private Animator animator;

    private const string IS_MOVING = "IsMoving";
    private const string CHASING_SPEED_MULTIPLIER = "ChasingSpeedMultiplier";
    private const string ATTACK = "Attack";

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        enemyAI.OnEnemyAttack += enemyAI_OnEnemyAttack;
    }

    private void Update()
    {
        animator.SetBool(IS_MOVING, enemyAI.IsRunning());
        animator.SetFloat(CHASING_SPEED_MULTIPLIER, enemyAI.GetRoamingAnimationSpeed());
    }

    private void OnDestroy()
    {
        enemyAI.OnEnemyAttack -= enemyAI_OnEnemyAttack;
    }

    private void enemyAI_OnEnemyAttack(object sender, System.EventArgs e)
    {
        animator.SetTrigger(ATTACK);
    }
}
